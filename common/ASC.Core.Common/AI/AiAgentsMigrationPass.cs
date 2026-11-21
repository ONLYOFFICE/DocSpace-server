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


namespace ASC.Core.Common.AI;

public class AiAgentsMigrationPassState : ISettings<AiAgentsMigrationPassState>
{
    public static Guid ID { get; } = new("5B0C6E0D-2F6A-4C57-9A3E-7D0B4E1C8F29");

    public DateTime? LastPassStart { get; set; }

    public Dictionary<int, int> Attempts { get; set; } = [];

    public DateTime LastModified { get; set; }

    public AiAgentsMigrationPassState GetDefault()
    {
        return new AiAgentsMigrationPassState();
    }
}

public static class AiAgentsMigrationPlan
{
    public static DateTime? GetWindowStart(DateTime? lastPassStart, TimeSpan overlap)
    {
        return lastPassStart - overlap;
    }

    public static List<int> GetCandidates(IEnumerable<int> changedTenantIds, IReadOnlyDictionary<int, int> attempts, int maxAttempts)
    {
        var candidates = new SortedSet<int>(changedTenantIds.Where(id => !IsLeftAlone(attempts, id, maxAttempts)));

        candidates.UnionWith(attempts.Where(a => a.Value < maxAttempts).Select(a => a.Key));

        return [.. candidates];
    }

    public static Dictionary<int, int> GetNextAttempts(IReadOnlyDictionary<int, int> attempts, int tenantId, bool failed)
    {
        var next = new Dictionary<int, int>(attempts);

        if (failed)
        {
            next[tenantId] = next.GetValueOrDefault(tenantId) + 1;
        }
        else
        {
            next.Remove(tenantId);
        }

        return next;
    }

    public static Dictionary<int, int> GetNextAttemptsWhenBusy(IReadOnlyDictionary<int, int> attempts, int tenantId)
    {
        var next = new Dictionary<int, int>(attempts);

        next.TryAdd(tenantId, 0);

        return next;
    }

    public static bool IsLeftAlone(IReadOnlyDictionary<int, int> attempts, int tenantId, int maxAttempts)
    {
        return attempts.TryGetValue(tenantId, out var count) && count >= maxAttempts;
    }
}
