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

namespace ASC.Files.Helpers;

[Scope]
public class SecurityControllerHelper(
    FilesSettingsHelper filesSettingsHelper,
    FileUploader fileUploader,
    SocketManager socketManager,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    FileShareDtoHelper fileShareDtoHelper,
    FileShareParamsHelper fileShareParamsHelper,
    FileChecker fileChecker,
    WebhookManager webhookManager,
    IDaoFactory daoFactory,
    IEventBus eventBus,
    TenantManager tenantManager,
    AuthContext authContext,
    FileSharing fileSharing)
    : FilesHelperBase(
        filesSettingsHelper,
        fileUploader,
        socketManager,
        fileDtoHelper,
        fileStorageService,
        fileChecker,
        webhookManager,
        daoFactory,
        eventBus,
        tenantManager,
        authContext)
{

    public async IAsyncEnumerable<FileShareDto> GetSecurityInfoAsync<T>(IEnumerable<T> fileIds, IEnumerable<T> folderIds)
    {
        var fileShares = await fileSharing.GetSharedInfoAsync(fileIds, folderIds);

        foreach (var fileShareDto in fileShares)
        {
            yield return await fileShareDtoHelper.Get(fileShareDto);
        }
    }

    public async IAsyncEnumerable<FileShareDto> SetSecurityInfoAsync<T>(List<T> fileIds, List<T> folderIds, List<FileShareParams> share, bool notify, string sharingMessage)
    {
        if (share == null || share.Count == 0)
        {
            yield break;
        }

        var fileShares = await share
            .ToAsyncEnumerable()
            .Select(async (FileShareParams s, CancellationToken _) => await fileShareParamsHelper.ToAceObjectAsync(s)).ToListAsync();

        var aceCollection = new AceCollection<T>
        {
            Files = fileIds,
            Folders = folderIds,
            Aces = fileShares,
            Message = sharingMessage
        };

        var subjects = share.Select(s => s.ShareTo).Distinct().ToList();
        var result = await _fileStorageService.SetAceObjectAsync(aceCollection, notify);

        foreach (var r in result.SelectMany(a => a.ProcessedItems))
        {
            await foreach (var s in fileSharing.GetPureSharesAsync(r.Entry, subjects))
            {
                yield return await fileShareDtoHelper.Get(s);
            }
        }
    }
}