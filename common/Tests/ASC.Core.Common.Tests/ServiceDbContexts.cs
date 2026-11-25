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

namespace ASC.Core.Common.Tests;

/// <summary>
/// A context built with the options every service builds its contexts with (<c>InstallerOptionsAction</c>), for a
/// connection string of the given provider. No connection is opened: the tests read the model or the SQL text.
/// </summary>
internal static class ServiceDbContexts
{
    public const string PostgreSql = "Npgsql";

    public const string MySql = "MySql.Data.MySqlClient";

    public static TContext Create<TContext>(string providerName) where TContext : DbContext
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:default:name"] = "default",
                ["ConnectionStrings:default:connectionString"] = providerName == PostgreSql
                    ? "Host=localhost;Database=docspace"
                    : "Server=localhost;Database=docspace",
                ["ConnectionStrings:default:providerName"] = providerName,
                // The MySQL provider asks the server for its version unless the configuration names one.
                ["mysqlServerVersion"] = "8.4.0"
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<ASC.Common.Logging.EFLoggerFactory>()
            .BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        new ASC.Core.Common.EF.InstallerOptionsAction("current", "default").OptionsAction(services, optionsBuilder);

        return (TContext)Activator.CreateInstance(typeof(TContext), optionsBuilder.Options)!;
    }
}
