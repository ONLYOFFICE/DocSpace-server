// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

namespace ASC.Data.Backup.Tasks;

[Scope]
public class DbFactory(IConfiguration configuration, ConfigurationExtension configurationExtension)
{
    public const string DefaultConnectionStringName = "default";

    internal string ConnectionStringSettings(string key = null, string connectionString = null, string region = "current")
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        return key != null ? configurationExtension.GetConnectionStrings(key, region).ConnectionString : configurationExtension.GetConnectionStrings(DefaultConnectionStringName, region).ConnectionString;
    }

    private DbProviderFactory DbProviderFactory
    {
        get
        {
            if (field == null)
            {
                var type = Type.GetType(configuration["DbProviderFactories:mysql:type"], true);
                field = (DbProviderFactory)Activator.CreateInstance(type, true);
            }

            return field;
        }
    }

    public DbConnection OpenConnection(string path = "default", string connectionString = null, string region = "current")
    {
        var connection = DbProviderFactory.CreateConnection();
        if (connection != null)
        {
            connection.ConnectionString = EnsureConnectionTimeout(ConnectionStringSettings(path, connectionString, region));
            connection.Open();
        }

        return connection;
    }

    public IDbDataAdapter CreateDataAdapter()
    {
        var result = DbProviderFactory.CreateDataAdapter();
        if (result == null && DbProviderFactory is MySqlClientFactory)
        {
            result = new MySqlDataAdapter();
        }

        return result;
    }

    public DbCommand CreateLastInsertIdCommand()
    {
        var command = DbProviderFactory.CreateCommand();
        command?.CommandText = configurationExtension.GetConnectionStrings(DefaultConnectionStringName).ProviderName.Contains("MySql", StringComparison.OrdinalIgnoreCase)
            ? "select Last_Insert_Id();"
            : "select last_insert_rowid();";

        return command;
    }

    public DbCommand CreateShowColumnsCommand(string tableName)
    {
        var command = DbProviderFactory.CreateCommand();
        command?.CommandText = "show columns from " + tableName + ";";

        return command;
    }

    private static string EnsureConnectionTimeout(string connectionString)
    {
        if (!connectionString.Contains("Connection Timeout"))
        {
            connectionString = connectionString.TrimEnd(';') + ";Connection Timeout=90";
        }

        return connectionString;
    }
}