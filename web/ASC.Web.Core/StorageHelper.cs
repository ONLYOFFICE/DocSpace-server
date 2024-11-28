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
