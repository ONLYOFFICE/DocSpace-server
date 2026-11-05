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

namespace ASC.AuditTrail.Models.Mappings;

/// <summary>
/// Takes the titles of a private room out of a stored audit event as the audit trail shows it. The stored event keeps
/// them, because the history of the room is built from the same row and its members read it there. The event names the
/// titles itself (<see cref="EventDescription{T}.PrivateTitles"/>): which part of a description holds a title differs
/// from action to action, and the other parts, such as the names of users, have to stay.
/// </summary>
public static class PrivateTitleMask
{
    // A title this short would match inside other words, so it counts only as a whole item of a list.
    private const int MinTitleLengthToMatchInsideText = 3;

    private static readonly TimeSpan _matchTimeout = TimeSpan.FromSeconds(1);

    private static readonly string[] _titleFields =
    [
        nameof(EventDescription<int>.RoomTitle),
        nameof(EventDescription<int>.RoomOldTitle),
        nameof(EventDescription<int>.ParentTitle),
        nameof(EventDescription<int>.ToParentTitle),
        nameof(EventDescription<int>.FromParentTitle)
    ];

    public static IList<string> Apply(IList<string> description, string neutralTitle)
    {
        var raw = description?.LastOrDefault();

        if (string.IsNullOrEmpty(raw) || !raw.StartsWith('{') || !raw.Contains(nameof(EventDescription<int>.PrivateTitles)))
        {
            return description;
        }

        JsonObject info;

        try
        {
            info = JsonNode.Parse(raw) as JsonObject;
        }
        catch (JsonException)
        {
            return description;
        }

        if (info?[nameof(EventDescription<int>.PrivateTitles)] is not JsonArray titles)
        {
            return description;
        }

        var pattern = BuildPattern(titles);
        info.Remove(nameof(EventDescription<int>.PrivateTitles));

        foreach (var field in _titleFields)
        {
            if (info.ContainsKey(field))
            {
                info[field] = neutralTitle;
            }
        }

        var result = description.Take(description.Count - 1)
            .Select(part => part == null || pattern == null
                ? part
                : Regex.Replace(part, pattern, _ => neutralTitle, RegexOptions.None, _matchTimeout))
            .ToList();

        result.Add(info.ToJsonString());

        return result;
    }

    private static string BuildPattern(JsonArray titles)
    {
        var alternatives = titles
            .Select(title => title?.GetValue<string>())
            .Where(title => !string.IsNullOrEmpty(title))
            .Distinct()
            .OrderByDescending(title => title.Length)
            .Select(title => title.Length >= MinTitleLengthToMatchInsideText
                ? Regex.Escape(title)
                : $@"(?<=^|,\s*){Regex.Escape(title)}(?=\s*,|$)")
            .ToList();

        return alternatives.Count == 0 ? null : string.Join('|', alternatives);
    }
}
