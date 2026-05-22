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

namespace ASC.Files.Thirdparty.OneDrive;

[Scope(typeof(IDaoBase<Item, Item, Item>))]
internal class OneDriveDaoBase(
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    FileUtility fileUtility,
    RegexDaoSelectorBase<Item, Item, Item> regexDaoSelectorBase)
    : ThirdPartyProviderDao<Item, Item, Item>(daoFactory, serviceProvider, userManager, tenantManager, tenantUtil, dbContextFactory, fileUtility, regexDaoSelectorBase), IDaoBase<Item, Item, Item>
{
    private OneDriveProviderInfo _providerInfo;

    public void Init(string pathPrefix, IProviderInfo<Item, Item, Item> providerInfo)
    {
        PathPrefix = pathPrefix;
        ProviderInfo = providerInfo;
        _providerInfo = providerInfo as OneDriveProviderInfo;
    }

    public string GetName(Item item)
    {
        return item.Name;
    }

    public string GetId(Item item)
    {
        return item.Id;
    }

    public bool IsFile(Item item)
    {
        return item.Folder == null;
    }

    public string MakeThirdId(object entryId)
    {
        var id = Convert.ToString(entryId, CultureInfo.InvariantCulture);

        return string.IsNullOrEmpty(id)
                   ? string.Empty
                   : id.TrimStart('/');
    }

    public string GetParentFolderId(Item onedriveItem)
    {
        return onedriveItem == null || IsRoot(onedriveItem)
                   ? null
                   : onedriveItem.ParentReference.Path.Equals(OneDriveStorage.RootPath, StringComparison.InvariantCultureIgnoreCase)
                       ? string.Empty
                       : onedriveItem.ParentReference.Id;
    }

    public string MakeId(Item onedriveItem)
    {
        var id = string.Empty;
        if (onedriveItem != null)
        {
            id = onedriveItem.Id;
        }

        return MakeId(id);
    }

    public override string MakeId(string id = null)
    {
        var i = string.IsNullOrEmpty(id) ? "" : "-|" + id.TrimStart('/');

        return $"{PathPrefix}{i}";
    }

    public string MakeFileTitle(Item onedriveItem)
    {
        if (onedriveItem == null || IsRoot(onedriveItem))
        {
            return ProviderInfo.CustomerTitle;
        }

        return Global.ReplaceInvalidCharsAndTruncate(onedriveItem.Name);
    }

    public string MakeFolderTitle(Item onedriveItem)
    {
        if (onedriveItem == null || IsRoot(onedriveItem))
        {
            return ProviderInfo.CustomerTitle;
        }

        return Global.ReplaceInvalidCharsAndTruncate(onedriveItem.Name);
    }

    public Folder<string> ToFolder(Item onedriveFolder)
    {
        switch (onedriveFolder)
        {
            case null:
                return null;
            case ErrorItem item:
                return ToErrorFolder(item);
        }

        if (onedriveFolder.Folder == null)
        {
            return null;
        }

        var isRoot = IsRoot(onedriveFolder);

        var folder = GetFolder();

        folder.Id = MakeId(isRoot ? string.Empty : onedriveFolder.Id);
        folder.ParentId = isRoot ? null : MakeId(GetParentFolderId(onedriveFolder));
        folder.Title = MakeFolderTitle(onedriveFolder);
        folder.CreateOn = isRoot ? ProviderInfo.CreateOn : onedriveFolder.CreatedDateTime.HasValue
            ? _tenantUtil.DateTimeFromUtc(onedriveFolder.CreatedDateTime.Value.DateTime) : default;
        folder.ModifiedOn = isRoot ? ProviderInfo.ModifiedOn : onedriveFolder.LastModifiedDateTime.HasValue
            ? _tenantUtil.DateTimeFromUtc(onedriveFolder.LastModifiedDateTime.Value.DateTime) : default;
        folder.SettingsPrivate = ProviderInfo.Private;
        folder.SettingsHasLogo = ProviderInfo.HasLogo;
        folder.SettingsColor = ProviderInfo.Color;
        folder.SettingsCover = ProviderInfo.Cover;
        
        ProcessFolderAsRoom(folder);

        return folder;
    }

    public bool IsRoot(Item onedriveFolder)
    {
        return onedriveFolder.ParentReference?.Id == null;
    }

    private File<string> ToErrorFile(ErrorItem onedriveFile)
    {
        if (onedriveFile == null)
        {
            return null;
        }

        var file = GetErrorFile(new ErrorEntry(onedriveFile.Error, onedriveFile.ErrorId));

        file.Title = MakeFileTitle(onedriveFile);

        return file;
    }

    private Folder<string> ToErrorFolder(ErrorItem onedriveFolder)
    {
        if (onedriveFolder == null)
        {
            return null;
        }

        var folder = GetErrorFolder(new ErrorEntry(onedriveFolder.Error, onedriveFolder.ErrorId));

        folder.Title = MakeFolderTitle(onedriveFolder);

        return folder;
    }

    public File<string> ToFile(Item onedriveFile)
    {
        switch (onedriveFile)
        {
            case null:
                return null;
            case ErrorItem item:
                return ToErrorFile(item);
        }

        if (onedriveFile.File == null)
        {
            return null;
        }

        var file = GetFile();

        file.Id = MakeId(onedriveFile.Id);
        file.ContentLength = onedriveFile.Size ?? 0;
        file.CreateOn = onedriveFile.CreatedDateTime.HasValue ? _tenantUtil.DateTimeFromUtc(onedriveFile.CreatedDateTime.Value.DateTime) : default;
        file.ParentId = MakeId(GetParentFolderId(onedriveFile));
        file.ModifiedOn = onedriveFile.LastModifiedDateTime.HasValue ? _tenantUtil.DateTimeFromUtc(onedriveFile.LastModifiedDateTime.Value.DateTime) : default;
        file.Title = MakeFileTitle(onedriveFile);
        file.ThumbnailStatus = Thumbnail.Created;
        file.Encrypted = ProviderInfo.Private;

        return file;
    }

    public async Task<Folder<string>> GetRootFolderAsync()
    {
        return ToFolder(await GetFolderAsync(""));
    }

    public async Task<Item> GetFileAsync(string itemId)
    {
        var onedriveId = MakeThirdId(itemId);
        try
        {
            return await _providerInfo.GetFileAsync(onedriveId);
        }
        catch (Exception ex)
        {
            return new ErrorItem(ex, onedriveId);
        }
    }

    public async Task<Item> CreateFolderAsync(string title, string folderId)
    {
        return await _providerInfo.CreateFolderAsync(title, MakeThirdId(folderId), GetId);
    }

    public async Task<Item> GetFolderAsync(string itemId)
    {
        var onedriveId = MakeThirdId(itemId);
        try
        {
            return await _providerInfo.GetFolderAsync(onedriveId);
        }
        catch (Exception ex)
        {
            return new ErrorItem(ex, onedriveId);
        }
    }

    public override async Task<IEnumerable<string>> GetChildrenAsync(string folderId)
    {
        var items = await GetItemsAsync(folderId);

        return items.Select(entry => MakeId(entry.Id));
    }

    public async Task<List<Item>> GetItemsAsync(string parentId, bool? folder = null)
    {
        var onedriveFolderId = MakeThirdId(parentId);
        var items = await _providerInfo.GetItemsAsync(onedriveFolderId, GetId, IsFile);

        if (!folder.HasValue)
        {
            return items;
        }

        return folder.Value ? items.Where(i => i.Folder != null).ToList() : items.Where(i => i.File != null).ToList();
    }

    private sealed class ErrorItem : Item, IErrorItem
    {
        public string Error { get; }
        public string ErrorId { get; }

        public ErrorItem(Exception e, object id)
        {
            ErrorId = id.ToString();
            if (e != null)
            {
                Error = e.Message;
            }
        }
    }
}