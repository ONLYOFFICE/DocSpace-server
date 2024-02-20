// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Web.Files.Classes;

public class FilesSettings : ISettings<FilesSettings>
{
    [JsonPropertyName("EnableThirdpartySettings")]
    public bool EnableThirdpartySetting { get; set; }

    [JsonPropertyName("FastDelete")]
    public bool FastDeleteSetting { get; set; }

    [JsonPropertyName("StoreOriginalFiles")]
    public bool StoreOriginalFilesSetting { get; set; }

    [JsonPropertyName("KeepNewFileName")]
    public bool KeepNewFileName { get; set; }

    [JsonPropertyName("ConvertNotify")]
    public bool ConvertNotifySetting { get; set; }

    [JsonPropertyName("DefaultSortedBy")]
    public SortedByType DefaultSortedBySetting { get; set; }

    [JsonPropertyName("DefaultSortedAsc")]
    public bool DefaultSortedAscSetting { get; set; }

    [JsonPropertyName("HideConfirmConvertSave")]
    public bool HideConfirmConvertSaveSetting { get; set; }

    [JsonPropertyName("HideConfirmConvertOpen")]
    public bool HideConfirmConvertOpenSetting { get; set; }

    [JsonPropertyName("Forcesave")]
    public bool ForcesaveSetting { get; set; }

    [JsonPropertyName("StoreForcesave")]
    public bool StoreForcesaveSetting { get; set; }

    [JsonPropertyName("HideRecent")]
    public bool HideRecentSetting { get; set; }

    [JsonPropertyName("HideFavorites")]
    public bool HideFavoritesSetting { get; set; }

    [JsonPropertyName("HideTemplates")]
    public bool HideTemplatesSetting { get; set; }

    [JsonPropertyName("DownloadZip")]
    public bool DownloadTarGzSetting { get; set; }

    [JsonPropertyName("ShareLink")]
    public bool DisableShareLinkSetting { get; set; }

    [JsonPropertyName("ShareLinkSocialMedia")]
    public bool DisableShareSocialMediaSetting { get; set; }

    [JsonPropertyName("AutomaticallyCleanUp")]
    public AutoCleanUpData AutomaticallyCleanUpSetting { get; set; }

    [JsonPropertyName("DefaultSharingAccessRights")]
    public List<FileShare> DefaultSharingAccessRightsSetting { get; set; }

    public FilesSettings GetDefault()
    {
        return new FilesSettings
        {
            FastDeleteSetting = false,
            EnableThirdpartySetting = true,
            StoreOriginalFilesSetting = true,
            ConvertNotifySetting = true,
            DefaultSortedBySetting = SortedByType.DateAndTime,
            DefaultSortedAscSetting = false,
            HideConfirmConvertSaveSetting = false,
            HideConfirmConvertOpenSetting = false,
            ForcesaveSetting = true,
            StoreForcesaveSetting = false,
            HideRecentSetting = false,
            HideFavoritesSetting = false,
            HideTemplatesSetting = false,
            DownloadTarGzSetting = false,
            AutomaticallyCleanUpSetting = null,
            DefaultSharingAccessRightsSetting = null
        };
    }

    [JsonIgnore]
    public Guid ID => new("{03B382BD-3C20-4f03-8AB9-5A33F016316E}");
}

