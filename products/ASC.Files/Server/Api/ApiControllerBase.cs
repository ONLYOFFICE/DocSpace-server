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

using System.Text.Json.Nodes;

using ASC.Webhooks.Core;

namespace ASC.Files.Api;

/// <summary>
/// Provides access to documents.
/// </summary>
/// <name>files</name>
[Scope]
[DefaultRoute]
[ApiController]
[ControllerName("files")]
public abstract class ApiControllerBase(FolderDtoHelper folderDtoHelper, FileDtoHelper fileDtoHelper)
    : ControllerBase
{
    protected readonly FolderDtoHelper _folderDtoHelper = folderDtoHelper;
    protected readonly FileDtoHelper _fileDtoHelper = fileDtoHelper;

    protected async Task<FileEntryDto> GetFileEntryWrapperAsync(FileEntry r)
    {
        FileEntryDto wrapper = null;
        if (r.FileEntryType == FileEntryType.Folder)
        {
            if (r is Folder<int> fol1)
            {
                wrapper = await _folderDtoHelper.GetAsync(fol1);
            }
            else if (r is Folder<string> fol2)
            {
                wrapper = await _folderDtoHelper.GetAsync(fol2);
            }
        }
        else
        {
            if (r is File<int> file1)
            {
                wrapper = await _fileDtoHelper.GetAsync(file1);
            }
            else if (r is File<string> file2)
            {
                wrapper = await _fileDtoHelper.GetAsync(file2);
            }
        }

        return wrapper;
    }
}


[Scope]
public class WebhookFileEntryAccessChecker(
    SecurityContext securityContext,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory) : IWebhookAccessChecker
{
    public async Task<bool> CheckAccessAsync(WebhookData webhookData)
    {
        if (securityContext.CurrentAccount.ID == webhookData.TargetUserId)
        {
            return true;
        }

        if (typeof(FileEntryDto).IsAssignableFrom(webhookData.ResponseType))
        {
            var entryNode = JsonNode.Parse(webhookData.ResponseString)["response"];
            return await CheckAccessByResponse(entryNode, webhookData.TargetUserId);
        }

        if (typeof(IEnumerable<FileEntryDto>).IsAssignableFrom(webhookData.ResponseType) ||
            typeof(IAsyncEnumerable<FileEntryDto>).IsAssignableFrom(webhookData.ResponseType))
        {
            var entryNodes = JsonNode.Parse(webhookData.ResponseString)["response"].AsArray();
            foreach (var entryNode in entryNodes)
            {
                if (!await CheckAccessByResponse(entryNode, webhookData.TargetUserId))
                {
                    return false;
                }
            }

            return true;
        }

        var accessByFolderId = await CheckAccessByRouteAsync(webhookData.RouteData, "folderId", FileEntryType.Folder, webhookData.TargetUserId);
        if (accessByFolderId.HasValue)
        {
            return accessByFolderId.Value;
        }

        var accessByFileId = await CheckAccessByRouteAsync(webhookData.RouteData, "fileId", FileEntryType.File, webhookData.TargetUserId);
        if (accessByFileId.HasValue)
        {
            return accessByFileId.Value;
        }

        return false;
    }

    private async Task<bool?> CheckAccessByRouteAsync(Dictionary<string, string> routeData, string param, FileEntryType fileEntryType, Guid userId)
    {
        if (routeData.TryGetValue(param, out var fileEntryIdStr) && !string.IsNullOrEmpty(fileEntryIdStr))
        {
            return int.TryParse(fileEntryIdStr, out var fileEntryIdInt)
                ? await CheckAccessAsync(fileEntryIdInt, fileEntryType, userId)
                : await CheckAccessAsync(fileEntryIdStr, fileEntryType, userId);
        }

        return null;
    }

    private async Task<bool> CheckAccessByResponse(JsonNode entryNode, Guid userId)
    {
        if (entryNode == null)
        {
            return false;
        }

        var entryType = (FileEntryType)entryNode["fileEntryType"].GetValue<int>();
        var entryIdNode = entryNode["id"];

        return entryIdNode.GetValueKind() == JsonValueKind.Number
            ? await CheckAccessAsync(entryIdNode.GetValue<int>(), entryType, userId)
            : await CheckAccessAsync(entryIdNode.GetValue<string>(), entryType, userId);
    }

    async Task<bool> CheckAccessAsync<T>(T fileEntryId, FileEntryType fileEntryType, Guid userId)
    {
        FileEntry<T> fileEntry = fileEntryType == FileEntryType.File
            ? await daoFactory.GetFileDao<T>().GetFileAsync(fileEntryId)
            : await daoFactory.GetCacheFolderDao<T>().GetFolderAsync(fileEntryId);

        return await fileSecurity.CanReadAsync(fileEntry, userId);
    }
}