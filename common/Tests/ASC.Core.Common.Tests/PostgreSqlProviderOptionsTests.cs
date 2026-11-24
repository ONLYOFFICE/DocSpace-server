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
/// The Npgsql options every service builds its contexts with (<c>InstallerOptionsAction</c>) that are not about one
/// column or function. No connection is opened.
/// </summary>
public class PostgreSqlProviderOptionsTests
{
    /// <summary>
    /// The execution strategy retries, as the MySQL one does. A retrying strategy is also what makes EF buffer a result
    /// before handing it over: the DAOs run a query while they still iterate another on the same context (the folder
    /// counts after a move), which Npgsql refuses on a streamed result.
    /// </summary>
    [Fact]
    public void ExecutionStrategy_RetriesOnFailure()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:default:name"] = "default",
                ["ConnectionStrings:default:connectionString"] = "Host=localhost;Database=docspace",
                ["ConnectionStrings:default:providerName"] = "Npgsql"
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<ASC.Common.Logging.EFLoggerFactory>()
            .BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<ASC.Core.Common.EF.Context.WebstudioDbContext>();
        new ASC.Core.Common.EF.InstallerOptionsAction("current", "default").OptionsAction(services, optionsBuilder);

        using var context = new ASC.Core.Common.EF.Context.WebstudioDbContext(optionsBuilder.Options);

        context.Database.CreateExecutionStrategy().RetriesOnFailure.Should().BeTrue();
    }

    /// <summary>
    /// The PostgreSQL Standalone snapshot is the SaaS one with the namespace changed: the Standalone context has the
    /// whole model, and Migrate() refuses to run while its snapshot differs from it (PendingModelChangesWarning), which
    /// only a Standalone stack on PostgreSQL would show. The db-migrations skill has the copy made after every PostgreSQL
    /// SaaS migration; this is what notices when it was not.
    /// </summary>
    [Fact]
    public void StandaloneSnapshot_IsTheSaaSOneWithItsOwnNamespace()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);

        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "migrations", "postgre")))
        {
            root = root.Parent;
        }

        root.Should().NotBeNull("the test runs from a checkout of the server repository");

        static string Read(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

        var saas = Read(Path.Combine(root!.FullName, "migrations", "postgre", "SaaS", "MigrationContext", "MigrationContextModelSnapshot.cs"));
        var standalone = Read(Path.Combine(root.FullName, "migrations", "postgre", "Standalone", "MigrationContext", "MigrationContextModelSnapshot.cs"));

        standalone.Should().Be(saas.Replace("namespace ASC.Migrations.PostgreSql.SaaS.Migrations", "namespace ASC.Migrations.PostgreSql.Standalone.Migrations"));
    }
}
