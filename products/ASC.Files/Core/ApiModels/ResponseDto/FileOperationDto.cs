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

namespace ASC.Files.Core.ApiModels.ResponseDto;

public class FileOperationDto
{
    /// <summary>
    /// Operation ID
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Operation type
    /// </summary>
    [JsonPropertyName("Operation")]
    public FileOperationType OperationType { get; init; }

    /// <summary>
    /// Operation progress
    /// </summary>
    [SwaggerSchemaCustom(Example = 100)]
    public int Progress { get; set; }

    /// <summary>
    /// Error
    /// </summary>
    [SwaggerSchemaCustom(Example = "")]
    public string Error { get; set; }

    /// <summary>
    /// Processing status
    /// </summary>
    [SwaggerSchemaCustom(Example = "1")]
    public string Processed { get; set; }

    /// <summary>
    /// Specifies if the operation is finished or not
    /// </summary>
    public bool Finished { get; set; }

    /// <summary>
    /// URL
    /// </summary>
    [Url]
    public string Url { get; set; }

    /// <summary>
    /// List of files
    /// </summary>
    public List<FileEntryDto> Files { get; set; }

    /// <summary>
    /// List of folders
    /// </summary>
    public List<FileEntryDto> Folders { get; set; }
}

[Scope]
public class FileOperationDtoHelper(FolderDtoHelper folderWrapperHelper,
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
            Finished = o.Finished
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

        async IAsyncEnumerable<FileEntryDto> GetFoldersAsync<T>(IEnumerable<T> folders)
        {
            var folderDao = daoFactory.GetFolderDao<T>();

            await foreach (var folder in folderDao.GetFoldersAsync(folders))
            {
                yield return await folderWrapperHelper.GetAsync(folder);
            }
        }

        async IAsyncEnumerable<FileEntryDto> GetFilesAsync<T>(IEnumerable<T> files)
        {
            var fileDao = daoFactory.GetFileDao<T>();

            await foreach (var file in fileDao.GetFilesAsync(files))
            {
                yield return await filesWrapperHelper.GetAsync(file);
            }
        }
    }
}
