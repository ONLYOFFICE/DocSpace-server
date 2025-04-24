// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Common.Caching;

public static class CacheExtention
{
    public static IFusionCache GetMemoryCache(this IFusionCacheProvider cacheProvider)
    {
        return cacheProvider.GetCache("memory");
    }

    public static TimeSpan OutputDuration = TimeSpan.FromMinutes(5);

    public static string GetUserTag(int tenant, Guid userId)
    {
        return $"user-{tenant}-{userId}";
    }

    public static string GetUserPhotoTag(int tenant, Guid userId)
    {
        return $"userphoto-{tenant}-{userId}";
    }

    public static string GetSettingsTag(int tenant, string settingName)
    {
        return $"settings-{tenant}-{settingName}";
    }

    public static string GetCoreSettingsTag(int tenant, string settingName)
    {
        return $"core-settings-{tenant}-{settingName}";
    }

    public static string GetSettingsTag(int tenant, Guid userId, string settingName)
    {
        return $"settings-{tenant}-{userId}-{settingName}";
    }

    public static string GetTenantQuotaTag(int tenant)
    {
        return $"quota-{tenant}";
    }

    public static string GetTenantQuotaRowTag(int tenant, string path)
    {
        return $"quotarows-{tenant}-{path}";
    }

    public static string GetTenantQuotaRowTag(int tenant, string path, Guid userId)
    {
        return $"quotarows-{tenant}-{path}-{userId}";
    }

    public static string GetRelationTag(int tenant, Guid sourceUserId)
    {
        return $"relation-{tenant}-{sourceUserId}";
    }

    public static string GetGroupTag(int tenant, Guid id)
    {
        return $"group-{tenant}-{id}";
    }

    public static string GetGroupRefTag(int tenant, Guid groupId)
    {
        return $"ref-{tenant}-{groupId}";
    }

    public static string GetWebPluginsTag(int tenant)
    {
        return $"ref-{tenant}";
    } 
    
    public static string GetProviderFileTag(string selector, int id, string fileId)
    {
        return $"provider-file-{selector}-{id}-{fileId}";
    }

    public static string GetProviderFolderTag(string selector, int id, string folderId)
    {
        return $"provider-folder-{selector}-{id}-{folderId}";
    }

    public static string GetProviderFolderItemsTag(string selector, int id, string folderId)
    {
        return $"provider-folder-items-{selector}-{id}-{folderId}";
    }

    public static string GetProviderTag(string selector, int id)
    {
        return $"provider-{selector}-{id}";
    }

    public static string GetWebItemSecurityTag(int tenant)
    {
        return $"webItem-{tenant}";
    }

    public static string GetTenantSettingsTag(int tenant, string key)
    {
        return $"settings-{tenant}-{key}";
    }

    public static string GetConsumerTag(int tenant, string consumer)
    {
        return $"consumer-{tenant}-{consumer}";
    }

    public static string GetTenantTag(int tenant)
    {
        return $"tenant-{tenant}";
    }

    public static string GetFolderTag<T>(int tenant, T id)
    {
        return $"folder-{tenant}-{id}";
    }

    public static string GetHistoriesFolderTag<T>(int tenant, T id)
    {
        return $"histories-folder-{tenant}-{id}";
    }

    public static string GetHistoriesFileTag<T>(int tenant, T id)
    {
        return $"histories-file-{tenant}-{id}";
    } 
    
    public static string GetDocumentServiceTag()
    {
        return $"DocumentServiceVersion";
    } 
    
    public static string GetDocumentServiceLicenseTag()
    {
        return $"DocumentServiceLicense";
    }

    public static string GetFoldersTag(int tenant, int parent)
    {
        return $"folders-{tenant}-{parent}";
    }

    public static string GetTariffTag(int tenant)
    {
        return $"tariff-{tenant}";
    }

    public static string GetPaymentTag(int tenant)
    {
        return $"payment-{tenant}";
    }

    public static string GetPluginsTag(int tenant)
    {
        return $"plugins-{tenant}";
    }

    public static string GetThirdpartiesTag(int tenant)
    {
        return $"thirdparty-{tenant}";
    }
}