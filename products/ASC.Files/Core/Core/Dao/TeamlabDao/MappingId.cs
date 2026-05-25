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

namespace ASC.Files.Core.Data;


[Scope(typeof(IMappingId<int>))]
internal class MappingId : IMappingId<int>
{
    public ValueTask<(string, int)> MappingIdAsync(int id, bool saveIfNotExist = false)
    {
        return ValueTask.FromResult<(string, int)>((id.ToString(), id));
    }
}

[Scope(typeof(IMappingId<string>))]
internal class ThirdPartyMappingId(TenantManager tenantManager, IDbContextFactory<FilesDbContext> dbContextFactory) : IMappingId<string>
{
    public ValueTask<(string, int)> MappingIdAsync(string id, bool saveIfNotExist = false)
    {
        if (id == null)
        {
            return ValueTask.FromResult<(string, int)>((null, 0));
        }

        var isNumeric = int.TryParse(id, out var n);

        if (isNumeric)
        {
            return  ValueTask.FromResult<(string, int)>((n.ToString(), n));
        }

        return InternalMappingIdAsync(id, saveIfNotExist);
    }

    private async ValueTask<(string, int)> InternalMappingIdAsync(string id, bool saveIfNotExist = false)
    {
        string result;

        if (Selectors.All.Exists(s => id.StartsWith(s.Id)))
        {
            result = Regex.Replace(BitConverter.ToString(Hasher.Hash(id, HashAlg.MD5)), "-", "").ToLower();
        }
        else
        {
            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
            var tenantId = tenantManager.GetCurrentTenantId();
            result = await filesDbContext.IdAsync(tenantId, id);
        }

        if (saveIfNotExist)
        {
            var tenantId = tenantManager.GetCurrentTenantId();

            var newItem = new DbFilesThirdpartyIdMapping
            {
                Id = id,
                HashId = result,
                TenantId = tenantId
            };

            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
            await filesDbContext.AddOrUpdateAsync(r => r.ThirdpartyIdMapping, newItem);
            await filesDbContext.SaveChangesAsync();
        }

        return (result, 0);
    }
}