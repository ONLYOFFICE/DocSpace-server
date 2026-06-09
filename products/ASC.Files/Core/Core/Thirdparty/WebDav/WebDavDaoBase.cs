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
    : AbstractWebDavDaoBase<WebDavEntry>(daoFactory, serviceProvider, userManager, tenantManager, tenantUtil, dbContextFactory, fileUtility, regexDaoSelectorBase),
        IDaoBase<WebDavEntry, WebDavEntry, WebDavEntry>
{
    protected override WebDavEntry CreateErrorEntry(string error, string id) => new ErrorWebDavEntry(error, id);

    private sealed class ErrorWebDavEntry(string errorMessage, string id) : WebDavEntry, IErrorItem
    {
        public string Error { get; } = errorMessage;
        public string ErrorId { get; } = id;
    }
}

internal abstract class AbstractWebDavDaoBase<TEntry>(
    IDaoFactory daoFactory,
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    FileUtility fileUtility,
    RegexDaoSelectorBase<TEntry, TEntry, TEntry> regexDaoSelectorBase)
    : ThirdPartyProviderDao<TEntry, TEntry, TEntry>(daoFactory, serviceProvider, userManager, tenantManager, tenantUtil, dbContextFactory, fileUtility, regexDaoSelectorBase),
        IDaoBase<TEntry, TEntry, TEntry>
    where TEntry : WebDavEntry
{

    private AbstractProviderInfo<TEntry, TEntry, TEntry> _providerInfo;

    public void Init(string pathPrefix, IProviderInfo<TEntry, TEntry, TEntry> providerInfo)
    {
        PathPrefix = pathPrefix;
        ProviderInfo = providerInfo;
        _providerInfo = providerInfo as AbstractProviderInfo<TEntry, TEntry, TEntry>;
    }

    public string GetName(TEntry item)
    {
        return item.DisplayName;
    }

    public string GetId(TEntry item)
    {
        return item.Id;
    }

    public bool IsFile(TEntry item)
    {
        return !item.IsCollection;
    }


    public bool IsRoot(TEntry folder)
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

    public string GetParentFolderId(TEntry item)
    {
        if (item == null || IsRoot(item))
        {
            return null;
        }

        var id = GetId(item);
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        var index = id.LastIndexOf('/');

        return index == -1 ? null : id[..index];
    }

    public string MakeId(TEntry item)
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

    public string MakeFolderTitle(TEntry folder)
    {
        if (folder == null || IsRoot(folder))
        {
            return ProviderInfo.CustomerTitle;
        }

        return Global.ReplaceInvalidCharsAndTruncate(GetName(folder));
    }

    public string MakeFileTitle(TEntry file)
    {
        var name = GetName(file);

        return string.IsNullOrEmpty(name) ? ProviderInfo.ProviderKey : Global.ReplaceInvalidCharsAndTruncate(name);
    }

    protected abstract TEntry CreateErrorEntry(string error, string id);

    public Folder<string> ToFolder(TEntry webDavFolder)
    {
        if (webDavFolder == null)
        {
            return null;
        }

        if (webDavFolder is IErrorItem)
        {
            return ToErrorFolder(webDavFolder);
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
        folder.SettingsCover = ProviderInfo.Cover;

        ProcessFolderAsRoom(folder);
        SetDateTime(webDavFolder, folder);

        return folder;
    }

    public File<string> ToFile(TEntry webDavFile)
    {
        if (webDavFile == null)
        {
            return null;
        }

        if (webDavFile is IErrorItem)
        {
            return ToErrorFile(webDavFile);
        }

        var file = GetFile();

        file.Id = MakeId(webDavFile);
        file.ContentLength = webDavFile.ContentLength ?? 0;
        file.ParentId = MakeId(GetParentFolderId(webDavFile));
        file.Title = MakeFileTitle(webDavFile);
        file.ThumbnailStatus = Thumbnail.Created;
        file.Encrypted = ProviderInfo.Private;
        file.Category = (int)FilterType.PdfForm;
        SetDateTime(webDavFile, file);

        return file;
    }

    private void SetDateTime(TEntry webDavEntry, FileEntry fileEntry)
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

    public async Task<TEntry> CreateFolderAsync(string title, string folderId)
    {
        return await _providerInfo.CreateFolderAsync(title, MakeThirdId(folderId), GetId);
    }

    public async Task<TEntry> GetFolderAsync(string folderId)
    {
        var id = MakeThirdId(folderId);

        try
        {
            return await _providerInfo.GetFolderAsync(id);
        }
        catch (Exception e)
        {
            return CreateErrorEntry(e.Message, folderId);
        }
    }

    public async Task<TEntry> GetFileAsync(string fileId)
    {
        var id = MakeThirdId(fileId);

        try
        {
            return await _providerInfo.GetFileAsync(id);
        }
        catch (Exception e)
        {
            return CreateErrorEntry(e.Message, fileId);
        }
    }

    public override async Task<IEnumerable<string>> GetChildrenAsync(string folderId)
    {
        var items = await GetItemsAsync(folderId);

        return items.Select(MakeId);
    }

    public async Task<List<TEntry>> GetItemsAsync(string parentId, bool? folder = null)
    {
        var id = MakeThirdId(parentId);
        var items = (await _providerInfo.GetItemsAsync(id, GetId, IsFile)).Cast<TEntry>();

        if (!folder.HasValue)
        {
            return items.ToList();
        }

        return folder.Value
            ? items.Where(x => x.IsCollection).ToList()
            : items.Where(x => !x.IsCollection).ToList();
    }

    private File<string> ToErrorFile(TEntry errorEntry)
    {
        if (errorEntry is not IErrorItem errorItem)
        {
            return null;
        }

        var file = GetErrorFile(new ErrorEntry(errorItem.Error, errorItem.ErrorId));
        file.Title = MakeFileTitle(errorEntry);

        return file;
    }

    private Folder<string> ToErrorFolder(TEntry errorEntry)
    {
        if (errorEntry is not IErrorItem errorItem)
        {
            return null;
        }

        var folder = GetErrorFolder(new ErrorEntry(errorItem.Error, errorItem.ErrorId));
        folder.Title = MakeFolderTitle(errorEntry);

        return folder;
    }
}
