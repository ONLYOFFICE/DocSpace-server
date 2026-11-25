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
/// DateTime.UtcNow into timestamp columns). A column whose MySQL type has no fraction of a second has none here
/// either, so the database rounds the value as MySQL does. The model is built without opening a connection.
/// </summary>
public class PostgreSqlDateTimeMappingTests
{
    private static readonly DateTime[] _values =
    [
        new DateTime(2026, 11, 22, 15, 30, 0, DateTimeKind.Local).AddTicks(4_999_999),
        new DateTime(2026, 11, 22, 15, 30, 0, DateTimeKind.Unspecified).AddTicks(5_000_000),
        new DateTime(2026, 11, 22, 15, 30, 0, DateTimeKind.Utc).AddTicks(1_234_567)
    ];

    [Fact]
    public void PostgreSql_EveryDateTimeProperty_StoresTheValueInTheKindItsColumnTakes_InAWholeSecondColumnAsOnMySql()
    {
        using var context = ServiceDbContexts.Create<ASC.Migrations.Core.MigrationContext>(ServiceDbContexts.PostgreSql);

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
            var keepsFraction = ASC.Core.Common.EF.PostgreSqlModelCustomizer.KeepsFraction(property.DeclaringType.GetTableName()!, property.GetColumnName());

            // The database rounds to the second where the MySQL column has no fraction, as MySQL does.
            if (!keepsFraction && ASC.Core.Common.EF.PostgreSqlModelCustomizer.IsTimestamp(property.GetColumnType()))
            {
                property.GetColumnType().Should().Be(withoutTimeZone
                    ? ASC.Core.Common.EF.PostgreSqlModelCustomizer.WholeSecondTimestamp
                    : ASC.Core.Common.EF.PostgreSqlModelCustomizer.WholeSecondTimestampTz, name);
            }
            else if (keepsFraction)
            {
                property.GetColumnType().Should().NotContain("(0)", name);
            }
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

    /// <summary>
    /// The columns that keep a fraction of a second on PostgreSQL are exactly those whose MySQL type has one: a new
    /// datetime(6) column on MySQL that is not added to the list would be rounded on PostgreSQL only.
    /// </summary>
    [Fact]
    public void KeepsFraction_IsExactlyTheColumnsWithAFractionOnMySql()
    {
        using var context = ServiceDbContexts.Create<ASC.Migrations.Core.MigrationContext>(ServiceDbContexts.MySql);

        var withFraction = context.Model.GetEntityTypes()
            .Where(entityType => entityType.GetTableName() is not null)
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
            .Select(property => (Table: property.DeclaringType.GetTableName()!, Column: property.GetColumnName(), Type: property.GetRelationalTypeMapping().StoreType))
            .ToList();

        withFraction.Should().Contain(column => column.Type.Contains('('), "the model has columns with a fraction");

        var mismatches = withFraction
            .Where(c => ASC.Core.Common.EF.PostgreSqlModelCustomizer.KeepsFraction(c.Table, c.Column) != (c.Type.Contains('(') && !c.Type.EndsWith("(0)", StringComparison.Ordinal)))
            .Select(c => $"{c.Table}.{c.Column} is {c.Type} on MySQL")
            .ToList();

        mismatches.Should().BeEmpty(string.Join("; ", mismatches));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("timestamptz", false)]
    [InlineData("timestamp with time zone", false)]
    [InlineData("timestamp", true)]
    [InlineData("timestamp without time zone", true)]
    [InlineData("timestamp(6)", true)]
    [InlineData("timestamp(0) without time zone", true)]
    [InlineData("timestamp(0) with time zone", false)]
    [InlineData("date", false)]
    public void IsWithoutTimeZone_ColumnType(string? columnType, bool expected)
    {
        ASC.Core.Common.EF.PostgreSqlModelCustomizer.IsWithoutTimeZone(columnType).Should().Be(expected);
    }

    /// <summary>The customizer is PostgreSQL's only: no DateTime property of the MySQL model gets its converters.</summary>
    [Fact]
    public void MySql_DateTimeProperties_GetNoneOfThePostgreSqlConverters()
    {
        using var context = ServiceDbContexts.Create<ASC.Migrations.Core.MigrationContext>(ServiceDbContexts.MySql);

        var dateTimeProperties = context.Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetProperties())
            .Where(property => property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
            .ToList();

        dateTimeProperties.Should().NotBeEmpty();

        var converted = dateTimeProperties
            .Where(property => property.GetValueConverter() == ASC.Core.Common.EF.PostgreSqlModelCustomizer.ToTimestamp
                || property.GetValueConverter() == ASC.Core.Common.EF.PostgreSqlModelCustomizer.ToTimestampTz)
            .Select(property => $"{property.DeclaringType.DisplayName()}.{property.Name}")
            .ToList();

        converted.Should().BeEmpty(string.Join("; ", converted));
    }
}
