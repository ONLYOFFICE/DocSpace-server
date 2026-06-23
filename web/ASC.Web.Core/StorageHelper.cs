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

[Scope]
public class StorageHelper(UserPhotoManager userPhotoManager, StorageFactory storageFactory, TenantManager tenantManager, ILogger<StorageHelper> logger)
{
    private const string StorageName = "customnavigation";
    private const string Base64Start = "data:image/png;base64,";

    public async Task<string> SaveTmpLogo(string tmpLogoPath)
    {
        if (string.IsNullOrEmpty(tmpLogoPath))
        {
            return null;
        }

        try
        {
            byte[] data;

            if (tmpLogoPath.StartsWith(Base64Start))
            {
                data = Convert.FromBase64String(tmpLogoPath[Base64Start.Length..]);

                return await SaveLogoAsync(Guid.NewGuid() + ".png", data);
            }

            var fileName = Path.GetFileName(tmpLogoPath);

            data = await userPhotoManager.GetTempPhotoData(fileName);

            await userPhotoManager.RemoveTempPhotoAsync(fileName);

            return await SaveLogoAsync(fileName, data);
        }
        catch (Exception ex)
        {
            logger.ErrorSaveTmpLogo(ex);
            return null;
        }
    }

    public async Task DeleteLogoAsync(string logoPath)
    {
        if (string.IsNullOrEmpty(logoPath))
        {
            return;
        }

        try
        {
            var store = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), StorageName);

            var fileName = Path.GetFileName(logoPath);

            if (await store.IsFileAsync(fileName))
            {
                await store.DeleteAsync(fileName);
            }
        }
        catch (Exception e)
        {
            logger.ErrorDeleteLogo(e);
        }
    }

    private async Task<string> SaveLogoAsync(string fileName, byte[] data)
    {
        var store = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), StorageName);

        using var stream = new MemoryStream(data);
        stream.Seek(0, SeekOrigin.Begin);
        return (await store.SaveAsync(fileName, stream)).ToString();
    }
}