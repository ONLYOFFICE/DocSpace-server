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

namespace ASC.Core;

[Scope]
public class AuthorizationManager(IAzService service, TenantManager tenantManager)
{
    public async Task<IEnumerable<AzRecord>> GetAcesAsync(Guid subjectId, Guid actionId)
    {
        var aces = await service.GetAcesAsync(tenantManager.GetCurrentTenantId(), default);

        return aces
            .Where(a => a.Action == actionId && (a.Subject == subjectId || subjectId == Guid.Empty))
            .ToList();
    }

    public async Task<IEnumerable<AzRecord>> GetAcesAsync(Guid subjectId, Guid actionId, ISecurityObjectId objectId)
    {
        var aces = await service.GetAcesAsync(tenantManager.GetCurrentTenantId(), default);

        return FilterAces(aces, subjectId, actionId, objectId)
            .ToList();
    }

    public async Task<IEnumerable<AzRecord>> GetAcesWithInheritsAsync(Guid subjectId, Guid actionId, ISecurityObjectId objectId, ISecurityObjectProvider secObjProvider)
    {
        if (objectId == null)
        {
            return await GetAcesAsync(subjectId, actionId, null);
        }

        var result = new List<AzRecord>();
        var aces = await service.GetAcesAsync(tenantManager.GetCurrentTenantId(), default);
        result.AddRange(FilterAces(aces, subjectId, actionId, objectId));

        var inherits = new List<AzRecord>();
        var secObjProviderHelper = new AzObjectSecurityProviderHelper(objectId, secObjProvider);
        while (secObjProviderHelper.NextInherit())
        {
            inherits.AddRange(FilterAces(aces, subjectId, actionId, secObjProviderHelper.CurrentObjectId));
        }

        inherits.AddRange(FilterAces(aces, subjectId, actionId, null));

        result.AddRange(DistinctAces(inherits));

        return result;
    }

    public async Task AddAceAsync(AzRecord r)
    {
        await service.SaveAceAsync(tenantManager.GetCurrentTenantId(), r);
    }

    public async Task RemoveAceAsync(AzRecord r)
    {
        await service.RemoveAceAsync(tenantManager.GetCurrentTenantId(), r);
    }

    public async Task RemoveAllAcesAsync(ISecurityObjectId id)
    {
        foreach (var r in await GetAcesAsync(Guid.Empty, Guid.Empty, id))
        {
            await RemoveAceAsync(r);
        }
    }

    private static IEnumerable<AzRecord> DistinctAces(IEnumerable<AzRecord> inheritAces)
    {
        var aces = new Dictionary<string, AzRecord>();
        foreach (var a in inheritAces)
        {
            aces[$"{a.Subject}{a.Action}{a.AceType:D}"] = a;
        }

        return aces.Values;
    }

    private IEnumerable<AzRecord> FilterAces(IEnumerable<AzRecord> aces, Guid subjectId, Guid actionId, ISecurityObjectId objectId)
    {
        var objId = AzObjectIdHelper.GetFullObjectId(objectId);

        return aces is AzRecordStore store ?
            store.Get(objId).Where(a => (a.Subject == subjectId || subjectId == Guid.Empty) && (a.Action == actionId || actionId == Guid.Empty)) :
            aces.Where(a => (a.Subject == subjectId || subjectId == Guid.Empty) && (a.Action == actionId || actionId == Guid.Empty) && a.Object == objId);
    }
}