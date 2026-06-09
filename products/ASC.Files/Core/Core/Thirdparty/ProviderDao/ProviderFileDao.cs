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

namespace ASC.Files.Thirdparty.ProviderDao;

[Scope(typeof(IFileDao<string>))]
internal class ProviderFileDao(
    ILogger<ProviderFileDao> logger,
    IServiceProvider serviceProvider,
    TenantManager tenantManager,
    CrossDao crossDao,
    SelectorFactory selectorFactory,
    ISecurityDao<string> securityDao,
    FileChecker fileChecker,
    IDbContextFactory<FilesDbContext> dbContextFactory)
    : ProviderDaoBase(serviceProvider, tenantManager, crossDao, selectorFactory, securityDao), IFileDao<string>
{
    public async Task InvalidateCacheAsync(string fileId)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);

        await fileDao.InvalidateCacheAsync(selector.ConvertId(fileId));
    }

    public async Task<File<string>> GetFileAsync(string fileId)
    {
        var selector = _selectorFactory.GetSelector(fileId);

        var fileDao = selector.GetFileDao(fileId);
        var result = await fileDao.GetFileAsync(selector.ConvertId(fileId));

        return result;
    }

    public async Task<File<string>> GetFileAsync(string fileId, int fileVersion)
    {
        var selector = _selectorFactory.GetSelector(fileId);

        var fileDao = selector.GetFileDao(fileId);
        var result = await fileDao.GetFileAsync(selector.ConvertId(fileId), fileVersion);

        return result;
    }

    public async Task<File<string>> GetFileAsync(string parentId, string title)
    {
        var selector = _selectorFactory.GetSelector(parentId);
        var fileDao = selector.GetFileDao(parentId);
        var result = await fileDao.GetFileAsync(selector.ConvertId(parentId), title);

        return result;
    }

    public async Task<File<string>> GetFileStableAsync(string fileId, int fileVersion = -1)
    {
        var selector = _selectorFactory.GetSelector(fileId);

        var fileDao = selector.GetFileDao(fileId);
        var result = await fileDao.GetFileAsync(selector.ConvertId(fileId), fileVersion);

        return result;
    }

    public IAsyncEnumerable<File<string>> GetFileHistoryAsync(string fileId)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);

        return fileDao.GetFileHistoryAsync(selector.ConvertId(fileId));
    }

    public async IAsyncEnumerable<File<string>> GetFilesAsync(IEnumerable<string> fileIds)
    {
        foreach (var (selectorLocal, matchedIds) in _selectorFactory.GetSelectors(fileIds))
        {
            if (selectorLocal == null)
            {
                continue;
            }

            foreach (var matchedId in matchedIds.GroupBy(selectorLocal.GetIdCode))
            {
                var fileDao = selectorLocal.GetFileDao(matchedId.FirstOrDefault());

                await foreach (var file in fileDao.GetFilesAsync(matchedId.Select(selectorLocal.ConvertId).ToList()))
                {
                    if (file != null)
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    public async IAsyncEnumerable<File<string>> GetFilesFilteredAsync(IEnumerable<string> fileIds, IEnumerable<string> excludeParentsIds, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText, string[] extension, bool searchInContent)
    {
        foreach (var (selectorLocal, matchedIds) in _selectorFactory.GetSelectors(fileIds))
        {
            if (selectorLocal == null)
            {
                continue;
            }

            foreach (var matchedId in matchedIds.GroupBy(selectorLocal.GetIdCode))
            {
                var fileDao = selectorLocal.GetFileDao(matchedId.FirstOrDefault());

                await foreach (var file in fileDao.GetFilesFilteredAsync(matchedId.Select(selectorLocal.ConvertId).ToArray(), excludeParentsIds, filterType, subjectGroup, subjectID, searchText,
                                   extension, searchInContent))
                {
                    if (file != null)
                    {
                        yield return file;
                    }
                }
            }
        }
    }

    public async IAsyncEnumerable<string> GetFilesAsync(string parentId)
    {
        var selector = _selectorFactory.GetSelector(parentId);
        var fileDao = selector.GetFileDao(parentId);
        var files = fileDao.GetFilesAsync(selector.ConvertId(parentId));

        await foreach (var f in files.Where(r => r != null))
        {
            yield return f;
        }
    }

    public async IAsyncEnumerable<File<string>> GetFilesAsync(string parentId, OrderBy orderBy, FilterType filterType, bool subjectGroup, Guid subjectID, string searchText,
        string[] extension, bool searchInContent, bool withSubfolders = false, bool excludeSubject = false, int offset = 0, int count = -1, string roomId = null, bool withShared = false, bool containingMyFiles = false, FolderType parentType = FolderType.DEFAULT, FormsItemDto formsItemDto = null, bool applyFormStepFilter = false, bool applyFfrStartedFormsFilter = false)
    {
        var selector = _selectorFactory.GetSelector(parentId);

        var fileDao = selector.GetFileDao(parentId);
        var files = fileDao.GetFilesAsync(selector.ConvertId(parentId), orderBy, filterType, subjectGroup, subjectID, searchText, extension, searchInContent, withSubfolders, excludeSubject, formsItemDto: formsItemDto);
        var result = await files.Where(r => r != null).ToListAsync();

        foreach (var r in result)
        {
            yield return r;
        }
    }

    public override Task<Stream> GetFileStreamAsync(File<string> file)
    {
        return GetFileStreamAsync(file, 0);
    }

    /// <summary>
    /// Get stream of file
    /// </summary>
    /// <param name="file"></param>
    /// <param name="offset"></param>
    /// <returns>Stream</returns>
    public async Task<Stream> GetFileStreamAsync(File<string> file, long offset)
    {
        return await GetFileStreamAsync(file, offset, long.MaxValue);
    }

    public async Task<Stream> GetFileStreamAsync(File<string> file, long offset, long length)
    {
        ArgumentNullException.ThrowIfNull(file);

        var fileId = file.Id;
        var selector = _selectorFactory.GetSelector(fileId);
        file.Id = selector.ConvertId(fileId);

        var fileDao = selector.GetFileDao(fileId);
        var stream = await fileDao.GetFileStreamAsync(file, offset, length);
        file.Id = fileId; //Restore id

        return stream;
    }


    public async Task<long> GetFileSizeAsync(File<string> file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var fileId = file.Id;
        var selector = _selectorFactory.GetSelector(fileId);
        file.Id = selector.ConvertId(fileId);

        var fileDao = selector.GetFileDao(fileId);
        var size = await fileDao.GetFileSizeAsync(file);
        file.Id = fileId; //Restore id

        return size;
    }



    public async Task<bool> IsSupportedPreSignedUriAsync(File<string> file)
    {
        ArgumentNullException.ThrowIfNull(file);

        var fileId = file.Id;
        var selector = _selectorFactory.GetSelector(fileId);
        file.Id = selector.ConvertId(fileId);

        var fileDao = selector.GetFileDao(fileId);
        var isSupported = await fileDao.IsSupportedPreSignedUriAsync(file);
        file.Id = fileId; //Restore id

        return isSupported;
    }

    public async Task<string> GetPreSignedUriAsync(File<string> file, TimeSpan expires, string shareKey = null)
    {
        ArgumentNullException.ThrowIfNull(file);

        var fileId = file.Id;
        var selector = _selectorFactory.GetSelector(fileId);
        file.Id = selector.ConvertId(fileId);

        var fileDao = selector.GetFileDao(fileId);
        var streamUri = await fileDao.GetPreSignedUriAsync(file, expires, shareKey);
        file.Id = fileId; //Restore id

        return streamUri;
    }
    public async Task<File<string>> SaveFileAsync(File<string> file, Stream fileStream, bool checkFolder)
    {
        return await SaveFileAsync(file, fileStream);
    }

    public async Task<File<string>> SaveFileAsync(File<string> file, Stream fileStream, Guid chatId = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var fileId = file.Id;
        var folderId = file.ParentId;

        IDaoSelector selector;
        File<string> fileSaved = null;
        //Convert

        var firstBytes = new byte[300];
        var read = await fileStream.ReadAtLeastAsync(firstBytes, firstBytes.Length, false);
        fileStream.Seek(0, SeekOrigin.Begin);

        if (fileId != null)
        {
            selector = _selectorFactory.GetSelector(fileId);
            file.Id = selector.ConvertId(fileId);
            if (folderId != null)
            {
                file.ParentId = selector.ConvertId(folderId);
            }

            var fileDao = selector.GetFileDao(fileId);
            fileSaved = await fileDao.SaveFileAsync(file, fileStream);
        }
        else if (folderId != null)
        {
            selector = _selectorFactory.GetSelector(folderId);
            file.ParentId = selector.ConvertId(folderId);
            var fileDao = selector.GetFileDao(folderId);
            fileSaved = await fileDao.SaveFileAsync(file, fileStream);
        }

        var fileType = FileUtility.GetFileTypeByFileName(file.Title);

        if (fileType == FileType.Pdf && file.Category == (int)FilterType.None)
        {
            if (read >= firstBytes.Length)
            {
                using var ms = new MemoryStream(firstBytes);
                if (await fileChecker.CheckExtendedPDFstream(ms))
                {
                    fileSaved.Category = (int)FilterType.PdfForm;
                }
            }
        }

        if (fileSaved != null)
        {
            file.Id = fileSaved.Id;
            file.ParentId = fileSaved.ParentId;
            return fileSaved;
        }
        else
        {
            file.Id = fileId;
            file.ParentId = folderId;
        }

        throw new ArgumentException("No file id or folder id toFolderId determine provider");
    }

    public async Task<File<string>> ReplaceFileVersionAsync(File<string> file, Stream fileStream)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Id == null)
        {
            throw new ArgumentException("No file id or folder id toFolderId determine provider");
        }

        var fileId = file.Id;
        var folderId = file.ParentId;

        //Convert
        var selector = _selectorFactory.GetSelector(fileId);

        file.Id = selector.ConvertId(fileId);
        if (folderId != null)
        {
            file.ParentId = selector.ConvertId(folderId);
        }

        var fileDao = selector.GetFileDao(fileId);

        return await fileDao.ReplaceFileVersionAsync(file, fileStream);
    }

    public async Task DeleteFileAsync(string fileId)
    {
        await DeleteFileAsync(fileId, Guid.Empty);
    }

    public async Task DeleteFileVersionAsync(File<string> file, int version)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Id == null)
        {
            throw new ArgumentException("No file id or folder id toFolderId determine provider");
        }

        var fileId = file.Id;
        var folderId = file.ParentId;

        //Convert
        var selector = _selectorFactory.GetSelector(fileId);

        file.Id = selector.ConvertId(fileId);
        if (folderId != null)
        {
            file.ParentId = selector.ConvertId(folderId);
        }

        var fileDao = selector.GetFileDao(fileId);

        await fileDao.DeleteFileVersionAsync(file, version);
    }

    public async Task DeleteFileAsync(string fileId, Guid ownerId)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);

        await fileDao.DeleteFileAsync(selector.ConvertId(fileId), ownerId);
    }

    public async Task<bool> IsExistAsync(string title, string folderId)
    {
        var selector = _selectorFactory.GetSelector(folderId);
        var fileDao = selector.GetFileDao(folderId);
        return await fileDao.IsExistAsync(title, selector.ConvertId(folderId));
    }

    public async Task<bool> IsExistAsync(string title, int category, string folderId)
    {
        return await IsExistAsync(title, folderId);
    }

    public async Task<string> GetAvailableTitleAsync(string requestTitle, string parentFolderId)
    {
        var selector = _selectorFactory.GetSelector(parentFolderId);
        var fileDao = selector.GetFileDao(parentFolderId);
        return await fileDao.GetAvailableTitleAsync(requestTitle, selector.ConvertId(parentFolderId));
    }

    public async Task<TTo> MoveFileAsync<TTo>(string fileId, TTo toFolderId, bool deleteLinks = false)
    {
        if (toFolderId is int tId)
        {
            return IdConverter.Convert<TTo>(await MoveFileAsync(fileId, tId, deleteLinks));
        }

        if (toFolderId is string tsId)
        {
            return IdConverter.Convert<TTo>(await MoveFileAsync(fileId, tsId, deleteLinks));
        }

        throw new NotImplementedException();
    }

    public async Task<int> MoveFileAsync(string fileId, int toFolderId, bool deleteLinks = false)
    {
        var movedFile = await PerformCrossDaoFileCopyAsync(fileId, toFolderId, true);

        return movedFile.Id;
    }

    public async Task<string> MoveFileAsync(string fileId, string toFolderId, bool deleteLinks = false)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        if (IsCrossDao(fileId, toFolderId))
        {
            var movedFile = await PerformCrossDaoFileCopyAsync(fileId, toFolderId, true);

            return movedFile.Id;
        }

        var fileDao = selector.GetFileDao(fileId);

        return await fileDao.MoveFileAsync(selector.ConvertId(fileId), selector.ConvertId(toFolderId), deleteLinks);
    }

    public async Task<File<TTo>> CopyFileAsync<TTo>(string fileId, TTo toFolderId)
    {
        if (toFolderId is int tId)
        {
            return await CopyFileAsync(fileId, tId, Guid.Empty) as File<TTo>;
        }

        if (toFolderId is string tsId)
        {
            return await CopyFileAsync(fileId, tsId) as File<TTo>;
        }

        throw new NotImplementedException();
    }

    public async Task<File<int>> CopyFileAsync(string fileId, int toFolderId, Guid chatId)
    {
        return await PerformCrossDaoFileCopyAsync(fileId, toFolderId, false, chatId);
    }

    public async Task<File<string>> CopyFileAsync(string fileId, string toFolderId)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        if (IsCrossDao(fileId, toFolderId))
        {
            return await PerformCrossDaoFileCopyAsync(fileId, toFolderId, false);
        }

        var fileDao = selector.GetFileDao(fileId);

        return await fileDao.CopyFileAsync(selector.ConvertId(fileId), selector.ConvertId(toFolderId));
    }

    public async Task<string> FileRenameAsync(File<string> file, string newTitle)
    {
        var selector = _selectorFactory.GetSelector(file.Id);
        var fileId = file.Id;
        var parentId = file.ParentId;

        var fileDao = selector.GetFileDao(file.Id);
        file.Id = ConvertId(file.Id);
        file.ParentId = ConvertId(file.ParentId);

        var newFileId = await fileDao.FileRenameAsync(file, newTitle);

        file.Id = fileId;
        file.ParentId = parentId;

        return newFileId;
    }

    public async Task<string> UpdateCommentAsync(string fileId, int fileVersion, string comment)
    {
        var selector = _selectorFactory.GetSelector(fileId);

        var fileDao = selector.GetFileDao(fileId);

        return await fileDao.UpdateCommentAsync(selector.ConvertId(fileId), fileVersion, comment);
    }

    public async Task CompleteVersionAsync(string fileId, int fileVersion)
    {
        var selector = _selectorFactory.GetSelector(fileId);

        var fileDao = selector.GetFileDao(fileId);

        await fileDao.CompleteVersionAsync(selector.ConvertId(fileId), fileVersion);
    }

    public async Task ContinueVersionAsync(string fileId, int fileVersion)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);

        await fileDao.ContinueVersionAsync(selector.ConvertId(fileId), fileVersion);
    }

    public bool UseTrashForRemove(File<string> file)
    {
        var selector = _selectorFactory.GetSelector(file.Id);
        var fileDao = selector.GetFileDao(file.Id);

        return fileDao.UseTrashForRemove(file);
    }

    public async Task SaveFormRoleMapping(string formId, IEnumerable<FormRole<string>> formRoles)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        if (formRoles == null || !formRoles.Any())
        {
            await filesDbContext.DeleteFormRoleMappingsAsync(tenantId, formId);
            return;
        }
        var sequence = 0;
        foreach (var formRole in formRoles)
        {
            sequence++;
            var roleDb = new DbThirdpartyFilesFormRoleMapping
            {
                TenantId = tenantId,
                FormId = formId,
                UserId = formRole.UserId,
                RoomId = formRole.RoomId,
                RoleName = formRole.RoleName,
                RoleColor = formRole.RoleColor,
                Sequence = sequence,
                Submitted = formRole.Submitted

            };
            await filesDbContext.ThirdpartyFilesFormRoleMapping.AddOrUpdateAsync(roleDb);
        }

        await filesDbContext.SaveChangesAsync();
    }
    public async IAsyncEnumerable<FormRole<string>> GetFormRoles(string formId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        await foreach (var r in filesDbContext.DbFormRolesAsync(tenantId, formId))
        {
            yield return r;
        }
    }
    public async Task<(int, List<FormRole<string>>)> GetUserFormRoles(string formId, Guid userId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var currentStep = await filesDbContext.DbFormRoleExistsAsync(tenantId, formId) ? await filesDbContext.DbFormRoleCurrentStepAsync(tenantId, formId) : -1;
        var roles = await GetFormUserRoles(formId, userId).ToListAsync();
        return (currentStep, roles);
    }
    public async IAsyncEnumerable<FormRole<string>> GetFormUserRoles(string formId, Guid userId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        await foreach (var r in filesDbContext.DbFormUserRolesQueryAsync(tenantId, formId.ToString(), userId))
        {
            yield return r;
        }
    }
    public async IAsyncEnumerable<FormRole<string>> GetUserFormRolesInRoom(string roomId, Guid userId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        await foreach (var r in filesDbContext.DbUserFormRolesInRoomQueryAsync(tenantId, roomId, userId))
        {
            yield return r;
        }
    }
    public async Task<FormRole<string>> ChangeUserFormRoleAsync(string formId, FormRole<string> formRole)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var toUpdate = await filesDbContext.FilesFormRoleAsync(tenantId, formId, formRole.RoleName, formRole.UserId);

        toUpdate.Submitted = formRole.Submitted;
        toUpdate.OpenedAt = formRole.OpenedAt;
        toUpdate.SubmissionDate = formRole.SubmissionDate;

        filesDbContext.Update(toUpdate);
        await filesDbContext.SaveChangesAsync();

        return formRole;
    }
    public async Task DeleteFormRolesAsync(string formId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var toDeleteRoles = await filesDbContext.DbFilesFormRoleMappingForDeleteAsync(tenantId, formId).ToListAsync();

        if (toDeleteRoles.Count != 0)
        {
            filesDbContext.RemoveRange(toDeleteRoles);
            await filesDbContext.SaveChangesAsync();
        }
    }

    public Task<int> UpdateCategoryAsync(string fileId, int fileVersion, int category, ForcesaveType forcesave)
    {
        return Task.FromResult(0);
    }

    #region chunking

    public async Task<ChunkedUploadSession<string>> CreateUploadSessionAsync(File<string> file, long contentLength)
    {
        var fileDao = GetFileDao(file);

        return await fileDao.CreateUploadSessionAsync(ConvertId(file), contentLength);
    }

    public async Task<File<string>> UploadChunkAsync(ChunkedUploadSession<string> uploadSession, Stream chunkStream, long chunkLength, int? chunkNumber = null)
    {
        if (chunkNumber.HasValue)
        {
            throw new ArgumentException("Can not async upload in provider folder.");
        }
        var fileDao = GetFileDao(uploadSession.File);
        uploadSession.File = ConvertId(uploadSession.File);
        await fileDao.UploadChunkAsync(uploadSession, chunkStream, chunkLength);

        return uploadSession.File;
    }

    public async Task<File<string>> FinalizeUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        var fileDao = GetFileDao(uploadSession.File);
        uploadSession.File = ConvertId(uploadSession.File);
        return await fileDao.FinalizeUploadSessionAsync(uploadSession);
    }

    public async Task AbortUploadSessionAsync(ChunkedUploadSession<string> uploadSession)
    {
        var fileDao = GetFileDao(uploadSession.File);
        uploadSession.File = ConvertId(uploadSession.File);

        await fileDao.AbortUploadSessionAsync(uploadSession);
    }

    private IFileDao<string> GetFileDao(File<string> file)
    {
        if (file.Id != null)
        {
            return _selectorFactory.GetSelector(file.Id).GetFileDao(file.Id);
        }

        if (file.ParentId != null)
        {
            return _selectorFactory.GetSelector(file.ParentId).GetFileDao(file.ParentId);
        }

        throw new ArgumentException("Can't create instance of dao for given file.", nameof(file));
    }

    private string ConvertId(string id)
    {
        return id != null ? _selectorFactory.GetSelector(id).ConvertId(id) : null;
    }

    private File<string> ConvertId(File<string> file)
    {
        file.Id = ConvertId(file.Id);
        file.ParentId = ConvertId(file.ParentId);

        return file;
    }

    public override Task<Stream> GetThumbnailAsync(string fileId, uint width, uint height)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);
        return fileDao.GetThumbnailAsync(selector.ConvertId(fileId), width, height);
    }

    public override Task<Stream> GetThumbnailAsync(File<string> file, uint width, uint height)
    {
        var fileDao = GetFileDao(file);
        return fileDao.GetThumbnailAsync(file, width, height);
    }

    public override async Task<EntryProperties<string>> GetProperties(string fileId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();
        var data = await filesDbContext.DataAsync(tenantId, fileId);
        return data != null ? EntryProperties<string>.Deserialize(data, logger) : null;
    }

    public override async Task<Dictionary<string, EntryProperties<string>>> GetPropertiesAsync(IEnumerable<string> filesIds)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        var properties = await filesDbContext.FilesPropertiesAsync(tenantId, filesIds).ToListAsync();

        var propertiesMap = new Dictionary<string, EntryProperties<string>>(properties.Count);
        foreach (var property in properties)
        {
            propertiesMap.TryAdd(property.EntryId, EntryProperties<string>.Deserialize(property.Data, logger));
        }

        return propertiesMap;
    }

    public override async Task SaveProperties(string fileId, EntryProperties<string> entryProperties)
    {
        string data;

        var tenantId = _tenantManager.GetCurrentTenantId();

        await using var filesDbContext = await dbContextFactory.CreateDbContextAsync();

        if (entryProperties == null || string.IsNullOrEmpty(data = EntryProperties<string>.Serialize(entryProperties, logger)))
        {
            await filesDbContext.DeleteFilesPropertiesAsync(tenantId, fileId.ToString());
            return;
        }

        await filesDbContext.AddOrUpdateAsync(r => r.FilesProperties, new DbFilesProperties { TenantId = tenantId, EntryId = fileId.ToString(), Data = data });
        await filesDbContext.SaveChangesAsync();
    }

    public async Task<int> SetCustomOrder(string fileId, string parentFolderId, int order)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);
        return await fileDao.SetCustomOrder(fileId, parentFolderId, order);
    }

    public async Task InitCustomOrder(Dictionary<string, int> fileIds, string parentFolderId)
    {
        var selector = _selectorFactory.GetSelector(parentFolderId);
        var fileDao = selector.GetFileDao(parentFolderId);
        await fileDao.InitCustomOrder(fileIds, parentFolderId);
    }

    public Task SetVectorizationStatusAsync(string fileId, VectorizationStatus status, Func<Task> action = null)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);
        return fileDao.SetVectorizationStatusAsync(selector.ConvertId(fileId), status);
    }
    
    public Task SetFileKey(string fileId, IEnumerable<FileKeyData> keys)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);
        return fileDao.SetFileKey(selector.ConvertId(fileId), keys);
    }
    
    public Task<List<FileKeys>> GetFileKeys(string fileId, Guid userId)
    {
        var selector = _selectorFactory.GetSelector(fileId);
        var fileDao = selector.GetFileDao(fileId);
        return fileDao.GetFileKeys(selector.ConvertId(fileId), userId);
    }
    
    public Task<long> GetTransferredBytesCountAsync(ChunkedUploadSession<string> uploadSession)
    {
        var fileDao = GetFileDao(uploadSession.File);
        uploadSession.File = ConvertId(uploadSession.File);
        return fileDao.GetTransferredBytesCountAsync(uploadSession);
    }

    #endregion
}
