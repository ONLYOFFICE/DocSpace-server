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

namespace ASC.Data.Backup.Storage;

[Scope]
public class DocumentsBackupStorage(SetupInfo setupInfo,
        TenantManager tenantManager,
        SecurityContext securityContext,
        IDaoFactory daoFactory,
        StorageFactory storageFactory,
        IServiceProvider serviceProvider,
        AscDistributedCache cache)
    : IBackupStorage, IGetterWriteOperator
{
    private int _tenantId;
    private FilesChunkedUploadSessionHolder _sessionHolder;

    public async Task InitAsync(int tenantId)
    {
        _tenantId = tenantId;
        var store = await storageFactory.GetStorageAsync(_tenantId, "files");
        _sessionHolder = new FilesChunkedUploadSessionHolder(daoFactory, store, "", cache, setupInfo.ChunkUploadSize);
    }

    public async Task<string> UploadAsync(string storageBasePath, string localPath, Guid userId)
    {
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        if (!userId.Equals(Guid.Empty))
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(userId);
        }
        else
        {
            var tenant = await tenantManager.GetTenantAsync(_tenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
        }

        if (int.TryParse(storageBasePath, out var fId))
        {
            return (await Upload(fId, localPath)).ToString();
        }

        return await Upload(storageBasePath, localPath);
    }

    public async Task<string> DownloadAsync(string storagePath, string targetLocalPath)
    {
        await tenantManager.SetCurrentTenantAsync(_tenantId);

        if (int.TryParse(storagePath, out var fId))
        {
            return await DownloadDaoAsync(fId, targetLocalPath);
        }

        return await DownloadDaoAsync(storagePath, targetLocalPath);
    }

    public async Task DeleteAsync(string storagePath)
    {
        await tenantManager.SetCurrentTenantAsync(_tenantId);

        if (int.TryParse(storagePath, out var fId))
        {
            await DeleteDaoAsync(fId);

            return;
        }

        await DeleteDaoAsync(storagePath);
    }

    public async Task<bool> IsExistsAsync(string storagePath)
    {
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        if (int.TryParse(storagePath, out var fId))
        {
            return await IsExistsDaoAsync(fId);
        }

        return await IsExistsDaoAsync(storagePath);
    }

    public Task<string> GetPublicLinkAsync(string storagePath)
    {
        return Task.FromResult(String.Empty);
    }

    private async Task<T> Upload<T>(T folderId, string localPath)
    {
        var folderDao = GetFolderDao<T>();
        var fileDao = await GetFileDaoAsync<T>();

        var folder = await folderDao.GetFolderAsync(folderId);
        if (folder == null)
        {
            throw new FileNotFoundException("Folder not found.");
        }

        await using var source = File.OpenRead(localPath);
        var newFile = serviceProvider.GetService<File<T>>();
        newFile.Title = Path.GetFileName(localPath);
        newFile.ParentId = folder.Id;
        newFile.ContentLength = source.Length;

        File<T> file = null;
        var buffer = new byte[setupInfo.ChunkUploadSize];
        var chunkedUploadSession = await fileDao.CreateUploadSessionAsync(newFile, source.Length);
        chunkedUploadSession.CheckQuota = false;

        int bytesRead;

        while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, (int)setupInfo.ChunkUploadSize))) > 0)
        {
            using var theMemStream = new MemoryStream();
            await theMemStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            theMemStream.Position = 0;
            file = await fileDao.UploadChunkAsync(chunkedUploadSession, theMemStream, bytesRead);
        }

        return file.Id;
    }

    private async Task<string> DownloadDaoAsync<T>(T fileId, string targetLocalPath)
    {
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        var fileDao = await GetFileDaoAsync<T>();
        var file = await fileDao.GetFileAsync(fileId);
        if (file == null)
        {
            throw new FileNotFoundException("File not found.");
        }

        await using var source = await fileDao.GetFileStreamAsync(file);
        var destPath = Path.Combine(targetLocalPath, file.Title);
        await using var destination = File.OpenWrite(destPath);
        await source.CopyToAsync(destination);
        return destPath;
    }

    private async Task DeleteDaoAsync<T>(T fileId)
    {
        var fileDao = await GetFileDaoAsync<T>();
        await fileDao.DeleteFileAsync(fileId);
    }

    private async Task<bool> IsExistsDaoAsync<T>(T fileId)
    {
        var fileDao = await GetFileDaoAsync<T>();
        try
        {
            var file = await fileDao.GetFileAsync(fileId);

            return file != null && file.RootFolderType != FolderType.TRASH;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<IDataWriteOperator> GetWriteOperatorAsync(string storageBasePath, string title, Guid userId)
    {
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        if (!userId.Equals(Guid.Empty))
        {
            await securityContext.AuthenticateMeWithoutCookieAsync(userId);
        }
        else
        {
            var tenant = await tenantManager.GetTenantAsync(_tenantId);
            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);
        }
        if (int.TryParse(storageBasePath, out var fId))
        {
            var uploadSession = await InitUploadChunkAsync(fId, title);
            var folderDao = GetFolderDao<int>();
            return await folderDao.CreateDataWriteOperatorAsync(fId, uploadSession, _sessionHolder);
        }
        else
        {
            var uploadSession = await InitUploadChunkAsync(storageBasePath, title);
            var folderDao = GetFolderDao<string>();
            return await folderDao.CreateDataWriteOperatorAsync(storageBasePath, uploadSession, _sessionHolder);
        }
    }

    public async Task<string> GetBackupExtensionAsync(string storageBasePath)
    {
        await tenantManager.SetCurrentTenantAsync(_tenantId);
        if (int.TryParse(storageBasePath, out var fId))
        {
            var folderDao = GetFolderDao<int>();
            return await folderDao.GetBackupExtensionAsync(fId);
        }
        else
        {
            var folderDao = GetFolderDao<string>();
            return await folderDao.GetBackupExtensionAsync(storageBasePath);
        }
    }

    private async Task<CommonChunkedUploadSession> InitUploadChunkAsync<T>(T folderId, string title)
    {
        var folderDao = GetFolderDao<T>();
        var fileDao = await GetFileDaoAsync<T>();

        var folder = await folderDao.GetFolderAsync(folderId);
        var newFile = serviceProvider.GetService<File<T>>();

        newFile.Title = title;
        newFile.ParentId = folder.Id;

        var chunkedUploadSession = await fileDao.CreateUploadSessionAsync(newFile, -1);
        chunkedUploadSession.CheckQuota = false;
        return chunkedUploadSession;
    }

    private IFolderDao<T> GetFolderDao<T>()
    {
        return daoFactory.GetFolderDao<T>();
    }

    private async Task<IFileDao<T>> GetFileDaoAsync<T>()
    {
        // hack: create storage using webConfigPath and put it into DataStoreCache
        // FileDao will use this storage and will not try to create the new one from service config
        await storageFactory.GetStorageAsync(_tenantId, "files");
        return daoFactory.GetFileDao<T>();
    }
}
