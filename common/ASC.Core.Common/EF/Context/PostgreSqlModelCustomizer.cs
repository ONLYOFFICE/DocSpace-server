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
/// hands it back. The precision still differs: PostgreSQL keeps the microseconds, where a MySQL datetime column without
/// a fraction rounds to the second.
/// </summary>
public sealed class PostgreSqlModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    public static readonly ValueConverter<DateTime, DateTime> ToTimestampTz = new(
        value => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified));

    public static readonly ValueConverter<DateTime, DateTime> ToTimestamp = new(
        value => value.Kind == DateTimeKind.Unspecified ? value : DateTime.SpecifyKind(value, DateTimeKind.Unspecified),
        value => DateTime.SpecifyKind(value, DateTimeKind.Unspecified));

    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entityType => entityType.GetProperties()))
        {
            // A nullable DateTime takes the same converter: EF passes nulls around it.
            if ((property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?)) && property.GetValueConverter() is null)
            {
                property.SetValueConverter(IsWithoutTimeZone(property.GetColumnType()) ? ToTimestamp : ToTimestampTz);
            }
        }
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
