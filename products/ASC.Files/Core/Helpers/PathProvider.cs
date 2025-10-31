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

namespace ASC.Web.Files.Classes;

[Scope]
public class PathProvider(WebImageSupplier webImageSupplier,
    IDaoFactory daoFactory,
    CommonLinkUtility commonLinkUtility,
    FilesLinkUtility filesLinkUtility,
    EmailValidationKeyProvider emailValidationKeyProvider,
    GlobalStore globalStore,
    ExternalShare externalShare)
{
    public static readonly string ProjectVirtualPath = "~/Products/Projects/TMDocs.aspx";
    public static readonly string StartURL = FilesLinkUtility.FilesBaseVirtualPath;

    public string GetImagePath(string imgFileName)
    {
        return webImageSupplier.GetAbsoluteWebPath(imgFileName, ProductEntryPoint.ID);
    }

    public string RoomUrlString
    {
        get { return "/rooms/shared/{0}/filter?withSubfolders={1}&folder={0}&count=100&page=1&sortby=DateAndTime&sortorder=descending"; }
    }
    
    public string AgentUrlString
    {
        get { return "/ai-agents/{0}/chat?folder={0}"; }
    }

    public string GetRoomsUrl(int roomId, bool withSubfolders = true)
    {
        return commonLinkUtility.GetFullAbsolutePath(string.Format(RoomUrlString, roomId, withSubfolders.ToString().ToLowerInvariant()));//ToDo
    }

    public string GetRoomsUrl(string roomId, bool withSubfolders = true)
    {
        return commonLinkUtility.GetFullAbsolutePath(string.Format(RoomUrlString, roomId, withSubfolders.ToString().ToLowerInvariant()));//ToDo
    }
    
    public string GetAgentUrl(string agentId)
    {
        return commonLinkUtility.GetFullAbsolutePath(string.Format(AgentUrlString, agentId));
    }

    public string GetFolderUrl<T>(Folder<T> folder, string key = null)
    {
        if (folder == null)
        {
            throw new ArgumentNullException(nameof(folder), FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        var result = "";
        var urlPathEncode = HttpUtility.UrlPathEncode(folder.Id.ToString());
        switch (folder.RootFolderType)
        {
            case FolderType.USER:
                result = string.Format($"rooms/personal/filter?folder={urlPathEncode}");
                break;
            case FolderType.Recent:
                result = string.Format($"recent/filter?folder={urlPathEncode}");
                break;
            case FolderType.Archive:
                result = string.Format($"rooms/archived/filter?folder={urlPathEncode}");
                break;
            case FolderType.SHARE:
                result = string.Format($"shared-with-me/filter?folder={urlPathEncode}");
                break;
            case FolderType.VirtualRooms:
            case FolderType.RoomTemplates:
                result = string.Format($"rooms/shared/{urlPathEncode}/filter?folder={urlPathEncode}");
                break;
        }

        if (!string.IsNullOrEmpty(key))
        {
            result += $"&key={key}";
        }

        return commonLinkUtility.GetFullAbsolutePath(string.Format($"{filesLinkUtility.FilesBaseAbsolutePath}{result}"));
    }

    public async Task<string> GetFolderUrlByIdAsync<T>(T folderId, string key = null)
    {
        var folder = await daoFactory.GetFolderDao<T>().GetFolderAsync(folderId);

        return GetFolderUrl(folder, key);
    }

    public string GetFileStreamUrl<T>(File<T> file, bool lastVersion = false)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file), FilesCommonResource.ErrorMessage_FileNotFound);
        }

        //NOTE: Always build path to handler!
        var uriBuilder = new UriBuilder(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath));
        var query = uriBuilder.Query;
        query += FilesLinkUtility.Action + "=stream&";
        query += FilesLinkUtility.FileId + "=" + HttpUtility.UrlEncode(file.Id.ToString()) + "&";
        var version = 0;
        if (!lastVersion)
        {
            version = file.Version;
            query += FilesLinkUtility.Version + "=" + file.Version + "&";
        }

        query += FilesLinkUtility.AuthKey + "=" + emailValidationKeyProvider.GetEmailKey(file.Id.ToString() + version);

        query = AddKey(query);

        return uriBuilder.Uri + "?" + query;
    }

    public string GetFileChangesUrl<T>(File<T> file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file), FilesCommonResource.ErrorMessage_FileNotFound);
        }

        var uriBuilder = new UriBuilder(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath));
        var query = uriBuilder.Query;
        query += $"{FilesLinkUtility.Action}=diff&";
        query += $"{FilesLinkUtility.FileId}={HttpUtility.UrlEncode(file.Id.ToString())}&";
        query += $"{FilesLinkUtility.Version}={file.Version}&";
        query += $"{FilesLinkUtility.AuthKey}={emailValidationKeyProvider.GetEmailKey(file.Id + file.Version.ToString(CultureInfo.InvariantCulture))}";

        query = AddKey(query);

        return $"{uriBuilder.Uri}?{query}";
    }

    public async Task<string> GetTempUrlAsync(Stream stream, string ext)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var store = await globalStore.GetStoreAsync();
        var fileName = string.Format("{0}{1}", Guid.NewGuid(), ext);
        var path = CrossPlatform.PathCombine("temp_stream", fileName);

        if (await store.IsFileAsync(FileConstant.StorageDomainTmp, path))
        {
            await store.DeleteAsync(FileConstant.StorageDomainTmp, path);
        }

        await store.SaveAsync(
            FileConstant.StorageDomainTmp,
            path,
            stream,
            MimeMapping.GetMimeMapping(ext),
            "attachment; filename=\"" + fileName + "\"");

        var uriBuilder = new UriBuilder(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath));
        var query = uriBuilder.Query;
        query += $"{FilesLinkUtility.Action}=tmp&";
        query += $"{FilesLinkUtility.FileTitle}={HttpUtility.UrlEncode(fileName)}&";
        query += $"{FilesLinkUtility.AuthKey}={emailValidationKeyProvider.GetEmailKey(fileName)}";

        return $"{uriBuilder.Uri}?{query}";
    }

    public string GetEmptyFileUrl(string extension)
    {
        var uriBuilder = new UriBuilder(commonLinkUtility.GetFullAbsolutePath(filesLinkUtility.FileHandlerPath));
        var query = uriBuilder.Query;
        query += $"{FilesLinkUtility.Action}=empty&";
        query += $"{FilesLinkUtility.FileTitle}={HttpUtility.UrlEncode(extension)}";

        return $"{uriBuilder.Uri}?{query}";
    }

    private string AddKey(string query)
    {
        var key = externalShare.GetKey();
        if (!string.IsNullOrEmpty(key))
        {
            query += $"&{FilesLinkUtility.ShareKey}={HttpUtility.UrlEncode(key)}";
        }

        return query;
    }
}