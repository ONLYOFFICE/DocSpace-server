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

namespace ASC.Files.Core.Helpers;

/// <summary>
/// Compares the fields of two form versions. Fields with the same column name anchor the comparison in form order;
/// between two anchors, removed and added fields of the same column types are paired one to one as renames, while
/// fields only added or only removed keep the table (a removed field keeps its column with the earlier answers).
/// </summary>
internal static class FormFieldMatcher
{
    private sealed record Field(FormMetadata Metadata, DbColumnDefinition Column);

    /// <summary>
    /// The renamed fields, empty when the fields are the same, added or removed. Null when the new version needs a table
    /// of its own: a field changed its type, or the pairing is not certain.
    /// </summary>
    public static List<FormFieldRename> Match(IEnumerable<FormMetadata> previous, IEnumerable<FormMetadata> current)
    {
        var previousFields = ToFields(previous);
        var currentFields = ToFields(current);
        if (previousFields.Count == 0 || currentFields.Count == 0)
        {
            return null;
        }

        var previousByName = previousFields.ToDictionary(f => f.Column.Name);
        var currentNames = currentFields.Select(f => f.Column.Name).ToHashSet();

        if (currentFields.Any(f => previousByName.TryGetValue(f.Column.Name, out var p) && !SameColumnType(p.Column, f.Column)))
        {
            return null;
        }

        var anchors = LongestCommonSubsequence(
            previousFields.Select(f => f.Column.Name).Where(currentNames.Contains).ToList(),
            currentFields.Select(f => f.Column.Name).Where(previousByName.ContainsKey).ToList());

        var renames = new List<FormFieldRename>();
        int i = 0, j = 0;

        foreach (var anchor in anchors.Append(null))
        {
            var removed = TakeGap(previousFields, ref i, anchor, f => !currentNames.Contains(f.Column.Name));
            var added = TakeGap(currentFields, ref j, anchor, f => !previousByName.ContainsKey(f.Column.Name));

            if (removed.Count == 0 || added.Count == 0)
            {
                continue;
            }

            if (removed.Count != added.Count)
            {
                return null;
            }

            for (var k = 0; k < removed.Count; k++)
            {
                if (!SameColumnType(removed[k].Column, added[k].Column))
                {
                    return null;
                }

                renames.Add(new FormFieldRename { OldKey = removed[k].Metadata.Key, NewKey = added[k].Metadata.Key });
            }
        }

        return renames;
    }

    private static List<Field> ToFields(IEnumerable<FormMetadata> metadata)
    {
        return metadata
            .Select(m => new Field(m, FormFillingReportCreator.BuildColumnDefinition(m)))
            .DistinctBy(f => f.Column.Name)
            .ToList();
    }

    // The fields up to the anchor (or to the end) that pass the filter; the position moves past the anchor.
    private static List<Field> TakeGap(List<Field> fields, ref int position, string anchor, Func<Field, bool> filter)
    {
        var gap = new List<Field>();
        while (position < fields.Count && fields[position].Column.Name != anchor)
        {
            if (filter(fields[position]))
            {
                gap.Add(fields[position]);
            }

            position++;
        }

        position++;

        return gap;
    }

    private static bool SameColumnType(DbColumnDefinition a, DbColumnDefinition b)
    {
        return a.Type == b.Type && (a.EnumValues ?? []).ToHashSet(StringComparer.Ordinal).SetEquals(b.EnumValues ?? []);
    }

    private static List<string> LongestCommonSubsequence(List<string> a, List<string> b)
    {
        var lengths = new int[a.Count + 1][];
        for (var x = 0; x <= a.Count; x++)
        {
            lengths[x] = new int[b.Count + 1];
        }

        for (var x = a.Count - 1; x >= 0; x--)
        {
            for (var y = b.Count - 1; y >= 0; y--)
            {
                lengths[x][y] = a[x] == b[y] ? lengths[x + 1][y + 1] + 1 : Math.Max(lengths[x + 1][y], lengths[x][y + 1]);
            }
        }

        var result = new List<string>();
        int i = 0, j = 0;
        while (i < a.Count && j < b.Count)
        {
            if (a[i] == b[j])
            {
                result.Add(a[i]);
                i++;
                j++;
            }
            else if (lengths[i + 1][j] >= lengths[i][j + 1])
            {
                i++;
            }
            else
            {
                j++;
            }
        }

        return result;
    }
}
