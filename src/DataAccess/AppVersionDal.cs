using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Minimal.Mvvm.Persistence;

/// <summary>
/// Stores application versions that own application settings. Settings of different application
/// versions are kept apart, so a downgrade or upgrade never mixes incompatible setting sets.
/// </summary>
public sealed class AppVersionDal(Func<SQLiteDbConnection> createConnection) : SQLiteDalBase(createConnection)
{
    /// <summary>
    /// Name of the table that stores application versions.
    /// </summary>
    public const string TableName = "app_version";

    private static readonly string[] s_columns = ["id", "app_version", "created_utc"];

    private static class ColumnIndex
    {
        public const int Id = 0;
        public const int AppVersion = 1;
        public const int CreatedUtc = 2;
    }

    static AppVersionDal()
    {
        Debug.Assert(ColumnIndex.CreatedUtc + 1 == s_columns.Length,
            "ColumnIndex and s_columns are out of sync by column count");
        Debug.Assert(s_columns[ColumnIndex.Id] == "id", "ColumnIndex.Id does not match s_columns");
        Debug.Assert(s_columns[ColumnIndex.AppVersion] == "app_version", "ColumnIndex.AppVersion does not match s_columns");
        Debug.Assert(s_columns[ColumnIndex.CreatedUtc] == "created_utc", "ColumnIndex.CreatedUtc does not match s_columns");
    }

    /// <summary>
    /// Finds the identifier of the specified application version.
    /// </summary>
    /// <param name="context">The database context, or <see langword="null"/> to create an owned one.</param>
    /// <param name="appVersion">The application version key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The version identifier, or <see langword="null"/> when the version is not stored.</returns>
    public ValueTask<long?> FindIdAsync(SQLiteDbContext? context, string appVersion, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appVersion);

        return ExecuteInDbContextAsync(context, static (ctx, state) =>
        {
            var (appVersion, cancellationToken) = state;

            using var command = ctx.Connection.CreateCommandSelect(TableName, [s_columns[ColumnIndex.Id]],
                "WHERE app_version=@app_version LIMIT 1",
                DbType.String.CreateInputParam("@app_version", appVersion));

            object? id = ctx.Connection.ExecuteScalar(command, cancellationToken);
            return id is null or DBNull ? (long?)null : Convert.ToInt64(id);
        }, (appVersion, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Gets the identifier of the most recently created application version.
    /// </summary>
    /// <param name="context">The database context, or <see langword="null"/> to create an owned one.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The latest version identifier, or <see langword="null"/> when no versions are stored.</returns>
    public ValueTask<long?> GetLatestIdAsync(SQLiteDbContext? context, CancellationToken cancellationToken)
    {
        return ExecuteInDbContextAsync(context, static (ctx, cancellationToken) =>
        {
            using var command = ctx.Connection.CreateCommandSelect(TableName, [s_columns[ColumnIndex.Id]], "ORDER BY id DESC LIMIT 1");

            object? id = ctx.Connection.ExecuteScalar(command, cancellationToken);
            return id is null or DBNull ? (long?)null : Convert.ToInt64(id);
        }, cancellationToken, cancellationToken);
    }

    /// <summary>
    /// Creates a new application version row.
    /// </summary>
    /// <param name="context">The database context, or <see langword="null"/> to create an owned one.</param>
    /// <param name="appVersion">The application version key.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The identifier of the created version.</returns>
    public ValueTask<long> CreateAsync(SQLiteDbContext? context, string appVersion, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(appVersion);

        return ExecuteInDbContextAsync(context, static (ctx, state) =>
        {
            var (appVersion, cancellationToken) = state;

            // RETURNING id (SQLite 3.35+) returns the generated identifier as the insert result,
            // so there is no need for a separate LastInsertRowId round-trip.
            using var command = ctx.Connection.CreateCommandInsert(TableName, [s_columns[ColumnIndex.AppVersion]],
                "RETURNING id",
                DbType.String.CreateInputParam("@app_version", appVersion));

            object? id = ctx.Connection.ExecuteScalar(command, cancellationToken);
            Throw.InvalidOperationExceptionIf(id is null or DBNull, "Insert did not return the app version identifier.");

            return Convert.ToInt64(id);
        }, (appVersion, cancellationToken), cancellationToken);
    }
}
