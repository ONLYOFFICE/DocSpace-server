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

/// <summary>
/// The file operation information.
/// </summary>
public class FileOperationDto
{
    /// <summary>
    /// The file operation ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public required string Id { get; set; }

    /// <summary>
    /// The file operation type.
    /// </summary>
    /// <example>0</example>
    [JsonPropertyName("Operation")]
    public required FileOperationType OperationType { get; init; }

    /// <summary>
    /// The file operation progress in percentage.
    /// </summary>
    /// <example>100</example>
    public required int Progress { get; set; }

    /// <summary>
    /// The file operation error message.
    /// </summary>
    /// <example>File not found.</example>
    public required string Error { get; set; }

    /// <summary>
    /// The file operation processing status.
    /// </summary>
    /// <example>1</example>
    public required string Processed { get; set; }

    /// <summary>
    /// Specifies if the file operation is finished or not.
    /// </summary>
    /// <example>true</example>
    public required bool Finished { get; set; }

    /// <summary>
    /// The file operation URL.
    /// </summary>
    /// <example>http://localhost/download</example>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// The list of files of the file operation.
    /// </summary>
    /// <example>[{"id": 10, "title": "document.docx"}]</example>
    public List<FileEntryBaseDto> Files { get; set; }

    /// <summary>
    /// The list of folders of the file operation.
    /// </summary>
    /// <example>[{"id": 20, "title": "My Folder"}]</example>
    public List<FileEntryBaseDto> Folders { get; set; }

    /// <summary>
    /// The status of the distributed task related to the file operation.
    /// </summary>
    /// <example>0</example>
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