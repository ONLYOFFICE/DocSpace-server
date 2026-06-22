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

    public string RoomUrlString => "/rooms/shared/{0}/filter?withSubfolders={1}&folder={0}&count=100&page=1&sortby=DateAndTime&sortorder=descending";

    public string AgentUrlString => "/ai-agents/{0}/chat?folder={0}";

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
        result = folder.RootFolderType switch
        {
            FolderType.USER => string.Format($"rooms/personal/filter?folder={urlPathEncode}"),
            FolderType.Recent => string.Format($"recent/filter?folder={urlPathEncode}"),
            FolderType.Archive => string.Format($"rooms/archived/filter?folder={urlPathEncode}"),
            FolderType.SHARE => string.Format($"shared-with-me/filter?folder={urlPathEncode}"),
            FolderType.VirtualRooms or FolderType.RoomTemplates => string.Format($"rooms/shared/{urlPathEncode}/filter?folder={urlPathEncode}"),
            FolderType.AiAgents => string.Format($"ai-agents/{urlPathEncode}/filter?folder={urlPathEncode}"),
            FolderType.Forms => string.Format($"forms/{urlPathEncode}/filter?folder={urlPathEncode}"),
            _ => result
        };

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