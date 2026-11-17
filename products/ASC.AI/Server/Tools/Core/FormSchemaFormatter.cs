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

namespace ASC.AI.Tools.Core;

/// <summary>
/// Renders submission-table columns for the prompts the model sees. Shared so the tools and the
/// pre-analysis cannot describe the same schema differently.
/// </summary>
public static class FormSchemaFormatter
{
    /// <summary>Renders a column as <c>col_status "Status" (String) [approved/pending]</c>.</summary>
    public static string FormatColumn(DbColumnDefinition column, int maxEnumValues = int.MaxValue)
    {
        var label = column.Label is not null && column.Label != column.Name ? $" \"{column.Label}\"" : string.Empty;
        var desc = $"{column.Name}{label} ({column.Type})";

        if (column.EnumValues is not { Count: > 0 } values)
        {
            return desc;
        }

        return values.Count <= maxEnumValues
            ? $"{desc} [{string.Join("/", values)}]"
            : $"{desc} [{string.Join("/", values.Take(maxEnumValues))}/...]";
    }

    /// <summary>
    /// Which versions of the form the rows of the table answer, so that a table continued by later versions is not taken
    /// for the responses to one version only. <paramref name="apart"/>: the earlier versions with responses in other
    /// tables; null when unknown.
    /// </summary>
    public static string FormatCoverage(int version, IReadOnlyList<int> inTable, IReadOnlyList<int>? apart)
    {
        var all = apart is { Count: 0 };
        var coverage = inTable.Count > 1
            ? $"These are {(all ? "all " : string.Empty)}the responses to the form, given to versions {string.Join(", ", inTable)}; " +
              $"version {version} is the current one. A field renamed in a later version keeps its earlier answers in the column of its current name."
            : all
                ? "These are all the responses to the form."
                : $"These are the responses to version {version} of the form, the current one.";

        if (apart is not { Count: > 0 })
        {
            return coverage;
        }

        var versions = apart.Count == 1 ? $"version {apart[0]}" : $"versions {string.Join(", ", apart)}";
        return $"{coverage} The responses to {versions} are kept apart and are not counted here.";
    }
}
