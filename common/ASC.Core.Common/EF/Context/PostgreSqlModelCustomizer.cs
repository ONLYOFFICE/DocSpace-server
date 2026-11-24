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

namespace ASC.Core.Common.EF;

/// <summary>
/// Stores a <see cref="DateTime"/> on PostgreSQL the way MySQL stores it: the value as it is, whatever its
/// <see cref="DateTime.Kind"/>. Npgsql writes to a timestamptz column only a value of Kind Utc, and to a timestamp
/// (without time zone) column only one that is not Utc, while the code passes tenant wall-clock times of Kind Local
/// (TenantUtil.DateTimeFromUtc), values of Kind Unspecified and DateTime.UtcNow alike. Every DateTime property without a
/// converter of its own gets one by its column type: marked Utc on the way into timestamptz (the type Npgsql picks
/// when the model names none) and Unspecified into timestamp, never shifted, and Unspecified on the way out, as MySQL
/// hands it back. A MySQL datetime column without a fraction rounds a value to the nearest second, so such a column is
/// timestamp(0) or timestamptz(0) here, which PostgreSQL rounds the same way; the columns that keep a fraction on MySQL
/// (<see cref="KeepsFraction"/>) keep the microseconds here too. The rounding is the database's, as on MySQL: a value is
/// rounded when it is stored, never in the code, so a parameter compared with such a column is compared unrounded.
/// A MySQL text column compares, sorts and indexes letters whatever their case (utf8_general_ci and the other _ci
/// collations), so a text column is citext here: EF passes a value compared with it as citext too, which makes =, IN,
/// LIKE (StartsWith, Contains, EndsWith), ORDER BY and a unique index ignore the case as on MySQL. The columns that
/// tell the case apart on MySQL (<see cref="CaseSensitive"/>) keep their type, and so do json and jsonb ones. citext
/// has no length, so the database here does not refuse a value longer than the MySQL column allows.
/// </summary>
public sealed class PostgreSqlModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    /// <summary>
    /// The columns whose MySQL type keeps a fraction of a second (datetime(6)), as table and column, besides the tables of
    /// ASC.Identity: every date column there keeps one (datetime(6), timestamp(3)), and the Java service writes them.
    /// </summary>
    private static readonly FrozenSet<(string Table, string Column)> _withFraction = new[]
    {
        ("ai_integration_messages", "timestamp")
    }.ToFrozenSet();

    private const string IdentityTablePrefix = "identity_";

    /// <summary>The text columns whose MySQL collation tells the case apart (a _bin one), as table and column.</summary>
    private static readonly FrozenSet<(string Table, string Column)> _caseSensitive = new[]
    {
        ("short_links", "link")
    }.ToFrozenSet();

    /// <summary>The PostgreSQL type of a text column that ignores the case, from the citext extension.</summary>
    public const string CaseInsensitiveText = "citext";

    public static readonly ValueConverter<DateTime, DateTime> ToTimestampTz = new(
        value => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified));

    public static readonly ValueConverter<DateTime, DateTime> ToTimestamp = new(
        value => value.Kind == DateTimeKind.Unspecified ? value : DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
        value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified));

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        modelBuilder.HasPostgresExtension(CaseInsensitiveText);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
        {
            // A nullable DateTime takes the same converter: EF passes nulls around it.
            if ((property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?)) && property.GetValueConverter() is null)
            {
                var columnType = property.GetColumnType();
                var withoutTimeZone = IsWithoutTimeZone(columnType);

                property.SetValueConverter(withoutTimeZone ? ToTimestamp : ToTimestampTz);

                var table = property.DeclaringType.GetTableName();

                if (table is not null && !KeepsFraction(table, property.GetColumnName()) && IsTimestamp(columnType))
                {
                    property.SetColumnType(withoutTimeZone ? WholeSecondTimestamp : WholeSecondTimestampTz);
                }
            }
            else if ((property.GetValueConverter()?.ProviderClrType ?? property.ClrType) == typeof(string)
                && property.DeclaringType.GetTableName() is { } table
                && IsText(property.GetColumnType())
                && !CaseSensitive(table, property.GetColumnName()))
            {
                property.SetColumnType(CaseInsensitiveText);
            }
        }
    }

    /// <summary>Whether the MySQL column of that table tells the case apart; every other text column ignores it.</summary>
    public static bool CaseSensitive(string table, string column)
    {
        return _caseSensitive.Contains((table, column));
    }

    /// <summary>
    /// Whether the column type is a character type (text, varchar, char), the ones a MySQL collation applies to; no type
    /// means text, Npgsql's default for a string.
    /// </summary>
    public static bool IsText(string columnType)
    {
        if (string.IsNullOrEmpty(columnType))
        {
            return true;
        }

        var type = columnType.Trim().ToLowerInvariant();

        return type is "text" or "bpchar" or "char"
            || type.StartsWith("varchar", StringComparison.Ordinal)
            || type.StartsWith("character", StringComparison.Ordinal)
            || type.StartsWith("char(", StringComparison.Ordinal);
    }

    /// <summary>The column types of a DateTime whose MySQL column has no fraction of a second.</summary>
    public const string WholeSecondTimestamp = "timestamp(0) without time zone";

    public const string WholeSecondTimestampTz = "timestamp(0) with time zone";

    /// <summary>Whether the MySQL column of that table keeps a fraction of a second; every other DateTime column has none.</summary>
    public static bool KeepsFraction(string table, string column)
    {
        return table.StartsWith(IdentityTablePrefix, StringComparison.Ordinal) || _withFraction.Contains((table, column));
    }

    /// <summary>Whether the column type is a timestamp of either kind; no type means timestamptz, Npgsql's default.</summary>
    public static bool IsTimestamp(string columnType)
    {
        return string.IsNullOrEmpty(columnType) || columnType.Trim().StartsWith("timestamp", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Whether the column type is timestamp without time zone; no type means timestamptz, Npgsql's default.</summary>
    public static bool IsWithoutTimeZone(string columnType)
    {
        if (string.IsNullOrEmpty(columnType))
        {
            return false;
        }

        var type = columnType.Trim().ToLowerInvariant();

        return type.StartsWith("timestamp", StringComparison.Ordinal) && !type.StartsWith("timestamptz", StringComparison.Ordinal) && !type.Contains("with time zone", StringComparison.Ordinal);
    }
}
