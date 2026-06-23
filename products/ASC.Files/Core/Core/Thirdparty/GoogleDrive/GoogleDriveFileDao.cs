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

using DriveFile = Google.Apis.Drive.v3.Data.File;
using File = System.IO.File;

namespace ASC.Files.Core.Core.Thirdparty.GoogleDrive;

[Scope(typeof(ThirdPartyFileDao<DriveFile, DriveFile, DriveFile>))]
internal class GoogleDriveFileDao(
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    RegexDaoSelectorBase<DriveFile, DriveFile, DriveFile> daoSelector,
    CrossDao crossDao,
    IFileDao<int> fileDao,
    IDaoBase<DriveFile, DriveFile, DriveFile> dao,
    TempPath tempPath,
    SetupInfo setupInfo,
    TenantManager tenantManager,
    Global global)
    : ThirdPartyFileDao<DriveFile, DriveFile, DriveFile>(userManager, dbContextFactory, daoSelector, crossDao, fileDao, dao, tenantManager, global)
{
    protected override string UploadSessionKey => "GoogleDriveSession";

    public override async Task<ChunkedUploadSession<string>> CreateUploadSessionAsync(File<string> file, long contentLength)
    {
        if (setupInfo.ChunkUploadSize > contentLength && contentLength != -1)
        {
            return new ChunkedUploadSession<string>(RestoreIds(file), contentLength) { UseChunks = false };
        }

        var uploadSession = new ChunkedUploadSession<string>(file, contentLength);

        DriveFile driveFile;
        var storage = (GoogleDriveStorage)await ProviderInfo.StorageAsync;

        if (file.Id != null)
        {
            driveFile = await Dao.GetFileAsync(file.Id);
        }
        else
        {
            var folder = await Dao.GetFolderAsync(file.ParentId);
            driveFile = GoogleDriveStorage.FileConstructor(file.Title, null, folder.Id);
        }

        var googleDriveSession = await storage.CreateRenewableSessionAsync(driveFile, contentLength);
        if (googleDriveSession != null)
        {
            uploadSession.Items[UploadSessionKey] = googleDriveSession;
        }
        else
        {
            uploadSession.TempPath = tempPath.GetTempFileName();
        }

        uploadSession.File = RestoreIds(uploadSession.File);

        return uploadSession;
    }

    public override async Task<File<string>> FinalizeUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        if (uploadSession.Items.ContainsKey(UploadSessionKey))
        {
            var googleDriveSession = uploadSession.GetItemOrDefault<RenewableUploadSession>(UploadSessionKey);

            await ProviderInfo.CacheResetAsync(googleDriveSession.FileId);
            var parentDriveId = googleDriveSession.FolderId;
            if (parentDriveId != null)
            {
                await ProviderInfo.CacheResetAsync(parentDriveId);
            }

            return Dao.ToFile(await Dao.GetFileAsync(googleDriveSession.FileId));
        }

        await using var fs = new FileStream(uploadSession.TempPath, FileMode.Open, FileAccess.Read, System.IO.FileShare.None, 4096, FileOptions.DeleteOnClose);

        return await SaveFileAsync(uploadSession.File, fs);
    }

    public override Task AbortUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        if (uploadSession.Items.ContainsKey(UploadSessionKey))
        {
            var googleDriveSession = uploadSession.GetItemOrDefault<RenewableUploadSession>(UploadSessionKey);

            if (googleDriveSession.Status != RenewableUploadSessionStatus.Completed)
            {
                googleDriveSession.Status = RenewableUploadSessionStatus.Aborted;
            }

            return Task.CompletedTask;
        }

        var path = uploadSession.TempPath;

        if (!string.IsNullOrEmpty(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    protected override async Task NativeUploadChunkAsync(ChunkedUploadSession<string> uploadSession, Stream stream, long chunkLength)
    {
        var googleDriveSession = uploadSession.GetItemOrDefault<RenewableUploadSession>(UploadSessionKey);
        var storage = (GoogleDriveStorage)await ProviderInfo.StorageAsync;
        var lastChunk = uploadSession.Items.ContainsKey("lastChunk") || googleDriveSession.BytesTransferred + chunkLength == googleDriveSession.BytesToTransfer;
        await storage.TransferAsync(googleDriveSession, stream, chunkLength, lastChunk);
    }
}