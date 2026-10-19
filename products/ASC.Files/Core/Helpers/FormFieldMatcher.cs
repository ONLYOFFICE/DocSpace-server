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
/// between two anchors, removed and added fields are paired as renames by their column types in form order when only
/// one pairing is the longest, and the rest are removed or added: a removed field keeps its column with the earlier
/// answers. Two fields given one key, which then share their value, count as one.
/// </summary>
internal static class FormFieldMatcher
{
    private sealed record Field(FormMetadata Metadata, DbColumnDefinition Column);

    /// <summary>
    /// The renamed fields, empty when the fields are the same, added or removed. Null when the new version needs a table
    /// of its own: a field kept its key under another type, or more than one pairing of renamed fields is possible.
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

            var pairs = PairByType(removed, added);
            if (pairs == null)
            {
                return null;
            }

            renames.AddRange(pairs.Select(p => new FormFieldRename { OldKey = p.Removed.Metadata.Key, NewKey = p.Added.Metadata.Key }));
        }

        return renames;
    }

    // The longest pairing of the fields by column type, kept in form order on both sides; null when another pairing is
    // as long, since which field became which is then a guess. The pairing that takes the earliest fields and the one
    // that takes the latest are the two extremes of all the longest ones, so they agree only when it is the one.
    private static List<(Field Removed, Field Added)> PairByType(List<Field> removed, List<Field> added)
    {
        int n = removed.Count, m = added.Count;
        bool Same(int r, int a) => SameColumnType(removed[r].Column, added[a].Column);

        var suffix = new int[n + 1, m + 1];
        for (var r = n - 1; r >= 0; r--)
        {
            for (var a = m - 1; a >= 0; a--)
            {
                suffix[r, a] = Same(r, a) ? suffix[r + 1, a + 1] + 1 : Math.Max(suffix[r + 1, a], suffix[r, a + 1]);
            }
        }

        var prefix = new int[n + 1, m + 1];
        for (var r = 1; r <= n; r++)
        {
            for (var a = 1; a <= m; a++)
            {
                prefix[r, a] = Same(r - 1, a - 1) ? prefix[r - 1, a - 1] + 1 : Math.Max(prefix[r - 1, a], prefix[r, a - 1]);
            }
        }

        var earliest = new List<(int, int)>();
        for (int r = 0, a = 0; r < n && a < m;)
        {
            if (Same(r, a) && suffix[r, a] == suffix[r + 1, a + 1] + 1)
            {
                earliest.Add((r++, a++));
            }
            else if (suffix[r + 1, a] == suffix[r, a])
            {
                r++;
            }
            else
            {
                a++;
            }
        }

        var latest = new List<(int, int)>();
        for (int r = n, a = m; r > 0 && a > 0;)
        {
            if (Same(r - 1, a - 1) && prefix[r, a] == prefix[r - 1, a - 1] + 1)
            {
                latest.Add((--r, --a));
            }
            else if (prefix[r - 1, a] == prefix[r, a])
            {
                r--;
            }
            else
            {
                a--;
            }
        }

        latest.Reverse();

        return earliest.SequenceEqual(latest) ? earliest.Select(p => (removed[p.Item1], added[p.Item2])).ToList() : null;
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
