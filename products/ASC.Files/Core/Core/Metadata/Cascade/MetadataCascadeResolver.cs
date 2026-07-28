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

namespace ASC.Files.Core;

/// <summary>
/// Resolves which ancestor a cascaded template link and its inherited values come from
/// when the same template cascades at several nested levels: the nearest ancestor wins.
/// </summary>
public static class MetadataCascadeResolver
{
    /// <summary>
    /// Per template: the cascading ancestor closest to the entry (the smallest tree level).
    /// Returns templateId -> source folderId.
    /// </summary>
    public static Dictionary<int, int> ResolveNearestSources(
        IEnumerable<(int TemplateId, int FolderId)> cascadeLinks,
        IReadOnlyDictionary<int, int> levelByFolderId)
    {
        return cascadeLinks
            .GroupBy(l => l.TemplateId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(l => levelByFolderId.GetValueOrDefault(l.FolderId, int.MaxValue))
                    .ThenBy(l => l.FolderId)
                    .First().FolderId);
    }

    /// <summary>
    /// Per field: the nearest ancestor that cascades the field's template and holds a value for the field.
    /// The field group must then be copied whole from that single folder (multi-choice values span several
    /// rows and must not mix sources); farther ancestors fill the fields the nearer ones left empty.
    /// Returns fieldId -> source folderId.
    /// </summary>
    public static Dictionary<int, int> ResolveFieldSources(
        IEnumerable<(int FieldId, int FolderId)> candidateValues,
        IEnumerable<(int TemplateId, int FolderId)> cascadeLinks,
        IReadOnlyDictionary<int, int> fieldTemplates,
        IReadOnlyDictionary<int, int> levelByFolderId)
    {
        var cascadeSet = cascadeLinks.ToHashSet();

        return candidateValues
            .Where(v => fieldTemplates.TryGetValue(v.FieldId, out var templateId) && cascadeSet.Contains((templateId, v.FolderId)))
            .GroupBy(v => v.FieldId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(v => levelByFolderId.GetValueOrDefault(v.FolderId, int.MaxValue))
                    .ThenBy(v => v.FolderId)
                    .First().FolderId);
    }
}
