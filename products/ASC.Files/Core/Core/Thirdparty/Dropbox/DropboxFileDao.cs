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

using File = System.IO.File;

namespace ASC.Files.Core.Core.Thirdparty.Dropbox;

[Scope(typeof(ThirdPartyFileDao<FileMetadata, FolderMetadata, Metadata>))]
internal class DropboxFileDao(
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextFactory,
    IDaoSelector<FileMetadata, FolderMetadata, Metadata> daoSelector,
    CrossDao crossDao,
    IFileDao<int> fileDao,
    IDaoBase<FileMetadata, FolderMetadata, Metadata> dao,
    TempPath tempPath,
    SetupInfo setupInfo,
    TenantManager tenantManager,
    Global global)
    : ThirdPartyFileDao<FileMetadata, FolderMetadata, Metadata>(userManager, dbContextFactory, daoSelector, crossDao, fileDao, dao, tenantManager, global)
{
    protected override string UploadSessionKey => "DropboxSession";

    public override async Task<ChunkedUploadSession<string>> CreateUploadSessionAsync(File<string> file, long contentLength)
    {
        if (setupInfo.ChunkUploadSize > contentLength && contentLength != -1)
        {
            return new ChunkedUploadSession<string>(RestoreIds(file), contentLength) { UseChunks = false };
        }

        var uploadSession = new ChunkedUploadSession<string>(file, contentLength);

        var storage = (DropboxStorage)await ProviderInfo.StorageAsync;
        var dropboxSession = await storage.CreateRenewableSessionAsync();
        if (dropboxSession != null)
        {
            uploadSession.Items[UploadSessionKey] = dropboxSession;
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
        var storage = (DropboxStorage)await ProviderInfo.StorageAsync;
        if (uploadSession.Items.ContainsKey(UploadSessionKey))
        {
            var dropboxSession = uploadSession.GetItemOrDefault<RenewableUploadShortSession>(UploadSessionKey);

            Metadata dropboxFile;
            var file = uploadSession.File;

            if (file.Id != null)
            {
                var dropboxFilePath = Dao.MakeThirdId(file.Id);
                dropboxFile = await storage.FinishRenewableSessionAsync(dropboxSession, dropboxFilePath);
            }
            else
            {
                var folderPath = Dao.MakeThirdId(file.ParentId);
                var title = await GetAvailableTitleAsync(file.Title, folderPath);
                dropboxFile = await storage.FinishRenewableSessionAsync(dropboxSession, folderPath, title);
            }

            await ProviderInfo.CacheResetAsync(Dao.MakeThirdId(dropboxFile));
            await ProviderInfo.CacheResetAsync(Dao.GetParentFolderId(dropboxFile));

            return Dao.ToFile(dropboxFile.AsFile);
        }

        await using var fs = new FileStream(uploadSession.TempPath, FileMode.Open, FileAccess.Read, System.IO.FileShare.None, 4096, FileOptions.DeleteOnClose);

        return await SaveFileAsync(uploadSession.File, fs);
    }

    public override Task AbortUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        var path = uploadSession.TempPath;

        if (!string.IsNullOrEmpty(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    protected override async Task NativeUploadChunkAsync(ChunkedUploadSession<string> uploadSession, Stream stream, long chunkLength)
    {
        var dropboxSession = uploadSession.GetItemOrDefault<RenewableUploadShortSession>(UploadSessionKey);
        var storage = (DropboxStorage)await ProviderInfo.StorageAsync;
        await storage.TransferAsync(dropboxSession, stream, chunkLength);
    }
}
