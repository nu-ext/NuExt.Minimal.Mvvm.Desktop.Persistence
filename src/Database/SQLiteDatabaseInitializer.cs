using System;
using System.Data.SQLite;
using System.IO;
using System.Reflection;

namespace Minimal.Mvvm.Persistence;

/// <summary>
/// Bootstraps a SQLite application database: prepares the database directory, exposes a connection factory,
/// and builds the migration plan of a single scope.
/// </summary>
/// <remarks>
/// Migrations are not applied by this class: <see cref="CreateMigrationPlan"/> returns a plan that the caller can
/// extend with additional scopes and apply inside its own transaction. The scope identifier and the migrations are
/// supplied by the consumer; only the recorded version has a default, which is the entry assembly version.
/// </remarks>
public class SQLiteDatabaseInitializer
{
    private readonly string _scope;
    private readonly SQLiteDbMigration[] _migrations;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLiteDatabaseInitializer"/> class.
    /// </summary>
    /// <param name="connectionString">The SQLite connection string of the database to initialize.</param>
    /// <param name="scope">The stable migration scope identifier.</param>
    /// <param name="migrations">The migrations in the order in which they must be applied.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="connectionString"/> or <paramref name="scope"/> is <see langword="null"/>,
    /// empty, or consists only of white-space characters.
    /// </exception>
    /// <exception cref="ArgumentNullException"><paramref name="migrations"/> is <see langword="null"/>.</exception>
    public SQLiteDatabaseInitializer(string connectionString, string scope, params SQLiteDbMigration[] migrations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentNullException.ThrowIfNull(migrations);

        ConnectionString = connectionString;
        _scope = scope;
        _migrations = migrations;
    }

    // Properties

    /// <summary>
    /// Gets the SQLite connection string of the database.
    /// </summary>
    public string ConnectionString { get; }

    // Methods

    /// <summary>
    /// Creates a new connection to the database.
    /// </summary>
    /// <returns>A new <see cref="SQLiteDbConnection"/> instance.</returns>
    public SQLiteDbConnection CreateConnection() => new(ConnectionString);

    /// <summary>
    /// Creates the directory that contains the database file when it does not exist.
    /// </summary>
    public void EnsureDatabaseDirectory()
    {
        var csb = new SQLiteConnectionStringBuilder(ConnectionString);
        string? databaseDirectory = Path.GetDirectoryName(Path.GetFullPath(csb.DataSource));
        if (!string.IsNullOrEmpty(databaseDirectory))
        {
            IOUtils.CheckDirectory(databaseDirectory, true);
        }
    }

    /// <summary>
    /// Creates and validates the migration plan without touching the database.
    /// </summary>
    /// <returns>The migration plan with the registered migration scope.</returns>
    /// <exception cref="System.Data.DbMigrationException">
    /// The migrations are invalid or declare invalid dependencies.
    /// </exception>
    public SQLiteDbMigrationPlan CreateMigrationPlan()
        => new SQLiteDbMigrationPlan().AddScope(_scope, _migrations, GetVersion());

    /// <summary>
    /// Gets the version recorded for each applied migration.
    /// </summary>
    /// <returns>The recorded version, or <see langword="null"/> when the version is not recorded.</returns>
    /// <remarks>
    /// The default implementation returns the version of the entry assembly, so a consumer that is
    /// started as an executable records its own version rather than the version of this package.
    /// </remarks>
    protected virtual string? GetVersion() => AssemblyInfo.Current.Version?.ToString();
}