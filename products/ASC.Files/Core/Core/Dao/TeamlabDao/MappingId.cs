// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Files.Core.Data;


[Scope(typeof(IMappingId<int>))]
internal class MappingId : IMappingId<int>
{
    public ValueTask<string> MappingIdAsync(int id, bool saveIfNotExist = false)
    {
        return ValueTask.FromResult(id.ToString());
    }
}

[Scope(typeof(IMappingId<string>))]
internal class ThirdPartyMappingId(TenantManager tenantManager, IDbContextFactory<FilesDbContext> dbContextFactory): IMappingId<string>
{
    public ValueTask<string> MappingIdAsync(string id, bool saveIfNotExist = false)
    {
        if (id == null)
        {
            return ValueTask.FromResult<string>(null);
        }

        var isNumeric = int.TryParse(id, out var n);

        if (isNumeric)
        {
            return ValueTask.FromResult(n.ToString());
        }

        return InternalMappingIdAsync(id, saveIfNotExist);
    }

    private async ValueTask<string> InternalMappingIdAsync(string id, bool saveIfNotExist = false)
    {
        string result;

        if (Selectors.All.Exists(s => id.StartsWith(s.Id)))
        {
            result = Regex.Replace(BitConverter.ToString(Hasher.Hash(id, HashAlg.MD5)), "-", "").ToLower();
        }
        else
        {
            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
            var tenantId = await tenantManager.GetCurrentTenantIdAsync();
            result = await filesDbContext.IdAsync(tenantId, id);
        }

        if (saveIfNotExist)
        {
            var tenantId = await tenantManager.GetCurrentTenantIdAsync();
            
            var newItem = new DbFilesThirdpartyIdMapping
            {
                Id = id,
                HashId = result,
                TenantId = tenantId
            };

            await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
            await filesDbContext.AddOrUpdateAsync(r => r.ThirdpartyIdMapping, newItem);
            await filesDbContext.SaveChangesWithValidateAsync();
        }

        return result;
    }
}