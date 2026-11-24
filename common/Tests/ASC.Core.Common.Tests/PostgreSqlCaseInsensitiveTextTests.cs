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
/// The options every service builds its contexts with (<c>InstallerOptionsAction</c>), for a PostgreSQL connection
/// string, over the whole model the migrations are made from: a text column that ignores the case on MySQL (a _ci
/// collation) is citext on PostgreSQL, so a lookup, a sort and a unique index ignore the case there too. The model is
/// built without opening a connection.
/// </summary>
public class PostgreSqlCaseInsensitiveTextTests
{
    private static ASC.Migrations.Core.MigrationContext CreateContext(string providerName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:default:name"] = "default",
                ["ConnectionStrings:default:connectionString"] = providerName == "Npgsql"
                    ? "Host=localhost;Database=docspace"
                    : "Server=localhost;Database=docspace",
                ["ConnectionStrings:default:providerName"] = providerName,
                ["mysqlServerVersion"] = "8.4.0"
            })
            .Build();

        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(configuration)
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddSingleton<ASC.Common.Logging.EFLoggerFactory>()
            .BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<ASC.Migrations.Core.MigrationContext>();
        new ASC.Core.Common.EF.InstallerOptionsAction("current", "default").OptionsAction(services, optionsBuilder);

        return new ASC.Migrations.Core.MigrationContext(optionsBuilder.Options);
    }

    /// <summary>The model with its design-time annotations (collations, extensions), which the runtime model drops.</summary>
    private static IModel DesignTimeModel(DbContext context)
    {
        return context.GetService<IDesignTimeModel>().Model;
    }

    private static List<IProperty> TextProperties(IModel model)
    {
        return model.GetEntityTypes()
            .Where(entityType => entityType.GetTableName() is not null)
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => (property.GetValueConverter()?.ProviderClrType ?? property.ClrType) == typeof(string))
            .ToList();
    }

    /// <summary>
    /// Whether the MySQL column compares letters whatever their case: a character type whose collation (its own, or its
    /// table's, or the database's utf8mb4 default) is a _ci one. A json column compares as written, a _bin one too.
    /// </summary>
    private static bool IgnoresCaseOnMySql(IProperty property)
    {
        var storeType = property.GetRelationalTypeMapping().StoreType.ToLowerInvariant();

        if (!(storeType.Contains("char") || storeType.Contains("text") || storeType.StartsWith("enum", StringComparison.Ordinal)))
        {
            return false;
        }

        var collation = property.GetCollation() ?? (string?)property.DeclaringType.FindAnnotation(RelationalAnnotationNames.Collation)?.Value;

        return collation is null || !collation.EndsWith("_bin", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PostgreSql_ATextColumn_IsCitextUnlessItTellsTheCaseApartOnMySql_AndTheExtensionIsCreated()
    {
        using var context = CreateContext("Npgsql");

        DesignTimeModel(context).GetPostgresExtensions().Select(extension => extension.Name).Should().Contain(ASC.Core.Common.EF.PostgreSqlModelCustomizer.CaseInsensitiveText);

        var properties = TextProperties(DesignTimeModel(context));

        properties.Should().Contain(property => property.GetColumnType() == ASC.Core.Common.EF.PostgreSqlModelCustomizer.CaseInsensitiveText);

        var mismatches = properties
            .Where(property =>
            {
                var expected = !ASC.Core.Common.EF.PostgreSqlModelCustomizer.CaseSensitive(property.DeclaringType.GetTableName()!, property.GetColumnName())
                    && property.GetColumnType() is not ("json" or "jsonb");

                return (property.GetColumnType() == ASC.Core.Common.EF.PostgreSqlModelCustomizer.CaseInsensitiveText) != expected;
            })
            .Select(property => $"{property.DeclaringType.GetTableName()}.{property.GetColumnName()} is {property.GetColumnType() ?? "no type"}")
            .ToList();

        mismatches.Should().BeEmpty(string.Join("; ", mismatches));
    }

    /// <summary>
    /// The citext columns are exactly those that ignore the case on MySQL: a new MySQL column with a _bin collation that is
    /// not added to <c>CaseSensitive</c>, or a text column that is json on one database only, would compare differently.
    /// </summary>
    [Fact]
    public void Citext_IsExactlyTheColumnsThatIgnoreTheCaseOnMySql()
    {
        using var mySql = CreateContext("MySql.Data.MySqlClient");
        using var postgreSql = CreateContext("Npgsql");

        var mismatches = new List<string>();

        var mySqlModel = DesignTimeModel(mySql);

        // A json or jsonb column holds a document on PostgreSQL (webstudio_settings.data is mediumtext on MySQL): it is
        // not text there, and the test above checks that it is left alone.
        foreach (var property in TextProperties(DesignTimeModel(postgreSql)).Where(property => property.GetColumnType() is not ("json" or "jsonb")))
        {
            var mySqlProperty = mySqlModel.FindEntityType(property.DeclaringType.Name)?.FindProperty(property.Name);

            if (mySqlProperty is null)
            {
                mismatches.Add($"{property.DeclaringType.GetTableName()}.{property.GetColumnName()} is not in the MySQL model");
                continue;
            }

            var citext = property.GetColumnType() == ASC.Core.Common.EF.PostgreSqlModelCustomizer.CaseInsensitiveText;

            if (citext != IgnoresCaseOnMySql(mySqlProperty))
            {
                mismatches.Add($"{property.DeclaringType.GetTableName()}.{property.GetColumnName()} is {property.GetColumnType() ?? "no type"} here, {mySqlProperty.GetRelationalTypeMapping().StoreType} {mySqlProperty.GetCollation()} on MySQL");
            }
        }

        mismatches.Should().BeEmpty(string.Join("; ", mismatches));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("text", true)]
    [InlineData("varchar", true)]
    [InlineData("varchar(255)", true)]
    [InlineData("character varying(38)", true)]
    [InlineData("char(32)", true)]
    [InlineData("citext", false)]
    [InlineData("jsonb", false)]
    [InlineData("json", false)]
    [InlineData("uuid", false)]
    public void IsText_ColumnType(string? columnType, bool expected)
    {
        ASC.Core.Common.EF.PostgreSqlModelCustomizer.IsText(columnType!).Should().Be(expected);
    }
}
