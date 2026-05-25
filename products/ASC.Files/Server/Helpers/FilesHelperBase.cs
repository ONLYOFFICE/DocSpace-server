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

public abstract class FilesHelperBase(
    FilesSettingsHelper filesSettingsHelper,
    FileUploader fileUploader,
    SocketManager socketManager,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    FileChecker fileChecker,
    FilesMessageService filesMessageService,
    WebhookManager webhookManager,
    IDaoFactory daoFactory,
    IEventBus eventBus,
    TenantManager tenantManager,
    AuthContext authContext)
{
    protected readonly FilesSettingsHelper _filesSettingsHelper = filesSettingsHelper;
    protected readonly FileUploader _fileUploader = fileUploader;
    protected readonly FileDtoHelper _fileDtoHelper = fileDtoHelper;
    protected readonly FileStorageService _fileStorageService = fileStorageService;
    protected readonly IDaoFactory _daoFactory = daoFactory;

    protected readonly FileChecker _fileChecker = fileChecker;

    public async Task<FileDto<T>> InsertFileAsync<T>(T folderId, Stream file, string title, bool createNewIfExist, bool keepConvertStatus = false)
    {
        try
        {
            var resultFile = await _fileUploader.ExecAsync(folderId, title, file.Length, file, createNewIfExist, !keepConvertStatus);

            await socketManager.CreateFileAsync(resultFile);

            await filesMessageService.SendAsync(resultFile.Version > 1
                ? MessageAction.FileUploadedWithOverwriting
                : MessageAction.FileUploaded, resultFile, resultFile.Title);

            await webhookManager.PublishAsync(WebhookTrigger.FileUploaded, resultFile);

            var folderDao = _daoFactory.GetCacheFolderDao<T>();
            var room = await folderDao.GetParentFoldersAsync(folderId).FirstOrDefaultAsync(f => f.IsRoom);
            if (room != null)
            {
                var data = room.Id is int rId && resultFile.Id is int fId
                    ? new RoomNotifyIntegrationData<int> { RoomId = rId, FileId = fId }
                : null;

                var thirdPartyData = room.Id is string srId && resultFile.Id is string sfId
                    ? new RoomNotifyIntegrationData<string> { RoomId = srId, FileId = sfId }
                : null;

                var evt = new RoomNotifyIntegrationEvent(authContext.CurrentAccount.ID, tenantManager.GetCurrentTenant().Id)
                {
                    Data = data,
                    ThirdPartyData = thirdPartyData
                };

                await eventBus.PublishAsync(evt);
            }

            return await _fileDtoHelper.GetAsync(resultFile);
        }
        catch (FileNotFoundException e)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FileNotFound, e);
        }
        catch (DirectoryNotFoundException e)
        {
            throw new ItemNotFoundException(FilesCommonResource.ErrorMessage_FolderNotFound, e);
        }
    }

    public async Task<FileDto<T>> GetFileInfoAsync<T>(T fileId, int version = -1)
    {
        var file = await _fileStorageService.GetFileAsync(fileId, version);
        file = file.NotFoundIfNull("File not found");

        return await _fileDtoHelper.GetAsync(file);
    }
}
