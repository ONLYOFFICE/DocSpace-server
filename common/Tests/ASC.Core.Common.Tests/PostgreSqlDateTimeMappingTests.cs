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
/// string, over the whole model the migrations are made from: Npgsql writes to timestamptz only a DateTime of Kind Utc
/// and to timestamp (without time zone) only one that is not Utc, and the code passes values of every Kind (a portal
/// registration writes the owner's WorkFromDate in the tenant's time zone, Kind Local; the AI storage writes
/// DateTime.UtcNow into timestamp columns). The model is built without opening a connection.
/// </summary>
public class PostgreSqlDateTimeMappingTests
{
    private static readonly DateTime[] _values =
    [
        new(2026, 11, 22, 15, 30, 0, DateTimeKind.Local),
        new(2026, 11, 22, 15, 30, 0, DateTimeKind.Unspecified),
        new(2026, 11, 22, 15, 30, 0, DateTimeKind.Utc)
    ];

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

    [Fact]
    public void PostgreSql_EveryDateTimeProperty_StoresTheValueAsItIsInTheKindItsColumnTakes()
    {
        using var context = CreateContext("Npgsql");

        var dateTimeProperties = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
            .ToList();

        // Both kinds of column are there, so neither branch of the converter choice goes untested.
        dateTimeProperties.Should().Contain(property => !ASC.Core.Common.EF.PostgreSqlModelCustomizer.IsWithoutTimeZone(property.GetColumnType()));
        dateTimeProperties.Should().Contain(property => ASC.Core.Common.EF.PostgreSqlModelCustomizer.IsWithoutTimeZone(property.GetColumnType()));

        foreach (var property in dateTimeProperties)
        {
            var name = $"{property.DeclaringType.DisplayName()}.{property.Name} ({property.GetColumnType() ?? "no type"})";
            var withoutTimeZone = ASC.Core.Common.EF.PostgreSqlModelCustomizer.IsWithoutTimeZone(property.GetColumnType());
            var converter = property.GetValueConverter();

            converter.Should().NotBeNull(name);

            foreach (var value in _values)
            {
                var stored = (DateTime)converter!.ConvertToProvider(value)!;
                stored.Kind.Should().Be(withoutTimeZone ? DateTimeKind.Unspecified : DateTimeKind.Utc, name);
                stored.Ticks.Should().Be(value.Ticks, $"{name}: the value is stored as it is, as on MySQL, not shifted");

                var read = (DateTime)converter.ConvertFromProvider(stored)!;
                read.Kind.Should().Be(DateTimeKind.Unspecified, name);
                read.Ticks.Should().Be(value.Ticks, name);
            }
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("timestamptz", false)]
    [InlineData("timestamp with time zone", false)]
    [InlineData("timestamp", true)]
    [InlineData("timestamp without time zone", true)]
    [InlineData("timestamp(6)", true)]
    [InlineData("date", false)]
    public void IsWithoutTimeZone_ColumnType(string? columnType, bool expected)
    {
        ASC.Core.Common.EF.PostgreSqlModelCustomizer.IsWithoutTimeZone(columnType).Should().Be(expected);
    }

    [Fact]
    public void MySql_DateTimeProperties_KeepNoConverter()
    {
        using var context = CreateContext("MySql.Data.MySqlClient");

        var workFromDate = context.Model.FindEntityType(typeof(ASC.Core.Common.EF.User))!
            .FindProperty(nameof(ASC.Core.Common.EF.User.WorkFromDate))!;

        workFromDate.GetValueConverter().Should().BeNull();
    }
}
