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

namespace ASC.Files.Api;

[ConstraintRoute("int")]
public class TagsControllerInternal(FileStorageService fileStorageService,
        EntryManager entryManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : TagsController<int>(fileStorageService, entryManager, folderDtoHelper, fileDtoHelper);

public class TagsControllerThirdparty(FileStorageService fileStorageService,
        EntryManager entryManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : TagsController<string>(fileStorageService, entryManager, folderDtoHelper, fileDtoHelper);

public abstract class TagsController<T>(FileStorageService fileStorageService,
        EntryManager entryManager,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Adds a file to the top of the Recent section of the calling account and returns the file. Other users do not
    /// see the change, and the file itself stays unchanged. Requires read access to the file, including access
    /// through an external link. Other callers get 403. The call changes data. You can repeat it, and the file keeps
    /// one entry. The section keeps the 1000 newest entries of the account and deletes older ones. The call returns
    /// an encrypted file of a private room but does not add it. To read the section, use `GET api/2.0/files/recent`.
    /// To delete entries, use `DELETE api/2.0/files/recent`. To hide the section, use
    /// `PUT api/2.0/files/displayrecent`. The call returns 404 for an unknown file or for a string id with an unknown
    /// storage type. If a string id points to a storage account that is not connected, the call returns 400.
    /// </remarks>
    /// <summary>Add a file to Recent</summary>
    /// <path>api/2.0/files/file/{fileId}/recent</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file as it stands after the entry was recorded", typeof(FileDto<int>))]
    [SwaggerResponse(400, "A third-party file identifier refers to a storage account that is not connected")]
    [SwaggerResponse(403, "The calling account cannot read this file")]
    [SwaggerResponse(404, "No file answers to this identifier")]
    [HttpPost("file/{fileId}/recent")]
    public async Task<FileDto<T>> AddFileToRecent(FileIdRequestDto<T> inDto)
    {
        var file = (await fileStorageService.GetFileAsync(inDto.FileId, -1)).NotFoundIfNull("File not found");

        await entryManager.MarkAsRecent(file);

        return await _fileDtoHelper.GetAsync(file);
    }

    /// <remarks>
    /// Adds one file to the favorites of the calling account, or deletes it from them. If `favorite` is `true`, the
    /// call adds the mark. If it is `false` or missing, the call deletes the mark. Other users do not see the mark,
    /// and the file stays where it is. Any signed-in user can call it. Although this is a GET, the call changes data.
    /// You can repeat it with the same value. The call skips an unknown file and a file without read access. It never
    /// marks an encrypted file of a private room. It returns the value of `favorite` even when it changes nothing. To
    /// see the result, use `GET api/2.0/files/@favorites`. A marked file in the trash is not listed there until it is
    /// restored. To change the marks of several files and folders, use `POST api/2.0/files/favorites` or
    /// `DELETE api/2.0/files/favorites`. If a string id points to a storage account that is not connected, the call
    /// returns 400. It also returns 400 if `favorite` is not a boolean. If a string id names an unknown storage type,
    /// the call returns 404.
    /// </remarks>
    /// <summary>Set the file favorite status</summary>
    /// <path>api/2.0/files/favorites/{fileId}</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Echo of the requested state, which does not prove that the mark was changed", typeof(bool))]
    [SwaggerResponse(400, "A parameter has the wrong type, or a third-party file identifier refers to a storage account that is not connected")]
    [SwaggerResponse(404, "A third-party file identifier names a storage type the portal does not know")]
    [HttpGet("favorites/{fileId}")]
    public async Task<bool> ToggleFileFavorite(ToggleFileFavoriteRequestDto<T> inDto)
    {
        return await fileStorageService.ToggleFileFavoriteAsync(inDto.FileId, inDto.Favorite);
    }
}

public class TagsControllerCommon(FileStorageService fileStorageService,
        FolderDtoHelper folderDtoHelper,
        FileDtoHelper fileDtoHelper)
    : ApiControllerBase(folderDtoHelper, fileDtoHelper)
{
    /// <remarks>
    /// Marks files and folders as favorites of the calling account. Other users do not see the mark, and the entries
    /// stay where they are. Any signed-in user can call it. The call skips items without read access, unknown ids,
    /// and encrypted files of a private room. The call changes data. You can repeat it with the same body, and a
    /// marked item stays listed once. It returns `true` even when it marks nothing. To see the result, use
    /// `GET api/2.0/files/@favorites`. A marked entry in the trash is not listed there until it is restored. The call
    /// reads only `fileIds` and `folderIds` from the body. To delete marks, use `DELETE api/2.0/files/favorites`. To
    /// mark one file, use `GET api/2.0/files/favorites/{fileId}`. If a string id points to a storage account that is
    /// not connected, the call returns 400. If it names an unknown storage type, the call returns 404.
    /// </remarks>
    /// <summary>Add favorite files and folders</summary>
    /// <path>api/2.0/files/favorites</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Always true: the request was understood, which does not mean that anything was marked", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(404, "A third-party identifier names a storage type the portal does not know")]
    [HttpPost("favorites")]
    public async Task<bool> AddFavorites(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        await fileStorageService.AddToFavoritesAsync(folderIntIds, fileIntIds);
        await fileStorageService.AddToFavoritesAsync(folderStringIds, fileStringIds);

        return true;
    }

    /// <remarks>
    /// Adds files to the personal template list of the calling account. The portal offers this list when a user
    /// starts a new document from an existing one. Any signed-in user can call it, except a guest. A guest gets 403.
    /// The call skips files without read access and unknown ids. It also skips a file whose extension is not in
    /// `extsWebTemplate` of `GET api/2.0/files/settings`. The call changes data. You can repeat it with the same
    /// body. It returns `true` even when it adds nothing. No operation of this API reads the list back. To delete
    /// files from the list, use `DELETE api/2.0/files/templates`.
    /// </remarks>
    /// <summary>Add template files</summary>
    /// <path>api/2.0/files/templates</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Always true: the request was understood, which does not mean that anything was added", typeof(bool))]
    [SwaggerResponse(403, "The caller is a guest")]
    [HttpPost("templates")]
    public async Task<bool> AddTemplates(TemplatesRequestDto inDto)
    {
        await fileStorageService.AddToTemplatesAsync(inDto.FileIds);

        return true;
    }

    /// <remarks>
    /// Deletes the favorite marks of the calling account from files and folders. The entries themselves stay
    /// unchanged and no longer appear in `GET api/2.0/files/@favorites`. Marks of other users stay. Any signed-in
    /// user can call it. The call skips items without read access. The call changes data. You can repeat it with the
    /// same body. It returns `true` even when it deletes nothing. The call reads only `fileIds` and `folderIds` from
    /// the body. The route also accepts them as repeated query parameters, but only in a request without a body. To
    /// delete the entries themselves, use `PUT api/2.0/files/fileops/delete`. To mark entries again, use
    /// `POST api/2.0/files/favorites`. If a string id points to a storage account that is not connected, the call
    /// returns 400. If it names an unknown storage type, the call returns 404.
    /// </remarks>
    /// <summary>Delete favorite files and folders</summary>
    /// <path>api/2.0/files/favorites</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Always true: the marks named in the request are gone or were never there", typeof(bool))]
    [SwaggerResponse(400, "The request body cannot be read, or a third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(404, "A third-party identifier names a storage type the portal does not know")]
    [HttpDelete("favorites")]
    [Consumes("application/json")]
    public async Task<bool> DeleteFavoritesFromBody([FromBody] BaseBatchRequestDto inDto)
    {
        return await DeleteFavorites(inDto);
    }

    /// <remarks>
    /// Deletes the favorite marks of the calling account from files and folders. The call takes the ids from the
    /// query, not from a body. Repeat `fileIds` and `folderIds` once per id. The `fileIds[]` form also works. A
    /// number is an id in the portal, and a string is an id in a connected third-party storage. The entries
    /// themselves stay unchanged and no longer appear in `GET api/2.0/files/@favorites`. Marks of other users stay.
    /// Any signed-in user can call it. The call skips items without read access. The call changes data. You can
    /// repeat it with the same query. It returns `true` even when it deletes nothing. To delete the entries
    /// themselves, use `PUT api/2.0/files/fileops/delete`. To mark entries again, use `POST api/2.0/files/favorites`.
    /// If a string id points to a storage account that is not connected, the call returns 400. If it names an
    /// unknown storage type, the call returns 404.
    /// </remarks>
    /// <summary>Delete favorite files and folders by query</summary>
    /// <path>api/2.0/files/favorites</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Always true: the marks named in the query are gone or were never there", typeof(bool))]
    [SwaggerResponse(400, "A third-party identifier refers to a storage account that is not connected")]
    [SwaggerResponse(404, "A third-party identifier names a storage type the portal does not know")]
    [HttpDelete("favorites")]
    public async Task<bool> DeleteFavoritesFromQuery([FromQuery][ModelBinder(BinderType = typeof(BaseBatchModelBinder))] BaseBatchRequestDto inDto)
    {
        return await DeleteFavorites(inDto);
    }

    /// <remarks>
    /// Deletes files from the personal template list of the calling account. The files themselves stay unchanged.
    /// Any signed-in user can call it, except a guest. A guest gets 403. The call skips files without read access.
    /// The call changes data. You can repeat it with the same body. It returns `true` even when it deletes nothing.
    /// The body is a bare JSON array of file ids. The call returns 400 if the body is not such an array. To add files
    /// to the list, use `POST api/2.0/files/templates`. That call takes an object with `fileIds`, not a bare array.
    /// </remarks>
    /// <summary>Delete template files</summary>
    /// <path>api/2.0/files/templates</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Always true: the files named in the array are no longer templates of the caller", typeof(bool))]
    [SwaggerResponse(400, "The body is not a JSON array of file ids")]
    [SwaggerResponse(403, "The caller is a guest")]
    [HttpDelete("templates")]
    public async Task<bool> DeleteTemplates(DeleteTemplateFilesRequestDto inDto)
    {
        await fileStorageService.DeleteTemplatesAsync(inDto.FileIds);

        return true;
    }

    /// <remarks>
    /// Deletes files from the Recent section of the calling account, which `GET api/2.0/files/recent` returns. The
    /// files themselves and the sections of other users stay unchanged. Any signed-in user can call it. The call does
    /// not check access, so you can delete a file that you can no longer read. Folder ids change nothing. If a
    /// third-party storage no longer has the file, its entry stays. The call changes data. You can repeat it with the
    /// same body. It returns an empty body and does not report what it deleted. The portal adds an entry again when
    /// the caller opens the file or calls `POST api/2.0/files/file/{fileId}/recent`. The call reads only `fileIds`
    /// and `folderIds` from the body. To hide the whole section, use `PUT api/2.0/files/displayrecent`. If a string
    /// id points to a storage account that is not connected, the call returns 400. If it names an unknown storage
    /// type, the call returns 404.
    /// </remarks>
    /// <summary>Delete recent files</summary>
    /// <path>api/2.0/files/recent</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Empty answer: the listed entries no longer appear in the Recent section")]
    [SwaggerResponse(400, "The request body cannot be read, or a third-party file or folder identifier refers to a storage account that is not connected")]
    [SwaggerResponse(404, "A third-party file or folder identifier names a storage type the portal does not know")]
    [HttpDelete("recent")]
    public async Task<NoContentResult> DeleteRecent(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        var t1 = fileStorageService.DeleteFromRecentAsync(folderIntIds, fileIntIds);
        var t2 = fileStorageService.DeleteFromRecentAsync(folderStringIds, fileStringIds);

        await Task.WhenAll(t1, t2);

        return NoContent();
    }

    private async Task<bool> DeleteFavorites(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, fileStringIds) = FileOperationsManager.GetIds(inDto.FileIds);

        await fileStorageService.DeleteFavoritesAsync(folderIntIds, fileIntIds);
        await fileStorageService.DeleteFavoritesAsync(folderStringIds, fileStringIds);

        return true;
    }
}
