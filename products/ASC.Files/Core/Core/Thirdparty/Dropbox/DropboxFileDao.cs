﻿// (c) Copyright Ascensio System SIA 2010-2023
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

namespace ASC.Files.Core.Core.Thirdparty.Dropbox;

[Scope]
internal class DropboxFileDao : ThirdPartyFileDao<FileMetadata, FolderMetadata, Metadata>
{
    private readonly TempPath _tempPath;
    private readonly SetupInfo _setupInfo;

    public DropboxFileDao(UserManager userManager,
        IDbContextFactory<FilesDbContext> dbContextFactory,
        IDaoSelector<FileMetadata, FolderMetadata, Metadata> daoSelector,
        CrossDao crossDao,
        IFileDao<int> fileDao,
        IDaoBase<FileMetadata, FolderMetadata, Metadata> dao,
        TempPath tempPath,
        SetupInfo setupInfo,
        TenantManager tenantManager) : base(userManager, dbContextFactory, daoSelector, crossDao, fileDao, dao, tenantManager)
    {
        _tempPath = tempPath;
        _setupInfo = setupInfo;
    }

    public override async Task<ChunkedUploadSession<string>> CreateUploadSessionAsync(File<string> file, long contentLength)
    {
        if (_setupInfo.ChunkUploadSize > contentLength && contentLength != -1)
        {
            return new ChunkedUploadSession<string>(RestoreIds(file), contentLength) { UseChunks = false };
        }

        var uploadSession = new ChunkedUploadSession<string>(file, contentLength);

        var storage = (DropboxStorage)await ProviderInfo.StorageAsync;
        var dropboxSession = await storage.CreateRenewableSessionAsync();
        if (dropboxSession != null)
        {
            uploadSession.Items["DropboxSession"] = dropboxSession;
        }
        else
        {
            uploadSession.Items["TempPath"] = _tempPath.GetTempFileName();
        }

        uploadSession.File = RestoreIds(uploadSession.File);

        return uploadSession;
    }

    public override async Task<File<string>> UploadChunkAsync(ChunkedUploadSession<string> uploadSession, Stream stream, long chunkLength)
    {
        if (!uploadSession.UseChunks)
        {
            if (uploadSession.BytesTotal == 0)
            {
                uploadSession.BytesTotal = chunkLength;
            }

            uploadSession.File = await SaveFileAsync(uploadSession.File, stream);
            uploadSession.BytesUploaded = chunkLength;

            return uploadSession.File;
        }

        if (uploadSession.Items.ContainsKey("DropboxSession"))
        {
            var dropboxSession = uploadSession.GetItemOrDefault<string>("DropboxSession");
            var storage = (DropboxStorage)await ProviderInfo.StorageAsync;
            await storage.TransferAsync(dropboxSession, uploadSession.BytesUploaded, stream);
        }
        else
        {
            var tempPath = uploadSession.GetItemOrDefault<string>("TempPath");
            await using var fs = new FileStream(tempPath, FileMode.Append);
            await stream.CopyToAsync(fs);
        }

        uploadSession.BytesUploaded += chunkLength;

        if (uploadSession.BytesUploaded == uploadSession.BytesTotal || uploadSession.LastChunk)
        {
            uploadSession.BytesTotal = uploadSession.BytesUploaded;
            uploadSession.File = await FinalizeUploadSessionAsync(uploadSession);
        }
        else
        {
            uploadSession.File = RestoreIds(uploadSession.File);
        }

        return uploadSession.File;
    }

    public override async Task<File<string>> FinalizeUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        var storage = (DropboxStorage)await ProviderInfo.StorageAsync;
        if (uploadSession.Items.ContainsKey("DropboxSession"))
        {
            var dropboxSession = uploadSession.GetItemOrDefault<string>("DropboxSession");

            Metadata dropboxFile;
            var file = uploadSession.File;
            if (file.Id != null)
            {
                var dropboxFilePath = Dao.MakeThirdId(file.Id);
                dropboxFile = await storage.FinishRenewableSessionAsync(dropboxSession, dropboxFilePath, uploadSession.BytesUploaded);
            }
            else
            {
                var folderPath = Dao.MakeThirdId(file.ParentId);
                var title = await Dao.GetAvailableTitleAsync(file.Title, folderPath, IsExistAsync);
                dropboxFile = await storage.FinishRenewableSessionAsync(dropboxSession, folderPath, title, uploadSession.BytesUploaded);
            }

            await ProviderInfo.CacheResetAsync(Dao.MakeThirdId(dropboxFile));
            await ProviderInfo.CacheResetAsync(Dao.GetParentFolderId(dropboxFile), false);

            return Dao.ToFile(dropboxFile.AsFile);
        }

        await using var fs = new FileStream(uploadSession.GetItemOrDefault<string>("TempPath"),
                                       FileMode.Open, FileAccess.Read, System.IO.FileShare.None, 4096, FileOptions.DeleteOnClose);

        return await SaveFileAsync(uploadSession.File, fs);
    }

    public override Task AbortUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        if (uploadSession.Items.ContainsKey("TempPath"))
        {
            System.IO.File.Delete(uploadSession.GetItemOrDefault<string>("TempPath"));
        }

        return Task.CompletedTask;
    }
}