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
    FileSharing fileSharing,
    UserManager userManager)
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
    private readonly AuthContext _authContext = authContext;

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
            .Where(async (s, _) => await userManager.CanUserShareAnotherUserAsync(_authContext.CurrentAccount.ID, s.ShareTo))
            .Select(async (FileShareParams s, CancellationToken _) => await fileShareParamsHelper.ToAceObjectAsync(s))
            .ToListAsync();

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
