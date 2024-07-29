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

namespace ASC.Files.Core.Core.Thirdparty.WebDav;

[Scope(typeof(IDaoBase<WebDavEntry, WebDavEntry, WebDavEntry>))]
internal class WebDavDaoBase(
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    FileUtility fileUtility,
    RegexDaoSelectorBase<WebDavEntry, WebDavEntry, WebDavEntry> regexDaoSelectorBase) 
    : ThirdPartyProviderDao<WebDavEntry, WebDavEntry, WebDavEntry>(daoFactory, serviceProvider, userManager, tenantManager, tenantUtil, dbContextFactory, fileUtility, regexDaoSelectorBase),
        IDaoBase<WebDavEntry, WebDavEntry, WebDavEntry>
{

    private WebDavProviderInfo _providerInfo;
    
    public void Init(string pathPrefix, IProviderInfo<WebDavEntry, WebDavEntry, WebDavEntry> providerInfo)
    {
        PathPrefix = pathPrefix;
        ProviderInfo = providerInfo;
        _providerInfo = providerInfo as WebDavProviderInfo;
    }

    public string GetName(WebDavEntry item)
    {
        return item.DisplayName;
    }

    public string GetId(WebDavEntry item)
    {
        return item.Id;
    }

    public bool IsRoot(WebDavEntry folder)
    {
        return IsRoot(folder.Id);
    }
    
    private static bool IsRoot(string folderId)
    {
        return folderId == "/";
    }

    public string MakeThirdId(object entryId)
    {
        var id = Convert.ToString(entryId, CultureInfo.InvariantCulture);

        if (string.IsNullOrEmpty(id) || id.StartsWith('/'))
        {
            return id;
        }

        try
        {
            return $"/{Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(id)).Trim('/')}";
        }
        catch
        {
            return id;
        }
    }
    
    public string GetParentFolderId(WebDavEntry item)
    {
        if (item == null || IsRoot(item))
        {
            return null;
        }
        
        var id = GetId(item);
        var index = id.LastIndexOf('/');
    
        return index == -1 ? null : id[..index];
    }

    public string MakeId(WebDavEntry item)
    {
        return MakeId(GetId(item));
    }

    public override string MakeId(string path = null)
    {
        if (string.IsNullOrEmpty(path) || IsRoot(path))
        {
            return PathPrefix;
        }

        return path.StartsWith('/') 
            ? $"{PathPrefix}-{WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(path))}" 
            : $"{PathPrefix}-{path}";
    }

    public string MakeFolderTitle(WebDavEntry folder)
    {
        if (folder == null || IsRoot(folder))
        {
            return ProviderInfo.CustomerTitle;
        }

        return Global.ReplaceInvalidCharsAndTruncate(GetName(folder));
    }

    public string MakeFileTitle(WebDavEntry file)
    {
        var name = GetName(file);
        
        return string.IsNullOrEmpty(name) ? ProviderInfo.ProviderKey : Global.ReplaceInvalidCharsAndTruncate(name);
    }

    public Folder<string> ToFolder(WebDavEntry webDavFolder)
    {
        switch (webDavFolder)
        {
            case null:
                return null;
            case ErrorWebDavEntry errorEntry:
                return ToErrorFolder(errorEntry);
        }

        var isRoot = IsRoot(webDavFolder);

        var folder = GetFolder();

        folder.Id = MakeId(webDavFolder);
        folder.ParentId = isRoot ? null : MakeId(GetParentFolderId(webDavFolder));
        folder.CreateOn = isRoot ? ProviderInfo.CreateOn : default;
        folder.ModifiedOn = isRoot ? ProviderInfo.ModifiedOn : default;
        folder.Title = MakeFolderTitle(webDavFolder);
        folder.SettingsPrivate = ProviderInfo.Private;
        folder.SettingsHasLogo = ProviderInfo.HasLogo;
        folder.SettingsColor = ProviderInfo.Color;
        ProcessFolderAsRoom(folder);
        SetDateTime(webDavFolder, folder);

        return folder;
    }

    public File<string> ToFile(WebDavEntry webDavFile)
    {
        switch (webDavFile)
        {
            case null:
                return null;
            case ErrorWebDavEntry errorEntry:
                return ToErrorFile(errorEntry);
        }

        var file = GetFile();

        file.Id = MakeId(webDavFile);
        file.ContentLength = webDavFile.ContentLength ?? 0;
        file.ParentId = MakeId(GetParentFolderId(webDavFile));
        file.Title = MakeFileTitle(webDavFile);
        file.ThumbnailStatus = Thumbnail.Created;
        file.Encrypted = ProviderInfo.Private;
        file.Shared = ProviderInfo.FolderType is FolderType.PublicRoom;
        SetDateTime(webDavFile, file);

        return file;
    }

    private void SetDateTime(WebDavEntry webDavEntry, FileEntry fileEntry)
    {
        if (webDavEntry.CreationDate != DateTime.MinValue)
        {
            fileEntry.CreateOn = _tenantUtil.DateTimeFromUtc(webDavEntry.CreationDate);
        }
        if (webDavEntry.LastModifiedDate != DateTime.MinValue)
        {
            fileEntry.ModifiedOn = _tenantUtil.DateTimeFromUtc(webDavEntry.LastModifiedDate);
        }
    }

    public async Task<Folder<string>> GetRootFolderAsync()
    {
        return ToFolder(await GetFolderAsync(string.Empty));
    }

    public async Task<WebDavEntry> CreateFolderAsync(string title, string folderId)
    {
        return await _providerInfo.CreateFolderAsync(title, MakeThirdId(folderId), GetId);
    }

    public async Task<WebDavEntry> GetFolderAsync(string folderId)
    {
        var id = MakeThirdId(folderId);
        
        try
        {
            return await _providerInfo.GetFolderAsync(id);
        }
        catch (Exception e)
        {
            return new ErrorWebDavEntry(e.Message, folderId);
        }
    }

    public async Task<WebDavEntry> GetFileAsync(string fileId)
    {
        var id = MakeThirdId(fileId);

        try
        {
            return await _providerInfo.GetFileAsync(id);
        }
        catch (Exception e)
        {
            return new ErrorWebDavEntry(e.Message, fileId);
        }
    }
    
    public override async Task<IEnumerable<string>> GetChildrenAsync(string folderId)
    {
        var items = await GetItemsAsync(folderId);

        return items.Select(MakeId);
    }

    public async Task<List<WebDavEntry>> GetItemsAsync(string parentId, bool? folder = null)
    {
        var id = MakeThirdId(parentId);
        var items = await _providerInfo.GetItemsAsync(id);

        if (!folder.HasValue)
        {
            return items;
        }

        return folder.Value 
            ? items.Where(x => x.IsCollection).ToList() 
            : items.Where(x => !x.IsCollection).ToList();
    }
    
    private File<string> ToErrorFile(ErrorWebDavEntry errorEntry)
    {
        if (errorEntry == null)
        {
            return null;
        }

        var file = GetErrorFile(new ErrorEntry(errorEntry.Error, errorEntry.ErrorId));
        file.Title = MakeFileTitle(errorEntry);

        return file;
    }
    
    private Folder<string> ToErrorFolder(ErrorWebDavEntry errorEntry)
    {
        if (errorEntry == null)
        {
            return null;
        }

        var folder = GetErrorFolder(new ErrorEntry(errorEntry.Error, errorEntry.ErrorId));
        folder.Title = MakeFolderTitle(errorEntry);

        return folder;
    }

    private class ErrorWebDavEntry(string errorMessage, string id) : WebDavEntry, IErrorItem
    {
        public string Error { get; } = errorMessage;
        public string ErrorId { get; } = id;
    }
}