[Scope]
public class FilesSettingsHelper(
    Global global,
    MessageService messageService,
    SettingsManager settingsManager,
    AuthContext authContext)
{
    private static readonly FilesSettings _emptySettings = new();

    public async Task<bool> GetConfirmDelete() => !(await LoadForCurrentUser()).FastDeleteSetting;

    public async Task SetConfirmDelete(bool value)
    {
        var setting = await LoadForCurrentUser();
        setting.FastDeleteSetting = !value;
        await SaveForCurrentUser(setting);
    }

    public async Task<bool> GetEnableThirdParty() => (await settingsManager.LoadAsync<FilesSettings>()).EnableThirdpartySetting;

    public async Task SetEnableThirdParty(bool value)
    {        
        if (!await global.IsDocSpaceAdministratorAsync)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var setting = await settingsManager.LoadAsync<FilesSettings>();
        setting.EnableThirdpartySetting = value;
        await settingsManager.SaveAsync(setting);
        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsThirdPartySettingsUpdated);
    }

    public async Task<bool> GetExternalShare()
    {
        return !(await Load()).DisableShareLinkSetting;
    }

    public async Task SetExternalShare(bool value)
    {
        var settings = await Load();
        settings.DisableShareLinkSetting = !value;
        await Save(settings);
    }

    public async Task<bool> GetExternalShareSocialMedia()
    {
        var setting = await Load();
        return !setting.DisableShareLinkSetting && !setting.DisableShareSocialMediaSetting;
    }

    public async Task SetExternalShareSocialMedia(bool value)
    {
        var settings = await Load();
        settings.DisableShareSocialMediaSetting = !value;
        await Save(settings);
    }
    
    public async Task<bool> ChangeExternalShareSettingsAsync(bool enable)
    {
        if (!await global.IsDocSpaceAdministratorAsync)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        await SetExternalShare(enable);

        if (!enable)
        {
            await SetExternalShareSocialMedia(false);
        }

        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsExternalShareSettingsUpdated);

        return await GetExternalShare();
    }
    
    public async Task<bool> ChangeExternalShareSocialMediaSettingsAsync(bool enable)
    {
        if (!await global.IsDocSpaceAdministratorAsync)
        {
            throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        await SetExternalShareSocialMedia(await GetExternalShare() && enable);

        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsExternalShareSettingsUpdated);

        return await GetExternalShareSocialMedia();
    }
    
    public async Task<bool> GetStoreOriginalFiles() => (await LoadForCurrentUser()).StoreOriginalFilesSetting;

    public async Task SetStoreOriginalFiles(bool value)
    {
        var setting = await LoadForCurrentUser();
        setting.StoreOriginalFilesSetting = value;
        await SaveForCurrentUser(setting);
        
        await messageService.SendHeadersMessageAsync(MessageAction.DocumentsUploadingFormatsSettingsUpdated);
    }

    public async Task<bool> GetKeepNewFileName() => (await LoadForCurrentUser()).KeepNewFileName;

    public async Task<bool> SetKeepNewFileName(bool value)
    {        
        var current = await LoadForCurrentUser();
        if (current.KeepNewFileName != value)
        {
            current.KeepNewFileName = value;
            await SaveForCurrentUser(current);
            await messageService.SendHeadersMessageAsync(MessageAction.DocumentsKeepNewFileNameSettingsUpdated);
        }

        return current.KeepNewFileName;
    }

    public async Task<bool> GetConvertNotify() => (await LoadForCurrentUser()).ConvertNotifySetting;

    public async Task SetConvertNotify(bool value)
    {
        var setting = await LoadForCurrentUser();
        setting.ConvertNotifySetting = value;
        await SaveForCurrentUser(setting);
    }

    public async Task<bool> GetHideConfirmConvertSave() => (await LoadForCurrentUser()).HideConfirmConvertSaveSetting;

    private async Task SetHideConfirmConvertSave(bool value)
    {
        var setting = await LoadForCurrentUser();
        setting.HideConfirmConvertSaveSetting = value;
        await SaveForCurrentUser(setting);
    }

    public async Task<bool> GetHideConfirmConvertOpen() => (await LoadForCurrentUser()).HideConfirmConvertOpenSetting;

    private async Task SetHideConfirmConvertOpen(bool value)
    {
        var setting = await LoadForCurrentUser();
        setting.HideConfirmConvertOpenSetting = value;
        await SaveForCurrentUser(setting);
    }

    public async Task<bool> HideConfirmConvert(bool isForSave)
    {
        if (isForSave)
        {
            await SetHideConfirmConvertSave(true);
        }
        else
        {
            await SetHideConfirmConvertOpen(true);
        }

        return true;
    }
    
    public async Task<OrderBy> GetDefaultOrder()
    {
        var setting = await LoadForCurrentUser();

        return new OrderBy(setting.DefaultSortedBySetting, setting.DefaultSortedAscSetting);
    }

    public async Task SetDefaultOrder(OrderBy value)
    {
        var setting = await LoadForCurrentUser();
        if (setting.DefaultSortedBySetting != value.SortedBy || setting.DefaultSortedAscSetting != value.IsAsc)
        {
            setting.DefaultSortedBySetting = value.SortedBy;
            setting.DefaultSortedAscSetting = value.IsAsc;
            await SaveForCurrentUser(setting);
        }
    }

    public bool GetForcesave() => true;

    public void SetForcesave(bool value)
    {
        //var setting = await LoadForCurrentUser();
        //setting.ForcesaveSetting = value;
        //await SaveForCurrentUser(setting);
        //await messageService.SendHeadersMessageAsync(MessageAction.DocumentsForcesave);
    }

    public bool GetStoreForcesave() => false;

    public void SetStoreForcesave(bool value)
    {
        //     if (!await global.IsDocSpaceAdministratorAsync)
        //     {
        //         throw new InvalidOperationException(FilesCommonResource.ErrorMessage_SecurityException);
        //     }
        //var setting = _settingsManager.Load<FilesSettings>();
        //setting.StoreForcesaveSetting = value;
        //_settingsManager.Save(setting);
        //await messageService.SendHeadersMessageAsync(MessageAction.DocumentsStoreForcesave);
    }

    public async Task<bool> GetRecentSection() => !(await LoadForCurrentUser()).HideRecentSetting;

    public async Task SetRecentSection(bool value)
    {        
        if (!authContext.IsAuthenticated)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var setting = await LoadForCurrentUser();
        setting.HideRecentSetting = !value;
        await SaveForCurrentUser(setting);
    }

    public async Task<bool> GetFavoritesSection() => !(await LoadForCurrentUser()).HideFavoritesSetting;

    public async Task SetFavoritesSection(bool value)
    {        
        if (!authContext.IsAuthenticated)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        
        var setting = await LoadForCurrentUser();
        setting.HideFavoritesSetting = !value;
        await SaveForCurrentUser(setting);
    }

    public async Task<bool> GetTemplatesSection() => !(await LoadForCurrentUser()).HideTemplatesSetting;

    public async Task SetTemplatesSection(bool value)
    {        
        if (!authContext.IsAuthenticated)
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
        var setting = await LoadForCurrentUser();
        setting.HideTemplatesSetting = !value;
        await SaveForCurrentUser(setting);
    }

    public async Task<bool> GetDownloadTarGz() => (await LoadForCurrentUser()).DownloadTarGzSetting;

    public async Task SetDownloadTarGz(bool value)
    {
        var setting = await LoadForCurrentUser();
        setting.DownloadTarGzSetting = value;
        await SaveForCurrentUser(setting);
    }

    public async Task<AutoCleanUpData> GetAutomaticallyCleanUp()
    {
        var setting = (await LoadForCurrentUser()).AutomaticallyCleanUpSetting;

        if (setting != null)
        {
            return setting;
        }

        setting = new AutoCleanUpData { IsAutoCleanUp = true, Gap = DateToAutoCleanUp.ThirtyDays };
        await SetAutomaticallyCleanUp(setting);

        return setting;
    }

    public async Task SetAutomaticallyCleanUp(AutoCleanUpData value)
    {
        var setting = await LoadForCurrentUser();
        setting.AutomaticallyCleanUpSetting = value;
        await SaveForCurrentUser(setting);
    }

    public async Task<List<FileShare>> GetDefaultSharingAccessRights()
    {
        var setting = (await LoadForCurrentUser()).DefaultSharingAccessRightsSetting;
        return setting ?? [FileShare.Read];
    }

    public async Task SetDefaultSharingAccessRights(List<FileShare> value)
    {
        List<FileShare> GetNormalizedList(List<FileShare> src)
        {
            if (src == null || !src.Any())
            {
                return null;
            }

            var res = new List<FileShare>();

            if (src.Contains(FileShare.FillForms))
            {
                res.Add(FileShare.FillForms);
            }

            if (src.Contains(FileShare.CustomFilter))
            {
                res.Add(FileShare.CustomFilter);
            }

            if (src.Contains(FileShare.Review))
            {
                res.Add(FileShare.Review);
            }

            if (src.Contains(FileShare.ReadWrite))
            {
                res.Add(FileShare.ReadWrite);
                return res;
            }

            if (src.Contains(FileShare.Comment))
            {
                res.Add(FileShare.Comment);
                return res;
            }

            res.Add(FileShare.Read);
            return res;
        }

        var setting = await LoadForCurrentUser();
        setting.DefaultSharingAccessRightsSetting = GetNormalizedList(value);
        await SaveForCurrentUser(setting);
    }

    private async Task<FilesSettings> Load()
    {
        return !authContext.IsAuthenticated ? _emptySettings : await settingsManager.LoadAsync<FilesSettings>();
    }

    private async Task Save(FilesSettings settings)
    {
        if (!authContext.IsAuthenticated)
        {
            return;
        }

        await settingsManager.SaveAsync(settings);
    }

    private async Task<FilesSettings> LoadForCurrentUser()
    {
        return !authContext.IsAuthenticated ? _emptySettings : await settingsManager.LoadForCurrentUserAsync<FilesSettings>();
    }

    private async Task SaveForCurrentUser(FilesSettings settings)
    {
        if (!authContext.IsAuthenticated)
        {
            return;
        }

        await settingsManager.SaveForCurrentUserAsync(settings);
    }
}
