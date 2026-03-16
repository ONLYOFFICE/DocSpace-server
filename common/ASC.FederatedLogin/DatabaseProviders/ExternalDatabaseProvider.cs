// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Data;
using System.Data.Common;

using Microsoft.Data.Sqlite;

using MySqlConnector;

#nullable enable
namespace ASC.FederatedLogin.DatabaseProviders;

[Scope]
public class ExternalDatabaseProvider : Consumer, IExternalDatabaseProvider, IValidateKeysProvider, IConsumerKeyMetadataProvider
{
    public string DatabaseType => this["databaseType"] ?? "mysql";
    public string Host => this["dbHost"];
    public string Port => this["dbPort"] ?? "3306";
    public string Database => this["dbName"];
    public string User => this["dbUser"];
    public string Password => this["dbPassword"];
    public string UseSsl => this["dbSsl"] ?? "false";
    public string SqliteFilePath => this["sqliteFilePath"];

    public bool IsEnabled()
    {
        if (string.IsNullOrWhiteSpace(DatabaseType))
        {
            return false;
        }

        return DatabaseType.ToLowerInvariant() switch
        {
            "mysql" => !string.IsNullOrWhiteSpace(Host) &&
                       !string.IsNullOrWhiteSpace(Database) &&
                       !string.IsNullOrWhiteSpace(User),
            "sqlite" => IsSqliteAllowed && !string.IsNullOrWhiteSpace(SqliteFilePath),
            _ => false
        };
    }

    private bool IsSqliteAllowed => CoreBaseSettings.Standalone;

    public AuthKeyMetadata GetKeyMetadata(string key) => key switch
    {
        "databaseType"   => new() { Order = 0, Type = "select",   Options = IsSqliteAllowed ? ["mysql", "sqlite"] : ["mysql"] },
        "dbHost"         => new() { Order = 1, DependsOn = "databaseType", DependsOnValue = "mysql" },
        "dbPort"         => new() { Order = 2, Type = "number", DependsOn = "databaseType", DependsOnValue = "mysql" },
        "dbName"         => new() { Order = 3, DependsOn = "databaseType", DependsOnValue = "mysql" },
        "dbUser"         => new() { Order = 4, DependsOn = "databaseType", DependsOnValue = "mysql" },
        "dbPassword"     => new() { Order = 5, Type = "password", DependsOn = "databaseType", DependsOnValue = "mysql" },
        "dbSsl"          => new() { Order = 6, Type = "toggle",   DependsOn = "databaseType", DependsOnValue = "mysql" },
        "sqliteFilePath" => new() { Order = 7, DependsOn = "databaseType", DependsOnValue = "sqlite" },
        _                => new()
    };

    public ExternalDatabaseProvider() { }

    public ExternalDatabaseProvider(
        TenantManager tenantManager,
        CoreBaseSettings coreBaseSettings,
        CoreSettings coreSettings,
        IConfiguration configuration,
        ICacheNotify<ConsumerCacheItem> cache,
        ConsumerFactory consumerFactory,
        string name,
        int order,
        bool paid,
        Dictionary<string, string> props,
        Dictionary<string, string>? additional = null)
        : base(tenantManager, coreBaseSettings, coreSettings,
              configuration, cache, consumerFactory,
              name, order, paid, props, additional)
    { }
    public Task<bool> ValidateKeysAsync()
        => ValidateConnectionAsync();

    public async Task<bool> ValidateConnectionAsync()
    {
        if (!IsEnabled())
        {
            return false;
        }

        if (DatabaseType?.ToLowerInvariant() == "sqlite")
        {
            var path = ValidateSqlitePath(SqliteFilePath, Configuration);
            if (!File.Exists(path))
            {
                return false;
            }
        }

        try
        {
            await using var connection = CreateConnection();
            await connection.OpenAsync();
            return connection.State == ConnectionState.Open;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> TestConnectionAsync()
        => ValidateConnectionAsync();

    public static async Task<ConnectionTestResult> TestConnectionAsync(ExternalDatabaseSettings settings, IConfiguration configuration)
    {
        try
        {
            if (settings.DatabaseType?.ToLowerInvariant() == "sqlite" &&
                !File.Exists(ValidateSqlitePath(settings.SqliteFilePath, configuration)))
            {
                return ConnectionTestResult.Failure("SQLite file not found.");
            }

            await using var connection = CreateConnection(settings, configuration);
            await connection.OpenAsync();
            return connection.State == ConnectionState.Open
                ? ConnectionTestResult.Ok()
                : ConnectionTestResult.Failure("Connection did not open.");
        }
        catch (Exception ex)
        {
            return ConnectionTestResult.Failure(ex.Message);
        }
    }

    public ExternalDatabaseSettings GetSettings()
    {
        return new ExternalDatabaseSettings
        {
            DatabaseType = DatabaseType,
            Host = Host,
            Port = int.TryParse(Port, out var portValue) ? portValue : 3306,
            DatabaseName = Database,
            User = User,
            Password = Password,
            UseSsl = bool.TryParse(UseSsl, out var ssl) && ssl,
            SqliteFilePath = SqliteFilePath
        };
    }

    public DbConnection CreateConnection()
    {
        return DatabaseType?.ToLowerInvariant() switch
        {
            "mysql" => CreateMySqlConnection(),
            "sqlite" => CreateSqliteConnection(ValidateSqlitePath(SqliteFilePath, Configuration)),
            _ => throw new NotSupportedException($"Database type '{DatabaseType}' is not supported yet.")
        };
    }

    public static DbConnection CreateConnection(ExternalDatabaseSettings settings, IConfiguration configuration)
    {
        return settings.DatabaseType?.ToLowerInvariant() switch
        {
            "mysql" => CreateMySqlConnection(settings),
            "sqlite" => CreateSqliteConnection(ValidateSqlitePath(settings.SqliteFilePath, configuration)),
            _ => throw new NotSupportedException($"Database type '{settings.DatabaseType}' is not supported yet.")
        };
    }

    private static string ValidateSqlitePath(string fileName, IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("SQLite file name is not configured.");
        }

        var storageRoot = configuration["$STORAGE_ROOT"];
        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            throw new InvalidOperationException("$STORAGE_ROOT is not configured.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(storageRoot, fileName));
        var normalizedRoot = Path.GetFullPath(storageRoot);

        if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !fullPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("SQLite path is outside the storage root.");
        }

        return fullPath;
    }

    private DbConnection CreateMySqlConnection()
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Host,
            Database = Database,
            UserID = User,
            Password = Password
        };

        builder.Port = uint.TryParse(Port, out var port) ? port : 3306;

        builder.SslMode = bool.TryParse(UseSsl, out var useSsl) && useSsl
            ? MySqlSslMode.Preferred
            : MySqlSslMode.None;

        return new MySqlConnection(builder.ConnectionString);
    }

    private static DbConnection CreateMySqlConnection(ExternalDatabaseSettings settings)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = settings.Host,
            Database = settings.DatabaseName,
            UserID = settings.User,
            Password = settings.Password,
            Port = (uint)settings.Port,
            SslMode = settings.UseSsl ? MySqlSslMode.Preferred : MySqlSslMode.None
        };

        return new MySqlConnection(builder.ConnectionString);
    }

    private static DbConnection CreateSqliteConnection(string filePath, SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Mode = mode
        }.ToString();

        return new SqliteConnection(connectionString);
    }
}