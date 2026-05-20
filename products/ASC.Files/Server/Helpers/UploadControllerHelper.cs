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
public class UploadControllerHelper(
    FilesSettingsHelper filesSettingsHelper,
    FileUploader fileUploader,
    SocketManager socketManager,
    FileDtoHelper fileDtoHelper,
    FileStorageService fileStorageService,
    ChunkedUploadSessionHelper chunkedUploadSessionHelper,
    TenantManager tenantManager,
    IDaoFactory daoFactory,
    FileChecker fileChecker,
    WebhookManager webhookManager,
    IEventBus eventBus,
    AuthContext authContext)
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
    public async Task<ChunkedUploadSessionResponse<T>> CreateEditSessionAsync<T>(T fileId, long fileSize)
    {
        var file = await _fileUploader.VerifyChunkedUploadForEditing(fileId, fileSize);

        return await CreateUploadSessionAsync(file, false, null, true);
    }


    public async Task<ChunkedUploadSessionResponse<T>> CreateUploadSessionAsync<T>(T folderId, string fileName, long fileSize, string relativePath, bool encrypted, ApiDateTime createOn, bool createNewIfExist, bool keepVersion = false)
    {
        var file = await _fileUploader.VerifyChunkedUploadAsync(folderId, fileName, fileSize, !createNewIfExist, relativePath);
        return await CreateUploadSessionAsync(file, encrypted, createOn, keepVersion);
    }

    private async Task<ChunkedUploadSessionResponse<T>> CreateUploadSessionAsync<T>(File<T> file, bool encrypted, ApiDateTime createOn, bool keepVersion = false)
    {
        var session = await _fileUploader.InitiateUploadAsync(file.ParentId, file.Id ?? default, file.Title, file.ContentLength, encrypted, keepVersion, createOn);

        return await chunkedUploadSessionHelper.ToResponseObjectAsync(session, true);
    }

    public async Task<List<FileDto<T>>> UploadFileAsync<T>(T folderId, UploadRequestDto uploadModel)
    {
        if (uploadModel.StoreOriginalFileFlag.HasValue)
        {
            await _filesSettingsHelper.SetStoreOriginalFiles(uploadModel.StoreOriginalFileFlag.Value);
        }

        if (uploadModel.File == null)
        {
            throw new InvalidOperationException("No input files");
        }

        var fileName = uploadModel.File.FileName;

        return
        [
            await InsertFileAsync(folderId, uploadModel.File.OpenReadStream(), fileName, uploadModel.CreateNewIfExist, uploadModel.KeepConvertStatus)
        ];
    }
}

/// <summary>
/// Represents a wrapper for the response of a chunked upload session operation.
/// </summary>
public class ChunkedUploadSessionResponseWrapper<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    /// <example>true</example>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the data of the chunked upload session response.
    /// </summary>
    /// <example>{"id": "00000000-0000-0000-0000-000000000000", "location": "https://example.com/upload"}</example>
    public ChunkedUploadSessionResponse<T> Data { get; set; }
}
