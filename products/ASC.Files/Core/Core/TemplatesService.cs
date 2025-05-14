// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Files.Core.Core;

/// <summary>
/// The TemplatesService class provides methods for managing template files within the system.
/// </summary>
/// <remarks>
/// This service enables the addition, deletion, and retrieval of template files.
/// Templates are managed with tags specifically marked for templates.
/// </remarks>
[Scope]
public class TemplatesService(
    AuthContext authContext,
    UserManager userManager,
    FileUtility fileUtility,
    FileSecurity fileSecurity,
    IDaoFactory daoFactory,
    EntryManager entryManager,
    ILogger<ThirdPartyIntegrationService> logger)
{
    /// <summary>
    /// Adds specified files to the templates.
    /// </summary>
    /// <typeparam name="T">The type of the files being processed.</typeparam>
    /// <param name="filesId">A collection of file identifiers to be added as templates.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of file entries added as templates.</returns>
    /// <exception cref="SecurityException">Thrown when the current user is a guest and does not have the necessary permissions.</exception>
    public async ValueTask<List<FileEntry<T>>> AddToTemplatesAsync<T>(IEnumerable<T> filesId)
    {
        if (await userManager.IsGuestAsync(authContext.CurrentAccount.ID))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }

        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var files = await fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId))
            .Where(file => fileUtility.ExtsWebTemplate.Contains(FileUtility.GetFileExtension(file.Title), StringComparer.CurrentCultureIgnoreCase))
            .ToListAsync();

        var tags = files.Select(file => Tag.Template(authContext.CurrentAccount.ID, file));

        await tagDao.SaveTagsAsync(tags);

        return files;
    }

    /// <summary>
    /// Deletes specified files from the templates.
    /// </summary>
    /// <typeparam name="T">The type of the files being processed.</typeparam>
    /// <param name="filesId">A collection of file identifiers to be removed from templates.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task DeleteTemplatesAsync<T>(IEnumerable<T> filesId)
    {
        var tagDao = daoFactory.GetTagDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var files = await fileSecurity.FilterReadAsync(fileDao.GetFilesAsync(filesId)).ToListAsync();

        var tags = files.Select(file => Tag.Template(authContext.CurrentAccount.ID, file));

        await tagDao.RemoveTagsAsync(tags);
    }


    /// <summary>
    /// Retrieves a collection of template file entries based on specified filters and search criteria.
    /// </summary>
    /// <typeparam name="T">The type parameter representing the file entity.</typeparam>
    /// <param name="filter">Specifies the type of files or folders to include, based on the <see cref="FilterType"/> enumeration.</param>
    /// <param name="from">The starting index for pagination.</param>
    /// <param name="count">The number of entries to return for pagination.</param>
    /// <param name="subjectGroup">Indicates whether the subject is a group.</param>
    /// <param name="subjectId">The unique identifier of the subject to filter by, or null for no specific subject.</param>
    /// <param name="searchText">The search text to filter file entries by name or content.</param>
    /// <param name="extension">An array of file extensions to filter by.</param>
    /// <param name="searchInContent">Indicates whether to search within the content of the files.</param>
    /// <returns>An asynchronous enumerable of template file entries matching the specified criteria.</returns>
    public async IAsyncEnumerable<FileEntry<T>> GetTemplatesAsync<T>(FilterType filter, int from, int count, bool subjectGroup, Guid? subjectId, string searchText, string[] extension, bool searchInContent)
    {
        subjectId ??= Guid.Empty;
        var folderDao = daoFactory.GetFolderDao<T>();
        var fileDao = daoFactory.GetFileDao<T>();

        var result = entryManager.GetTemplatesAsync(folderDao, fileDao, filter, subjectGroup, subjectId.Value, searchText, extension, searchInContent);

        await foreach (var r in result.Skip(from).Take(count))
        {
            yield return r;
        }
    }
}