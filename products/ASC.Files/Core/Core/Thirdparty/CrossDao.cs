﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Files.Core.Thirdparty;

[Scope]
internal class CrossDao //Additional SharpBox
(IServiceProvider serviceProvider,
        SetupInfo setupInfo,
        FileConverter fileConverter,
        SocketManager socketManager)
{
    public async Task<File<TTo>> PerformCrossDaoFileCopyAsync<TFrom, TTo>(
        TFrom fromFileId, IFileDao<TFrom> fromFileDao, Func<TFrom, TFrom> fromConverter,
        TTo toFolderId, IFileDao<TTo> toFileDao, Func<TTo, TTo> toConverter,
        bool deleteSourceFile)
    {
        //Get File from first dao
        var fromFile = await fromFileDao.GetFileAsync(fromConverter(fromFileId));

        if (fromFile.ContentLength > setupInfo.AvailableFileSize)
        {
            throw new Exception(string.Format(deleteSourceFile ? FilesCommonResource.ErrorMessage_FileSizeMove : FilesCommonResource.ErrorMessage_FileSizeCopy,
                                              FileSizeComment.FilesSizeToString(setupInfo.AvailableFileSize)));
        }
        
        var securityDao = serviceProvider.GetService<ISecurityDao<TFrom>>();
        var securityDaoTo = serviceProvider.GetService<ISecurityDao<TTo>>();
        var tagDao = serviceProvider.GetService<ITagDao<TFrom>>();

        var fromFileCopy = (File<TFrom>)fromFile.Clone();
        
        var fromFileShareRecords = securityDao.GetPureShareRecordsAsync(fromFileCopy);
        var fromFileNewTags = tagDao.GetNewTagsAsync(Guid.Empty, fromFileCopy);
        var fromFileLockTag = await tagDao.GetTagsAsync(fromFileId, FileEntryType.File, TagType.Locked).FirstOrDefaultAsync();
        var fromFileFavoriteTag = await tagDao.GetTagsAsync(fromFile.Id, FileEntryType.File, TagType.Favorite).ToListAsync();
        var fromFileTemplateTag = await tagDao.GetTagsAsync(fromFile.Id, FileEntryType.File, TagType.Template).ToListAsync();

        var toFile = serviceProvider.GetService<File<TTo>>();

        toFile.Title = fromFile.Title;
        toFile.Encrypted = fromFile.Encrypted;
        toFile.ParentId = toConverter(toFolderId);
        toFile.ThumbnailStatus = Thumbnail.Waiting;

        fromFile.Id = fromConverter(fromFile.Id);

        var mustConvert = !string.IsNullOrEmpty(fromFile.ConvertedType);
        await using (var fromFileStream = mustConvert
                         ? await fileConverter.ExecAsync(fromFile)
                         : await fromFileDao.GetFileStreamAsync(fromFile))
        {
            toFile.ContentLength = fromFileStream.CanSeek ? fromFileStream.Length : fromFile.ContentLength;
            toFile = await toFileDao.SaveFileAsync(toFile, fromFileStream);
        }

        if (!deleteSourceFile)
        {
            return toFile;
        }

        await foreach (var record in fromFileShareRecords.Where(x => x.EntryType == FileEntryType.File))
        {
            var toRecord = new FileShareRecord<TTo>
            {
                TenantId = record.TenantId,
                EntryId = toFile.Id,
                EntryType = record.EntryType,
                SubjectType = record.SubjectType,
                Subject = record.Subject,
                Owner = record.Owner,
                Share = record.Share,
                Options = record.Options,
                Level = record.Level
            };
            
            await securityDaoTo.SetShareAsync(toRecord);
        }

        var fromFileTags = await fromFileNewTags.ToListAsync();
        if (fromFileLockTag != null)
        {
            fromFileTags.Add(fromFileLockTag);
        }

        fromFileTags.AddRange(fromFileFavoriteTag);
        fromFileTags.AddRange(fromFileTemplateTag);

        if (fromFileTags.Count > 0)
        {
            fromFileTags.ForEach(x => x.EntryId = toFile.Id);

            await tagDao.SaveTagsAsync(fromFileTags);
        }

        //Delete source file if needed
        await fromFileDao.DeleteFileAsync(fromConverter(fromFileId));


        return toFile;
    }

    public async Task<Folder<TTo>> PerformCrossDaoFolderCopyAsync<TFrom, TTo>
        (TFrom fromFolderId, IFolderDao<TFrom> fromFolderDao, IFileDao<TFrom> fromFileDao, Func<TFrom, TFrom> fromConverter,
        TTo toRootFolderId, IFolderDao<TTo> toFolderDao, IFileDao<TTo> toFileDao, Func<TTo, TTo> toConverter,
        bool deleteSourceFolder, CancellationToken? cancellationToken)
    {
        var fromFolder = await fromFolderDao.GetFolderAsync(fromConverter(fromFolderId));

        var toFolder1 = serviceProvider.GetService<Folder<TTo>>();
        toFolder1.Title = fromFolder.Title;
        toFolder1.ParentId = toConverter(toRootFolderId);

        var toFolder = await toFolderDao.GetFolderAsync(fromFolder.Title, toConverter(toRootFolderId));
        var toFolderId = toFolder != null
                             ? toFolder.Id
                             : await toFolderDao.SaveFolderAsync(toFolder1);

        if (toFolder == null)
        {
            await socketManager.CreateFolderAsync(await toFolderDao.GetFolderAsync(toConverter(toFolderId)));
        }

        var foldersToCopy = await fromFolderDao.GetFoldersAsync(fromConverter(fromFolderId)).ToListAsync();
        var fileIdsToCopy = await fromFileDao.GetFilesAsync(fromConverter(fromFolderId)).ToListAsync();
        Exception copyException = null;
        
        //Copy files first
        foreach (var fileId in fileIdsToCopy)
        {
            cancellationToken?.ThrowIfCancellationRequested();

            try
            {
                await PerformCrossDaoFileCopyAsync(fileId, fromFileDao, fromConverter,
                    toFolderId, toFileDao, toConverter,
                    deleteSourceFolder);
            }
            catch (Exception ex)
            {
                copyException = ex;
            }
        }
        
        foreach (var folder in foldersToCopy)
        {
            cancellationToken?.ThrowIfCancellationRequested();

            try
            {
                await PerformCrossDaoFolderCopyAsync(folder.Id, fromFolderDao, fromFileDao, fromConverter,
                    toFolderId, toFolderDao, toFileDao, toConverter,
                    deleteSourceFolder, cancellationToken);
            }
            catch (Exception ex)
            {
                copyException = ex;
            }
        }

        if (deleteSourceFolder)
        {
            var securityDao = serviceProvider.GetService<ISecurityDao<TFrom>>();
            var securityDaoTo = serviceProvider.GetService<ISecurityDao<TTo>>();
            var fromFileShareRecords = securityDao.GetPureShareRecordsAsync(fromFolder);

            await foreach (var record in fromFileShareRecords.Where(x => x.EntryType == FileEntryType.Folder))
            {
                var toRecord = new FileShareRecord<TTo>
                {
                    TenantId = record.TenantId,
                    EntryId = toFolderId,
                    EntryType = record.EntryType,
                    SubjectType = record.SubjectType,
                    Subject = record.Subject,
                    Owner = record.Owner,
                    Share = record.Share,
                    Options = record.Options,
                    Level = record.Level
                };
                
                await securityDaoTo.SetShareAsync(toRecord);
            }

            var tagDao = serviceProvider.GetService<ITagDao<TFrom>>();
            var fromFileNewTags = await tagDao.GetNewTagsAsync(Guid.Empty, fromFolder).ToListAsync();

            if (fromFileNewTags.Count > 0)
            {
                fromFileNewTags.ForEach(x => x.EntryId = toFolderId);

                await tagDao.SaveTagsAsync(fromFileNewTags);
            }

            if (copyException == null)
            {
                var id = fromConverter(fromFolderId);
                var folder = await fromFolderDao.GetFolderAsync(id);
                await socketManager.DeleteFolder(folder, action: async () => await fromFolderDao.DeleteFolderAsync(id));
            }
        }

        if (copyException != null)
        {
            throw copyException;
        }

        return await toFolderDao.GetFolderAsync(toConverter(toFolderId));
    }
}