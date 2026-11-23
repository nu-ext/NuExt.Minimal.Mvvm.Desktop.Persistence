using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Persistence;

/// <summary>
/// Provides read and write access to key/value settings stored as rows of a SQLite table.
/// </summary>
/// <remarks>
/// The table is expected to contain an owner id column plus name, raw value and value type columns.
/// The value type is persisted as a stable token (for example <c>string</c> or <c>integer</c>) and is
/// converted to <see cref="Type"/> only at this boundary.
/// </remarks>
public sealed class KeyValueSettingDal(Func<SQLiteDbConnection> createConnection) : SQLiteDalBase(createConnection)
{
    private static readonly string[] s_columns = ["setting_name", "setting_value", "value_type"];

    private static class ColumnIndex
    {
        public const int Name = 0;
        public const int Value = 1;
        public const int ValueType = 2;
    }

    static KeyValueSettingDal()
    {
        Debug.Assert(ColumnIndex.ValueType + 1 == s_columns.Length,
            "ColumnIndex and s_columns are out of sync by column count");
        Debug.Assert(s_columns[ColumnIndex.Name] == "setting_name", "ColumnIndex.Name does not match s_columns");
        Debug.Assert(s_columns[ColumnIndex.Value] == "setting_value", "ColumnIndex.Value does not match s_columns");
        Debug.Assert(s_columns[ColumnIndex.ValueType] == "value_type", "ColumnIndex.ValueType does not match s_columns");
    }

    /// <summary>
    /// Gets the name of the table that stores the settings.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Gets the name of the owner id column of the settings table.
    /// </summary>
    public required string IdName { get; init; }

    /// <summary>
    /// Loads the settings stored for the specified owner.
    /// </summary>
    /// <param name="context">The database context, or <see langword="null"/> to create an owned one.</param>
    /// <param name="id">The owner row identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The settings of the owner; an empty list when no rows are stored.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is zero or negative.</exception>
    public ValueTask<List<KeyValueSetting>> LoadSettingsAsync(SQLiteDbContext? context, long id, CancellationToken cancellationToken)
    {
        Debug.Assert(id > 0);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        return ExecuteInDbContextAsync(context, static (ctx, state) =>
        {
            state.CancellationToken.ThrowIfCancellationRequested();

            using var command = ctx.Connection.CreateCommandSelect(state.TableName, s_columns, $"WHERE {state.IdName}=@{state.IdName}",
                DbType.Int64.CreateInputParam($"@{state.IdName}", state.Id));

            var list = new List<KeyValueSetting>();
            int count = ctx.Connection.ExecuteReader(command, Read, list, cancellationToken: state.CancellationToken);
            Debug.Assert(list.Count == count);

            return list;

        }, (TableName, IdName, Id: id, CancellationToken: cancellationToken), cancellationToken);

        static void Read(SQLiteDataReader reader, List<KeyValueSetting> list)
        {
            list.Add(ReadSetting(reader));
        }

        static KeyValueSetting ReadSetting(SQLiteDataReader reader)
        {
            return new KeyValueSetting
            {
                Name = reader.GetString(ColumnIndex.Name),
                Value = reader.GetString(ColumnIndex.Value),
                ValueType = SettingValueTypes.ToType(reader.GetString(ColumnIndex.ValueType))
            };
        }
    }

    /// <summary>
    /// Saves the specified settings for the owner.
    /// </summary>
    /// <param name="context">The database context, or <see langword="null"/> to create an owned one.</param>
    /// <param name="id">The owner row identifier.</param>
    /// <param name="settings">The settings to persist.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of written rows.</returns>
    /// <remarks>
    /// Only rows that differ from the stored values are written, so the result can be smaller than the number of
    /// settings, and can be zero when nothing changed.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="id"/> is zero or negative.</exception>
    public ValueTask<int> SaveSettingsAsync(SQLiteDbContext? context, long id, ICollection<KeyValueSetting> settings, CancellationToken cancellationToken)
    {
        Debug.Assert(id > 0);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);

        Debug.Assert(settings != null);
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.Count == 0)
            return ValueTask.FromResult(0);

        return ExecuteInDbContextAsync(context, static (ctx, state) =>
        {
            // The owner id column is part of the inserted row, so it must be present in the field list
            // and stay first, matching the order of the parameters below.
            string[] columns = [state.IdName, .. s_columns];

            // Rows already stored for this owner. IsChanged alone is not enough to decide what to write:
            // a newly created setting has never been persisted, yet IsChanged is false for it. Comparing
            // the stored values as well makes the decision independent of the change tracking state.
            var persisted = new Dictionary<string, (string Value, string ValueType)>(StringComparer.Ordinal);
            using (var selectCommand = ctx.Connection.CreateCommandSelect(state.TableName, s_columns,
                       $"WHERE {state.IdName}=@{state.IdName}",
                       DbType.Int64.CreateInputParam($"@{state.IdName}", state.Id)))
            {
                ctx.Connection.ExecuteReader(selectCommand,
                    static (reader, map) => map[reader.GetString(ColumnIndex.Name)] =
                        (reader.GetString(ColumnIndex.Value), reader.GetString(ColumnIndex.ValueType)),
                    persisted, cancellationToken: state.CancellationToken);
            }

            int num = 0;
            using (SQLiteCommand command = ctx.Connection.CreateCommandInsertOrReplace(state.TableName, columns,
                       DbType.Int64.CreateInputParam($"@{state.IdName}", state.Id),
                       DbType.String.CreateInputParam("@setting_name", ""),
                       DbType.String.CreateInputParam("@setting_value", ""),
                       DbType.String.CreateInputParam("@value_type", "")
                   ))
            {
                foreach (var setting in state.Settings)
                {
                    state.CancellationToken.ThrowIfCancellationRequested();

                    string valueType = SettingValueTypes.ToToken(setting.ValueType);

                    if (!setting.IsChanged &&
                        persisted.TryGetValue(setting.Name, out var stored) &&
                        string.Equals(stored.Value, setting.Value, StringComparison.Ordinal) &&
                        string.Equals(stored.ValueType, valueType, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    command.Parameters["@setting_name"].Value = setting.Name;
                    command.Parameters["@setting_value"].Value = setting.Value;
                    command.Parameters["@value_type"].Value = valueType;

                    int affected = ctx.Connection.ExecuteNonQuery(command, state.CancellationToken);
                    Debug.Assert(affected == 1, "affected: " + affected);
                    num += affected;
                }
            }

            return num;
        }, (TableName, IdName, Id: id, Settings: settings, CancellationToken: cancellationToken), cancellationToken);
    }
}
