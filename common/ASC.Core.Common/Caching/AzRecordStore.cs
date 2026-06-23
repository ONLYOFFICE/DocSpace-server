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

namespace ASC.Core.Caching;

internal class AzRecordStore : IEnumerable<AzRecord>
{
    private readonly Dictionary<string, List<AzRecord>> _byObjectId = new();


    public AzRecordStore(IEnumerable<AzRecord> aces)
    {
        foreach (var a in aces)
        {
            Add(a);
        }
    }


    public IEnumerable<AzRecord> Get(string objectId)
    {
        _byObjectId.TryGetValue(objectId ?? string.Empty, out var aces);

        return aces ?? [];
    }

    public void Add(AzRecord r)
    {
        if (r == null)
        {
            return;
        }

        var id = r.Object ?? string.Empty;
        if (!_byObjectId.ContainsKey(id))
        {
            _byObjectId[id] = [];
        }
        _byObjectId[id].RemoveAll(a => a.Subject == r.Subject && a.Action == r.Action); // remove escape, see DbAzService
        _byObjectId[id].Add(r);
    }

    public void Remove(AzRecord r)
    {
        if (r == null)
        {
            return;
        }

        var id = r.Object ?? string.Empty;
        if (_byObjectId.TryGetValue(id, out var list))
        {
            list.RemoveAll(a => a.Subject == r.Subject && a.Action == r.Action && a.AceType == r.AceType);
        }
    }

    public IEnumerator<AzRecord> GetEnumerator()
    {
        return _byObjectId.Values.SelectMany(v => v).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}