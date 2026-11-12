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
/// Compares the fields of two form versions without the editor's reports: by their place in the form when only keys
/// changed, and by the column names they share.
/// </summary>
internal static class FormFieldMatcher
{
    private sealed record Field(FormMetadata Metadata, DbColumnDefinition Column);

    /// <summary>
    /// The renamed fields when only keys changed: the two versions have as many fields, in the same order, of the same
    /// types and options, and each field is paired with the one in its place. Null when anything else changed, or when a
    /// key moved to another place, since the fields may then have been reordered.
    /// </summary>
    public static List<FormFieldRename> MatchSameLayout(IEnumerable<FormMetadata> previous, IEnumerable<FormMetadata> current)
    {
        var previousFields = ToFields(previous);
        var currentFields = ToFields(current);
        if (previousFields.Count == 0 || previousFields.Count != currentFields.Count)
        {
            return null;
        }

        var previousNames = previousFields.Select(f => f.Column.Name).ToHashSet();
        var currentNames = currentFields.Select(f => f.Column.Name).ToHashSet();
        var renames = new List<FormFieldRename>();

        for (var i = 0; i < previousFields.Count; i++)
        {
            var (before, after) = (previousFields[i], currentFields[i]);
            if (!SameColumnType(before.Column, after.Column))
            {
                return null;
            }

            if (before.Column.Name == after.Column.Name)
            {
                continue;
            }

            if (currentNames.Contains(before.Column.Name) || previousNames.Contains(after.Column.Name))
            {
                return null;
            }

            renames.Add(new FormFieldRename { OldKey = before.Metadata.Key, NewKey = after.Metadata.Key });
        }

        return renames;
    }

    /// <summary>
    /// The number of fields the two versions share by column name; null when one of them kept its name under another
    /// type, so that the versions cannot share a table. The options of a list may differ: its column takes them all.
    /// </summary>
    public static int? CountShared(IEnumerable<FormMetadata> previous, IEnumerable<FormMetadata> current)
    {
        var previousByName = ToFields(previous).ToDictionary(f => f.Column.Name);
        var shared = 0;
        foreach (var field in ToFields(current))
        {
            if (!previousByName.TryGetValue(field.Column.Name, out var p))
            {
                continue;
            }

            if (p.Column.Type != field.Column.Type)
            {
                return null;
            }

            shared++;
        }

        return shared;
    }

    private static List<Field> ToFields(IEnumerable<FormMetadata> metadata)
    {
        return metadata
            .Select(m => new Field(m, FormFillingReportCreator.BuildColumnDefinition(m)))
            .DistinctBy(f => f.Column.Name)
            .ToList();
    }

    private static bool SameColumnType(DbColumnDefinition a, DbColumnDefinition b)
    {
        return a.Type == b.Type && (a.EnumValues ?? []).ToHashSet(StringComparer.Ordinal).SetEquals(b.EnumValues ?? []);
    }
}
