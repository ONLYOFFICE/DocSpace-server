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

namespace ASC.Files.Core.Core.Thirdparty.WebDav;

[Scope]
internal class WebDavDaoBase(IServiceProvider serviceProvider,
    UserManager userManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    SetupInfo setupInfo,
    FileUtility fileUtility,
    TempPath tempPath,
    RegexDaoSelectorBase<WebDavResource, WebDavResource, WebDavResource> regexDaoSelectorBase) 
    : ThirdPartyProviderDao<WebDavResource, WebDavResource, WebDavResource>(serviceProvider, userManager, tenantManager, tenantUtil, dbContextFactory, setupInfo, fileUtility, tempPath, regexDaoSelectorBase),
        IDaoBase<WebDavResource, WebDavResource, WebDavResource>
{

    private AbstractProviderInfo<WebDavResource, WebDavResource, WebDavResource, MockLoginProvider> _providerInfo;
    private const string ErrorETag = "02b7fe8f9a0548d7b863cd0f9aeb062a";
    
    public void Init(string pathPrefix, IProviderInfo<WebDavResource, WebDavResource, WebDavResource> providerInfo)
    {
        PathPrefix = pathPrefix;
        ProviderInfo = providerInfo;
        _providerInfo = providerInfo as AbstractProviderInfo<WebDavResource, WebDavResource, WebDavResource, MockLoginProvider>;
    }

    public string GetName(WebDavResource item)
    {
        if (item == null)
        {
            return null;
        }
        
        return item.DisplayName ?? GetId(item)?.Split('/').Last();
    }

    public string GetId(WebDavResource item)
    {
        if (item?.Uri == null)
        {
            return null;
        }

        return IsRoot(item) ? item.Uri : HttpUtility.UrlDecode(item.Uri.TrimEnd('/'));
    }

    public bool IsRoot(WebDavResource folder)
    {
        return folder is { Uri: "/" };
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

    public string GetParentFolderId(WebDavResource item)
    {
        if (item == null || IsRoot(item))
        {
            return null;
        }

        var id = GetId(item);
        var pathLength = id.Length - GetName(item).Length;

        return id[..(pathLength > 1 ? pathLength - 1 : 0)];
    }

    public string MakeId(WebDavResource item)
    {
        return MakeId(GetId(item));
    }

    public override string MakeId(string path = null)
    {
        if (string.IsNullOrEmpty(path) || path == "/")
        {
            return PathPrefix;
        }

        return path.StartsWith('/') ? $"{PathPrefix}-{WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(path))}" : $"{PathPrefix}-{path}";
    }

    public string MakeFolderTitle(WebDavResource folder)
    {
        if (folder == null || IsRoot(folder))
        {
            return ProviderInfo.CustomerTitle;
        }

        return Global.ReplaceInvalidCharsAndTruncate(GetName(folder));
    }

    public string MakeFileTitle(WebDavResource file)
    {
        var name = GetName(file);
        
        return string.IsNullOrEmpty(name) ? ProviderInfo.ProviderKey : Global.ReplaceInvalidCharsAndTruncate(name);
    }

    public Folder<string> ToFolder(WebDavResource webDavFolder)
    {
        if (webDavFolder == null)
        {
            return null;
        }
        
        if (webDavFolder.ETag == ErrorETag && !string.IsNullOrEmpty(webDavFolder.DisplayName))
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
        SetFolderType(folder, isRoot);
        SetDateTime(webDavFolder, folder);

        return folder;
    }

    public File<string> ToFile(WebDavResource webDavFile)
    {
        if (webDavFile == null)
        {
            return null;
        }

        if (webDavFile.ETag == ErrorETag && !string.IsNullOrEmpty(webDavFile.DisplayName))
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
        SetDateTime(webDavFile, file);

        return file;
    }

    private void SetDateTime(WebDavResource webDavFile, FileEntry file)
    {
        var creationDate = webDavFile.CreationDate?.ToUniversalTime();
        var lastModifiedDate = webDavFile.LastModifiedDate?.ToUniversalTime();
        
        if (creationDate.HasValue && creationDate.Value != DateTime.MinValue)
        {
            file.CreateOn = _tenantUtil.DateTimeFromUtc(creationDate.Value);
        }
        if (lastModifiedDate.HasValue && lastModifiedDate.Value != DateTime.MinValue)
        {
            file.ModifiedOn = _tenantUtil.DateTimeFromUtc(lastModifiedDate.Value);
        }
    }

    public async Task<Folder<string>> GetRootFolderAsync()
    {
        return ToFolder(await GetFolderAsync(string.Empty));
    }

    public Task<WebDavResource> GetFolderAsync(string folderId)
    {
        var id = MakeThirdId(folderId);
        
        try
        {
            return _providerInfo.GetFolderAsync(id);
        }
        catch (Exception e)
        {
            return Task.FromResult(MakeErrorResource(id, e));
        }
    }

    public Task<WebDavResource> GetFileAsync(string fileId)
    {
        var id = MakeThirdId(fileId);

        try
        {
            return _providerInfo.GetFileAsync(id);
        }
        catch (Exception e)
        {
            return Task.FromResult(MakeErrorResource(id, e));
        }
    }

    private static WebDavResource MakeErrorResource(string id, Exception e)
    {
        var builder = new WebDavResource.Builder();
        builder.WithUri(id);
        builder.WithETag(ErrorETag);
        builder.WithDisplayName(e.Message);

        return builder.Build();
    }

    public override async Task<IEnumerable<string>> GetChildrenAsync(string folderId)
    {
        var items = await GetItemsAsync(folderId);

        return items.Select(MakeId);
    }

    public async Task<List<WebDavResource>> GetItemsAsync(string parentId, bool? folder = null)
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

    public async Task<string> GetAvailableTitleAsync(string requestTitle, string parentFolderId, Func<string, string, Task<bool>> isExist)
    {
        if (!await isExist(requestTitle, parentFolderId))
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

        while (await isExist(requestTitle, parentFolderId))
        {
            requestTitle = re.Replace(requestTitle, MatchEvaluator);
        }

        return requestTitle;
    }
    
    private Folder<string> ToErrorFolder(WebDavResource webDavResource)
    {
        var folder = GetFolder();
        BuildErrorEntry(webDavResource, folder);

        return folder;
    }

    private File<string> ToErrorFile(WebDavResource webDavResource)
    {
        var file = GetFile();
        BuildErrorEntry(webDavResource, file);
        
        return file;
    }

    private void BuildErrorEntry(WebDavResource webDavResource, FileEntry<string> file)
    {
        file.Id = MakeId(webDavResource.Uri);
        file.ParentId = null;
        file.CreateOn = _tenantUtil.DateTimeNow();
        file.ModifiedOn = _tenantUtil.DateTimeNow();
        file.Error = webDavResource.DisplayName;
    }

    private static string MatchEvaluator(Match match)
    {
        var index = Convert.ToInt32(match.Groups[2].Value);
        var staticText = match.Value[$" ({index})".Length..];

        return $" ({index + 1}){staticText}";
    }
}