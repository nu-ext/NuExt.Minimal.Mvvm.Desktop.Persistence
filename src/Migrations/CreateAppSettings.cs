using System;
using System.Data.SQLite;

namespace Minimal.Mvvm.Persistence.Migrations;

/// <summary>
/// Creates the application settings storage: the owner table that separates settings sets by
/// application version, the key/value settings table, and the initial owner row.
/// </summary>
/// <remarks>
/// Settings have the same shape as other key/value settings tables, so a single settings DAL
/// serves all of them: the application version row acts as the owner of the settings, which keeps
/// setting sets of different application versions from conflicting.
/// </remarks>
public class CreateAppSettings : SQLiteDbMigration
{
    /// <inheritdoc />
    public override string Id => "0001_CreateAppSettings";

    /// <summary>
    /// Gets the application version stored in the initial version row.
    /// </summary>
    /// <returns>The application version key, escaped for use in a SQL literal.</returns>
    /// <remarks>
    /// Override to store a different version key, for example when the settings set must be bound
    /// to a version other than the assembly version of the running application.
    /// </remarks>
    protected virtual string GetAppVersion()
    {
        // The version row belongs to the application version that creates the database,
        // so the initial settings set is bound to the actual version instead of a placeholder.
        return AppVersionKey.Current.Replace("'", "''");
    }

    /// <inheritdoc />
    public override void Up(SQLiteDbMigrationContext context)
    {
        string appVersion = GetAppVersion();
        Throw.InvalidOperationExceptionIf(string.IsNullOrWhiteSpace(appVersion),
            $"{nameof(GetAppVersion)} must return a non-empty application version.");

        context.ExecuteNonQuery(
            $"""
            CREATE TABLE app_version (                              -- Application version
                id              INTEGER PRIMARY KEY,                -- Internal ID
                app_version     TEXT NOT NULL                       -- Application version (major.minor)
                                CHECK(length(trim(app_version)) > 0),
                created_utc     DATETIME NOT NULL                   -- Record creation time
                                DEFAULT CURRENT_TIMESTAMP,

                UNIQUE (app_version)
            );

            INSERT INTO app_version (app_version) VALUES ('{appVersion}');

            CREATE TABLE app_setting (                              -- Application settings
                app_version_id      INTEGER NOT NULL,               -- Application version
                setting_name        TEXT NOT NULL                   -- Setting name
                                    CHECK(length(trim(setting_name)) > 0),
                setting_value       TEXT NOT NULL,                  -- Setting value
                value_type          TEXT NOT NULL DEFAULT 'string'  -- Value type
                                    CHECK(value_type IN (
                                        'string',
                                        'integer',
                                        'real',
                                        'boolean',
                                        'date',
                                        'datetime',
                                        'json'
                                    )),

                PRIMARY KEY (app_version_id, setting_name),

                FOREIGN KEY (app_version_id)
                    REFERENCES app_version (id)
                    ON DELETE CASCADE
            );
            """);
    }
}
