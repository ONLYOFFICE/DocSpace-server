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
    /// Stamps the file as just used by the calling account and puts it at the top of that account's Recent section,
    /// then answers with the file as it stands now. The list is personal: no other member sees the change, and the
    /// file itself is untouched. Read access is enough, so a room member with view-only rights and an invited guest
    /// may call it, and a visitor who reaches the file through an external link is recorded against that link. A
    /// caller without read access is refused with 403, and an identifier that resolves to nothing answers 404.
    /// Repeating the call is safe: the file keeps a single entry and only moves back to the top. The section holds
    /// the 1000 newest entries of an account and drops the oldest beyond that on its own; folders never enter it, and
    /// an encrypted file of a private room is answered normally but never recorded. Read the section back with
    /// `GET api/2.0/files/recent` and drop entries with `DELETE api/2.0/files/recent`; whether it is offered among
    /// the sections of `GET api/2.0/files/@root` is decided by `PUT api/2.0/files/displayrecent`.
    /// </remarks>
    /// <summary>Add a file to Recent</summary>
    /// <path>api/2.0/files/file/{fileId}/recent</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "The file as it stands after the entry was recorded", typeof(FileDto<int>))]
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
    /// Sets or clears the favorite mark of one file for the calling account: `true` adds the file to the favorites,
    /// `false` takes it out again. The call changes stored state even though it is a GET, so it is not one to issue
    /// speculatively; repeating it with the same value changes nothing further. The mark is personal, no other member
    /// sees it, and the file stays where it is stored. Read access is enough, so a room member with view-only rights
    /// and a guest may call it. The answer only echoes the value that was asked for: an identifier that resolves to
    /// nothing and a file the caller cannot read are skipped without a word, an encrypted file of a private room is
    /// never marked, and the requested value still comes back, so read the outcome from
    /// `GET api/2.0/files/@favorites` instead. A file moved to the Trash keeps its mark and is left out of that
    /// listing until it is restored. To mark several entries at once, or to mark folders, use
    /// `POST api/2.0/files/favorites` and `DELETE api/2.0/files/favorites`.
    /// </remarks>
    /// <summary>Set the file favorite status</summary>
    /// <path>api/2.0/files/favorites/{fileId}</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Echo of the requested state, which does not prove that the mark was changed", typeof(bool))]
    [SwaggerResponse(403, "Changing the favorite mark is refused for the caller")]
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
    /// Marks the listed files and folders as favorites for the calling account. The favorite list is personal:
    /// nothing changes for other members, and the entries stay where they are stored. Read access to each item is
    /// enough, so a room member with view-only rights and a guest may call it. Items the caller cannot read, ids that
    /// do not exist and encrypted files of a private room are skipped without a word, and the answer is `true` even
    /// when nothing was marked, so read the outcome back from `GET api/2.0/files/@favorites` instead of trusting it.
    /// Numeric ids address entries stored in the portal itself, string ids entries on a connected third-party
    /// account, and both kinds may be sent in one request. The call is mutating but safe to repeat: an item already
    /// marked stays listed once. An entry moved to the Trash keeps its mark and is left out of the listing until it
    /// is restored. `returnSingleOperation` arrives with the shared body and does nothing here. Use
    /// `DELETE api/2.0/files/favorites` to undo, or `GET api/2.0/files/favorites/{fileId}` for a single file.
    /// </remarks>
    /// <summary>Add favorite files and folders</summary>
    /// <path>api/2.0/files/favorites</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Always true: the request was understood, which does not mean that anything was marked", typeof(bool))]
    [SwaggerResponse(403, "Marking favorites is refused for the caller")]
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
    /// Adds the listed files to the personal template list of the calling account, the set the portal offers when a
    /// new document is started from an existing one. The list belongs to the account and no other member sees it.
    /// Every authenticated member type may manage their own list, a guest is refused, and read access to each file is
    /// required. Only formats the portal treats as template documents survive: the accepted extensions arrive in
    /// `extsWebTemplate` of `GET api/2.0/files/settings`, and a file of any other format is dropped silently. Only
    /// numeric ids are accepted, so a file on a connected third-party account cannot become a template. The answer is
    /// `true` whenever the request was understood, which an empty list, an id that does not exist and an unreadable
    /// file all achieve, so it confirms nothing about what was added; no operation of this document reads the list
    /// back. Repeating the call is safe. Use `DELETE api/2.0/files/templates` to drop a file again.
    /// </remarks>
    /// <summary>Add template files</summary>
    /// <path>api/2.0/files/templates</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Always true: the request was understood, which does not mean that anything was added", typeof(bool))]
    [HttpPost("templates")]
    public async Task<bool> AddTemplates(TemplatesRequestDto inDto)
    {
        await fileStorageService.AddToTemplatesAsync(inDto.FileIds);

        return true;
    }

    /// <remarks>
    /// Removes the favorite mark from the listed files and folders for the calling account. Nothing is deleted from
    /// storage: the entries keep their place, their content and their sharing, and only disappear from
    /// `GET api/2.0/files/@favorites`; to delete the entries themselves call `PUT api/2.0/files/fileops/delete`
    /// instead. Marks of other members are untouched, and read access to each item is enough to call it. The ids go
    /// into the JSON body documented here; the same route also accepts them as repeated `fileIds` and `folderIds`
    /// query parameters, but only in a request that carries no JSON body at all. Numeric ids address entries stored
    /// in the portal itself, string ids entries on a connected third-party account. The answer is `true` whenever the
    /// request was understood, which an empty request, an id that does not exist and an item that was never marked
    /// all achieve, so it does not report how many marks were dropped. `returnSingleOperation` arrives with the
    /// shared body and does nothing here. Repeating the call is safe. Use `POST api/2.0/files/favorites` to mark
    /// entries again.
    /// </remarks>
    /// <summary>Delete favorite files and folders</summary>
    /// <path>api/2.0/files/favorites</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Always true: the marks named in the request are gone or were never there", typeof(bool))]
    [HttpDelete("favorites")]
    [Consumes("application/json")]
    public async Task<bool> DeleteFavoritesFromBody([FromBody] BaseBatchRequestDto inDto)
    {
        return await DeleteFavorites(inDto);
    }

    /// <remarks>
    /// Removes the favorite mark from the listed files and folders for the calling account, taking the ids from the
    /// query string rather than from a body: `fileIds` and `folderIds` are repeated once per id, and the `fileIds[]`
    /// spelling is accepted as well. Nothing is deleted from storage — the entries keep their place and only
    /// disappear from `GET api/2.0/files/@favorites`; to delete them call `PUT api/2.0/files/fileops/delete`. Marks
    /// of other members are untouched, and read access to each item is enough to call it. Numeric ids address entries
    /// stored in the portal itself, string ids entries on a connected third-party account. The answer is `true`
    /// whenever the request was understood, which an empty query, an id that does not exist and an item that was
    /// never marked all achieve, so it does not report how many marks were dropped. Repeating the call is safe. Use
    /// `POST api/2.0/files/favorites` to mark entries again.
    /// </remarks>
    /// <summary>Delete favorite files and folders by query</summary>
    /// <path>api/2.0/files/favorites</path>
    [Tags("Files / Operations")]
    [SwaggerResponse(200, "Always true: the marks named in the query are gone or were never there", typeof(bool))]
    [HttpDelete("favorites")]
    public async Task<bool> DeleteFavoritesFromQuery([FromQuery][ModelBinder(BinderType = typeof(BaseBatchModelBinder))] BaseBatchRequestDto inDto)
    {
        return await DeleteFavorites(inDto);
    }

    /// <remarks>
    /// Takes the listed files off the personal template list of the calling account, leaving the files themselves
    /// untouched: only the template mark is dropped. The body of this request is a bare JSON array of numeric file
    /// ids rather than an object with a field, and a request that carries no array at all is rejected as an invalid
    /// request. Every authenticated member type may manage their own list, a guest is refused, and read access to a
    /// file is required for its mark to be dropped. The answer is `true` whenever the array was understood, which an
    /// empty array, an id that does not exist and a file that was never a template all achieve, so it confirms
    /// nothing about what was removed. Repeating the call is safe. Use `POST api/2.0/files/templates` to put a file
    /// back on the list; that operation expects an object with a `fileIds` field, so the two bodies are not
    /// interchangeable.
    /// </remarks>
    /// <summary>Delete template files</summary>
    /// <path>api/2.0/files/templates</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Always true: the files named in the array are no longer templates of the caller", typeof(bool))]
    [HttpDelete("templates")]
    public async Task<bool> DeleteTemplates(DeleteTemplateFilesRequestDto inDto)
    {
        await fileStorageService.DeleteTemplatesAsync(inDto.FileIds);

        return true;
    }

    /// <remarks>
    /// Removes the listed entries from the Recent section of the calling account, the history of opened files that
    /// `GET api/2.0/files/recent` returns. Nothing is deleted from storage and no other member's history is touched;
    /// access to the entries is not checked at all, so a file the caller can no longer read can still be cleared from
    /// their own history. Only numeric file ids are honoured, so a file on a connected third-party account cannot be
    /// cleared this way, and folder ids are accepted but change nothing because the section lists files only. The
    /// answer carries no body and reports nothing about how many entries were found: an empty request and an id that
    /// was never in the section are accepted alike. Repeating the call is safe, but an entry returns the next time
    /// the file is opened or `POST api/2.0/files/file/{fileId}/recent` is called for it. To hide the whole section
    /// instead, call `PUT api/2.0/files/displayrecent`.
    /// </remarks>
    /// <summary>Delete recent files</summary>
    /// <path>api/2.0/files/recent</path>
    [Tags("Files / Files")]
    [SwaggerResponse(200, "Empty answer: the listed entries no longer appear in the Recent section")]
    [HttpDelete("recent")]
    public async Task<NoContentResult> DeleteRecent(BaseBatchRequestDto inDto)
    {
        var (folderIntIds, folderStringIds) = FileOperationsManager.GetIds(inDto.FolderIds);
        var (fileIntIds, _) = FileOperationsManager.GetIds(inDto.FileIds);

        var t1 = fileStorageService.DeleteFromRecentAsync(folderIntIds, fileIntIds);
        var t2 = fileStorageService.DeleteFromRecentAsync(folderStringIds, []);

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
