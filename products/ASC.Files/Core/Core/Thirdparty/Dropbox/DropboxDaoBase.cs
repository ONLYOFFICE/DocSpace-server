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

namespace ASC.Files.Thirdparty.Dropbox;

[Scope]
internal class DropboxDaoBase(
    IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    SetupInfo setupInfo,
    FileUtility fileUtility,
    TempPath tempPath,
    RegexDaoSelectorBase<FileMetadata, FolderMetadata, Metadata> regexDaoSelectorBase)
    : ThirdPartyProviderDao<FileMetadata, FolderMetadata, Metadata>(serviceProvider, userManager, tenantManager,
            tenantUtil, dbContextFactory, setupInfo, fileUtility, tempPath, regexDaoSelectorBase),
        IDaoBase<FileMetadata, FolderMetadata, Metadata>
{
    private DropboxProviderInfo _providerInfo;

    public void Init(string pathPrefix, IProviderInfo<FileMetadata, FolderMetadata, Metadata> providerInfo)
    {
        PathPrefix = pathPrefix;
        ProviderInfo = providerInfo;
        _providerInfo = providerInfo as DropboxProviderInfo;
    }

    public string GetId(Metadata dropboxItem)
    {
        string path = null;
        if (dropboxItem != null)
        {
            path = dropboxItem.PathDisplay;
        }

        return path;
    }

    public string GetName(Metadata dropboxItem)
    {
        string name = null;
        if (dropboxItem != null)
        {
            name = dropboxItem.Name;
        }

        return name;
    }

    public string GetParentFolderId(Metadata dropboxItem)
    {
        if (dropboxItem == null || IsRoot(dropboxItem.AsFolder))
        {
            return null;
        }

        var pathLength = dropboxItem.PathDisplay.Length - dropboxItem.Name.Length;

        return dropboxItem.PathDisplay[..(pathLength > 1 ? pathLength - 1 : 0)];
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
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(id));
        }
        catch
        {
            return id;
        }
    }

    public string MakeId(Metadata dropboxItem)
    {
        return MakeId(GetId(dropboxItem));
    }

    public override string MakeId(string path = null)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return PathPrefix;
        }

        return path.StartsWith('/') ? $"{PathPrefix}-{WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(path))}" : $"{PathPrefix}-{path}";
    }

    public string MakeFolderTitle(FolderMetadata dropboxFolder)
    {
        if (dropboxFolder == null || IsRoot(dropboxFolder))
        {
            return ProviderInfo.CustomerTitle;
        }

        return Global.ReplaceInvalidCharsAndTruncate(dropboxFolder.Name);
    }

    public string MakeFileTitle(FileMetadata dropboxFile)
    {
        if (dropboxFile == null || string.IsNullOrEmpty(dropboxFile.Name))
        {
            return ProviderInfo.ProviderKey;
        }

        return Global.ReplaceInvalidCharsAndTruncate(dropboxFile.Name);
    }

    public Folder<string> ToFolder(FolderMetadata dropboxFolder)
    {
        switch (dropboxFolder)
        {
            case null:
                return null;
            case ErrorFolder errorFolder:
                return ToErrorFolder(errorFolder);
        }

        var isRoot = IsRoot(dropboxFolder);

        var folder = GetFolder();

        folder.Id = MakeId(dropboxFolder);
        folder.ParentId = isRoot ? null : MakeId(GetParentFolderId(dropboxFolder));
        folder.CreateOn = isRoot ? ProviderInfo.CreateOn : default;
        folder.ModifiedOn = isRoot ? ProviderInfo.ModifiedOn : default;
        folder.Title = MakeFolderTitle(dropboxFolder);
        folder.SettingsPrivate = ProviderInfo.Private;
        folder.SettingsHasLogo = ProviderInfo.HasLogo;
        folder.SettingsColor = ProviderInfo.Color;
        ProcessFolderAsRoom(folder);

        if (folder.CreateOn != DateTime.MinValue && folder.CreateOn.Kind == DateTimeKind.Utc)
        {
            folder.CreateOn = _tenantUtil.DateTimeFromUtc(folder.CreateOn);
        }

        if (folder.ModifiedOn != DateTime.MinValue && folder.ModifiedOn.Kind == DateTimeKind.Utc)
        {
            folder.ModifiedOn = _tenantUtil.DateTimeFromUtc(folder.ModifiedOn);
        }

        return folder;
    }

    public bool IsRoot(FolderMetadata dropboxFolder)
    {
        return dropboxFolder is { Id: "/" };
    }

    private File<string> ToErrorFile(ErrorFile dropboxFile)
    {
        if (dropboxFile == null)
        {
            return null;
        }

        var file = GetErrorFile(new ErrorEntry(dropboxFile.ErrorId, dropboxFile.Error));

        file.Title = MakeFileTitle(dropboxFile);

        return file;
    }

    private Folder<string> ToErrorFolder(ErrorFolder dropboxFolder)
    {
        if (dropboxFolder == null)
        {
            return null;
        }

        var folder = GetErrorFolder(new ErrorEntry(dropboxFolder.Error, dropboxFolder.ErrorId));

        folder.Title = MakeFolderTitle(dropboxFolder);

        return folder;
    }

    public File<string> ToFile(FileMetadata dropboxFile)
    {
        switch (dropboxFile)
        {
            case null:
                return null;
            case ErrorFile errorFile:
                return ToErrorFile(errorFile);
        }

        var file = GetFile();

        file.Id = MakeId(dropboxFile);
        file.ContentLength = (long)dropboxFile.Size;
        file.CreateOn = _tenantUtil.DateTimeFromUtc(dropboxFile.ServerModified);
        file.ParentId = MakeId(GetParentFolderId(dropboxFile));
        file.ModifiedOn = _tenantUtil.DateTimeFromUtc(dropboxFile.ServerModified);
        file.Title = MakeFileTitle(dropboxFile);
        file.ThumbnailStatus = Thumbnail.Created;
        file.Encrypted = ProviderInfo.Private;

        return file;
    }

    public async Task<Folder<string>> GetRootFolderAsync()
    {
        return ToFolder(await GetFolderAsync(string.Empty));
    }
    
    public async Task<FolderMetadata> CreateFolderAsync(string title, string folderId)
    {
        return await _providerInfo.CreateFolderAsync(title, MakeThirdId(folderId), GetId);
    }

    public async Task<FolderMetadata> GetFolderAsync(string folderId)
    {
        var dropboxFolderId = MakeThirdId(folderId);
        try
        {
            var folder = await _providerInfo.GetFolderAsync(dropboxFolderId);
            return folder;
        }
        catch (Exception ex)
        {
            return new ErrorFolder(ex, dropboxFolderId);
        }
    }

    public Task<FileMetadata> GetFileAsync(string fileId)
    {
        var dropboxFileId = MakeThirdId(fileId);
        try
        {
            return _providerInfo.GetFileAsync(dropboxFileId);
        }
        catch (Exception ex)
        {
            return Task.FromResult<FileMetadata>(new ErrorFile(ex, dropboxFileId));
        }
    }

    public override async Task<IEnumerable<string>> GetChildrenAsync(string folderId)
    {
        var items = await GetItemsAsync(folderId);

        return items.Select(MakeId);
    }

    public async Task<List<Metadata>> GetItemsAsync(string parentId, bool? folder = null)
    {
        var dropboxFolderId = MakeThirdId(parentId);
        var items = await _providerInfo.GetItemsAsync(dropboxFolderId);

        if (!folder.HasValue)
        {
            return items;
        }

        return folder.Value ? items.Where(i => i.AsFolder != null).ToList() : items.Where(i => i.AsFile != null).ToList();
    }

    private sealed class ErrorFolder : FolderMetadata, IErrorItem
    {
        public string Error { get; }
        public string ErrorId { get; }

        public ErrorFolder(Exception e, object id)
        {
            ErrorId = id.ToString();
            if (e != null)
            {
                Error = e.Message;
            }
        }
    }

    private sealed class ErrorFile : FileMetadata, IErrorItem
    {
        public string Error { get; }
        public string ErrorId { get; }

        public ErrorFile(Exception e, object id)
        {
            ErrorId = id.ToString();
            if (e != null)
            {
                Error = e.Message;
            }
        }
    }

    public async Task<string> GetAvailableTitleAsync(string requestTitle, string parentFolderPath, Func<string, string, Task<bool>> isExist)
    {
        if (!await isExist(requestTitle, parentFolderPath))
        {
            return requestTitle;
        }

        var re = new Regex(@"( \(((?<index>[0-9])+)\)(\.[^\.]*)?)$");
        var match = re.Match(requestTitle);

        if (!match.Success)
        {
            var insertIndex = requestTitle.Length;
            if (requestTitle.LastIndexOf('.') != -1)
            {
                insertIndex = requestTitle.LastIndexOf('.');
            }

            requestTitle = requestTitle.Insert(insertIndex, " (1)");
        }

        while (await isExist(requestTitle, parentFolderPath))
        {
            requestTitle = re.Replace(requestTitle, MatchEvaluator);
        }

        return requestTitle;
    }

    private static string MatchEvaluator(Match match)
    {
        var index = Convert.ToInt32(match.Groups[2].Value);
        var staticText = match.Value[$" ({index})".Length..];

        return $" ({index + 1}){staticText}";
    }
}
