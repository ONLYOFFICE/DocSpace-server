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

namespace ASC.Files.Core.ApiModels.ResponseDto;

/// <summary>One background file operation of the caller, as it stood when the answer was built.</summary>
public class FileOperationDto
{
    /// <summary>
    /// The identifier of the operation, the one to pass to `PUT api/2.0/files/fileops/terminate/{id}` to stop it.
    /// Operations belong to the account that started them, so an identifier of somebody else is never listed here.
    /// </summary>
    /// <example>a1f4c9b2-3d8e-4f77-9b16-2c5de8f0a913</example>
    public required string Id { get; set; }

    /// <summary>
    /// What the operation does with the entries, which also decides what else is reported: only a download fills
    /// `url`, and a deletion leaves `files` and `folders` empty.
    /// </summary>
    /// <example>3</example>
    [JsonPropertyName("Operation")]
    public required FileOperationType OperationType { get; init; }

    /// <summary>
    /// How far the operation has come, from 0 to 100. Reaching 100 only means it stopped; whether it did what it was
    /// asked for is told by `error`.
    /// </summary>
    /// <example>100</example>
    public required int Progress { get; set; }

    /// <summary>
    /// The reason the operation could not finish its work, in the language of the request. Empty when nothing went
    /// wrong, which is the only way to tell a successful operation from a failed one.
    /// </summary>
    /// <example>Folder not found.</example>
    public required string Error { get; set; }

    /// <summary>
    /// How many entries the operation has handled so far, written as a decimal number in a string. It counts items,
    /// not percent, and stays behind `progress` on operations that walk into subfolders.
    /// </summary>
    /// <example>12</example>
    public required string Processed { get; set; }

    /// <summary>
    /// Whether the operation has stopped running. A finished operation is reported once and then dropped, so the next
    /// read of the operation list no longer contains it.
    /// </summary>
    /// <example>true</example>
    public required bool Finished { get; set; }

    /// <summary>
    /// The address the packed archive can be downloaded from once a bulk download has finished. Empty for every other
    /// kind of operation.
    /// </summary>
    /// <example>https://portal.example.com/filehandler.ashx?action=bulk</example>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// The files the operation produced or moved, in the order it wrote them down. Empty while nothing has been
    /// written yet and for a deletion, which reports no entries at all.
    /// </summary>
    /// <example>[{"id": 10, "title": "document.docx"}]</example>
    public List<FileEntryBaseDto> Files { get; set; }

    /// <summary>
    /// The folders the operation produced or moved, in the order it wrote them down. Empty while nothing has been
    /// written yet and for a deletion.
    /// </summary>
    /// <example>[{"id": 20, "title": "Reports"}]</example>
    public List<FileEntryBaseDto> Folders { get; set; }

    /// <summary>
    /// The state of the background task behind the operation, which tells a task that was cancelled or that crashed
    /// from one that ran to its end.
    /// </summary>
    /// <example>2</example>
    public DistributedTaskStatus Status { get; set; }
}

[Scope]
public class FileOperationDtoHelper(
    FolderDtoHelper folderWrapperHelper,
    FileDtoHelper filesWrapperHelper,
    IDaoFactory daoFactory,
    CommonLinkUtility commonLinkUtility)
{
    public async Task<FileOperationDto> GetAsync(FileOperationResult o)
    {
        var result = new FileOperationDto
        {
            Id = o.Id,
            OperationType = o.OperationType,
            Progress = o.Progress,
            Error = o.Error,
            Processed = o.Processed,
            Finished = o.Finished,
            Status = o.Status
        };

        if (string.IsNullOrEmpty(o.Result) || result.OperationType == FileOperationType.Delete)
        {
            return result;
        }

        {
            var arr = o.Result.Split(':');
            var folders = arr
                .Where(s => s.StartsWith("folder_"))
                .Select(s => s[7..])
                .ToList();

            if (folders.Count > 0)
            {
                var fInt = new List<int>();
                var fString = new List<string>();

                foreach (var folder in folders)
                {
                    if (int.TryParse(folder, out var f))
                    {
                        fInt.Add(f);
                    }
                    else
                    {
                        fString.Add(folder);
                    }
                }

                var internalFolders = GetFoldersAsync(fInt).ToListAsync();
                var thirdPartyFolders = GetFoldersAsync(fString).ToListAsync();

                result.Folders = [];
                foreach (var f in await Task.WhenAll(internalFolders.AsTask(), thirdPartyFolders.AsTask()))
                {
                    result.Folders.AddRange(f);
                }
            }

            var files = arr
                .Where(s => s.StartsWith("file_"))
                .Select(s => s[5..])
                .ToList();

            if (files.Count > 0)
            {
                var fInt = new List<int>();
                var fString = new List<string>();

                foreach (var file in files)
                {
                    if (int.TryParse(file, out var f))
                    {
                        fInt.Add(f);
                    }
                    else
                    {
                        fString.Add(file);
                    }
                }

                var internalFiles = GetFilesAsync(fInt).ToListAsync();
                var thirdPartyFiles = GetFilesAsync(fString).ToListAsync();

                result.Files = [];

                foreach (var f in await Task.WhenAll(internalFiles.AsTask(), thirdPartyFiles.AsTask()))
                {
                    result.Files.AddRange(f);
                }
            }

            if (result.OperationType == FileOperationType.Download)
            {
                result.Url = commonLinkUtility.GetFullAbsolutePath(o.Result);
            }
        }

        return result;

        async IAsyncEnumerable<FileEntryBaseDto> GetFoldersAsync<T>(IEnumerable<T> folders)
        {
            var folderDao = daoFactory.GetFolderDao<T>();

            await foreach (var folder in folderDao.GetFoldersAsync(folders))
            {
                yield return await folderWrapperHelper.GetAsync(folder);
            }
        }

        async IAsyncEnumerable<FileEntryBaseDto> GetFilesAsync<T>(IEnumerable<T> files)
        {
            var fileDao = daoFactory.GetFileDao<T>();

            await foreach (var file in fileDao.GetFilesAsync(files))
            {
                yield return await filesWrapperHelper.GetAsync(file);
            }
        }
    }
}