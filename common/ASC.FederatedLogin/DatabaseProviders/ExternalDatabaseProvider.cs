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

using MySqlConnector;

namespace ASC.FederatedLogin.DatabaseProviders;

[Scope]
public class ExternalDatabaseProvider : Consumer, IExternalDatabaseProvider, IValidateKeysProvider
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
            "sqlite" => !string.IsNullOrWhiteSpace(SqliteFilePath),
            _ => false
        };
    }

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
            UseSsl = bool.TryParse(UseSsl, out var ssl) && ssl
        };
    }

    public DbConnection CreateConnection()
    {
        switch (DatabaseType?.ToLowerInvariant())
        {
            case "mysql":
                return CreateMySqlConnection();
            case "sqlite":
                throw new NotImplementedException("SQLite support will be added in the future.");
            default:
                throw new NotSupportedException($"Database type '{DatabaseType}' is not supported yet.");
        }
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
}