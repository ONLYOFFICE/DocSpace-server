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

namespace ASC.Files.Core.ApiModels.RequestDto;

/// <summary>
/// The parameter shared by every request that starts a background file operation.
/// </summary>
public abstract class FileOperationRequestBaseDto
{
    /// <summary>
    /// Which operations the answer carries: `true` returns the operation this call started and nothing else, `false`
    /// returns every operation of the same kind that the caller has running or unread. When nothing was queued, which
    /// happens for an empty selection, `true` falls back to the full list.
    /// </summary>
    /// <example>false</example>
    public bool ReturnSingleOperation { get; set; }
}

/// <summary>
/// The files and folders a background operation is applied to.
/// </summary>
public class BaseBatchRequestDto : FileOperationRequestBaseDto
{
    /// <summary>
    /// The folders to act on, by id, as reported by a folder listing such as `GET api/2.0/files/{folderId}`. A number
    /// addresses a folder stored in the portal itself, a string addresses a folder on a connected third-party
    /// account, and both kinds may be sent in one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FolderIds { get; set; } = [];

    /// <summary>
    /// The files to act on, by id, as reported by a folder listing such as `GET api/2.0/files/{folderId}`. A number
    /// addresses a file stored in the portal itself, a string addresses a file on a connected third-party account,
    /// and both kinds may be sent in one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FileIds { get; set; } = [];
}

/// <summary>
/// The files and folders to pack into one archive, together with the formats they are converted to.
/// </summary>
public class DownloadRequestDto : FileOperationRequestBaseDto
{
    /// <summary>
    /// The folders to pack, by id; everything inside them that the caller may read goes into the archive. A number
    /// addresses a folder stored in the portal itself, a string addresses a folder on a connected third-party
    /// account, and both kinds may be sent in one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FolderIds { get; set; } = [];

    /// <summary>
    /// The files to pack as they are, by id, without conversion. A number addresses a file stored in the portal
    /// itself, a string addresses a file on a connected third-party account, and both kinds may be sent in one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FileIds { get; set; } = [];

    /// <summary>
    /// The files to convert before they are packed, each named together with the format it is converted to. A file
    /// listed here does not have to be repeated in `fileIds`.
    /// </summary>
    /// <example>[{"key": "1", "value": "pdf", "password": "password123"}]</example>
    public List<DownloadRequestItemDto> FileConvertIds { get; set; } = [];
}

/// <summary>
/// One file of a bulk download, together with the format it is converted to.
/// </summary>
public class DownloadRequestItemDto
{
    /// <summary>
    /// The file to convert and pack, by id — a number for a file stored in the portal itself, a string for a file on
    /// a connected third-party account.
    /// </summary>
    /// <example>1</example>
    public required JsonElement Key { get; init; }

    /// <summary>
    /// The format the file is converted to before it is packed, as a file extension without a leading dot.
    /// </summary>
    /// <example>pdf</example>
    public required string Value { get; init; }

    /// <summary>
    /// The password that opens the source file, for a file protected with one; a protected file cannot be converted
    /// without it.
    /// </summary>
    /// <example>password123</example>
    public string Password { get; init; }
}

/// <summary>
/// The files and folders to delete, and how final the deletion is.
/// </summary>
public class DeleteBatchRequestDto : FileOperationRequestBaseDto
{
    /// <summary>
    /// The folders to delete, by id, each with everything it contains. A number addresses a folder stored in the
    /// portal itself, a string addresses a folder on a connected third-party account, and both kinds may be sent in
    /// one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FolderIds { get; set; } = [];

    /// <summary>
    /// The files to delete, by id. A number addresses a file stored in the portal itself, a string addresses a file
    /// on a connected third-party account, and both kinds may be sent in one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FileIds { get; set; } = [];

    /// <summary>
    /// Whether the finished operation is still reported: `false` keeps its final record readable through
    /// `GET api/2.0/files/fileops` until it has been read once, `true` drops the record as soon as the work is done.
    /// It does not postpone the deletion and does not delete anything of its own.
    /// </summary>
    /// <example>false</example>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Where the deleted items go: `false` moves them to the Trash of the caller, from which they can be restored,
    /// `true` removes them at once and for good.
    /// </summary>
    /// <example>false</example>
    public bool Immediately { get; set; }
}

/// <summary>
/// The file whose versions are deleted, and the versions to delete.
/// </summary>
public class DeleteVersionBatchRequestDto : FileOperationRequestBaseDto
{
    /// <summary>
    /// Whether the finished operation is still reported: `false` keeps its final record readable through
    /// `GET api/2.0/files/fileops` until it has been read once, `true` drops the record as soon as the work is done.
    /// It does not postpone the deletion and does not delete anything of its own.
    /// </summary>
    /// <example>false</example>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// The file whose history the versions are taken from; only files stored in the portal itself are addressed here.
    /// </summary>
    /// <example>1</example>
    public required int FileId { get; set; }

    /// <summary>
    /// The version numbers to remove, as reported by `GET api/2.0/files/file/{fileId}/history`. At least one number
    /// has to be sent: an empty list removes the file itself instead of one of its versions. The number of the
    /// current version is refused outright, while a number that no longer exists is passed over without a complaint.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public required List<int> Versions { get; set; } = [];
}

/// <summary>
/// The parameters of a single file deletion.
/// </summary>
public class Delete
{
    /// <summary>
    /// When to delete: `true` waits until the editing session on the file has ended, `false` deletes at once, pulling
    /// the file away from whoever is working on it.
    /// </summary>
    /// <example>false</example>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// Where the file goes: `false` moves it to Trash, from where it can be restored, `true` deletes it for good.
    /// Inside a room, where there is no Trash, deletion is always final.
    /// </summary>
    /// <example>false</example>
    public bool Immediately { get; set; }
}

/// <summary>
/// The request that deletes one file.
/// </summary>
public class DeleteRequestDto<T> : FileOperationRequestBaseDto
{
    /// <summary>
    /// The file to delete.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "fileId")]
    public required T FileId { get; set; }

    /// <summary>
    /// When and how the file is deleted.
    /// </summary>
    /// <example>{"deleteAfter": false, "immediately": false}</example>
    [FromBody]
    public required Delete File { get; set; }
}

/// <summary>
/// The files and folders to move or copy, the folder they go to, and the way name clashes are settled.
/// </summary>
public class BatchRequestDto : FileOperationRequestBaseDto
{
    /// <summary>
    /// The folders to move or copy, by id. A number addresses a folder stored in the portal itself, a string
    /// addresses a folder on a connected third-party account, and both kinds may be sent in one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FolderIds { get; set; } = [];

    /// <summary>
    /// The files to move or copy, by id. A number addresses a file stored in the portal itself, a string addresses a
    /// file on a connected third-party account, and both kinds may be sent in one list.
    /// </summary>
    /// <example>[1, 2, 3]</example>
    public List<JsonElement> FileIds { get; set; } = [];

    /// <summary>
    /// The folder the items go to, by id — a number for a folder stored in the portal itself, a string for a folder
    /// on a connected third-party account. Take it from a folder listing such as `GET api/2.0/files/@root`; the
    /// caller has to be allowed to create items in it, and the id of a room addresses the root of that room.
    /// </summary>
    /// <example>1</example>
    public JsonElement DestFolderId { get; set; }

    /// <summary>
    /// What happens to an item whose name is already taken in the destination folder: `skip` leaves it where it is,
    /// `overwrite` replaces the entry at the destination, and `duplicate` places it beside that entry under a name
    /// with a numeric suffix. `GET api/2.0/files/fileops/move` reports which items would clash.
    /// </summary>
    /// <example>0</example>
    public FileConflictResolveType ConflictResolveType { get; set; }

    /// <summary>
    /// Whether the finished operation is still reported: `false` keeps its final record readable through
    /// `GET api/2.0/files/fileops` until it has been read once, `true` drops the record as soon as the work is done.
    /// It deletes nothing: a move takes the sources away in any case, and a copy always leaves them.
    /// </summary>
    /// <example>false</example>
    public bool DeleteAfter { get; set; }

    /// <summary>
    /// What is taken from a listed folder: `false` moves or copies the folder itself, `true` takes only what it
    /// contains, so its files and subfolders land in the destination and the folder is not recreated there.
    /// </summary>
    /// <example>false</example>
    public bool Content { get; set; }

    /// <summary>
    /// Marks every copied PDF form as a draft prepared for filling, which is how such a copy reports its filling
    /// status in a virtual data room. Files that are not forms are left unaffected.
    /// </summary>
    /// <example>false</example>
    public bool ToFillOut { get; set; }
}

/// <summary>
/// The part of the caller's Trash to empty.
/// </summary>
public class EmptyTrashRequestDto
{
    /// <summary>
    /// Which operations the answer carries: `true` returns the operation this call started and nothing else, `false`
    /// returns every delete operation that the caller has running or unread.
    /// </summary>
    /// <example>false</example>
    [FromQuery]
    public bool Single { get; set; }

    /// <summary>
    /// Limits the sweep to the items whose original location was inside a section or a room of one of the named
    /// types, leaving the rest of the Trash untouched; without the parameter the whole Trash is emptied. `5` covers
    /// what was deleted from personal documents, `14` what was deleted from rooms.
    /// </summary>
    /// <example>[5]</example>
    [FromQuery(Name = "folderType")]
    public List<FolderType> FolderType { get; set; }
}

/// <summary>
/// The operation to report on.
/// </summary>
public class FileOperationResultRequestBaseDto
{
    /// <summary>
    /// The operation to report on, as returned in `id` when it was started; without it every operation of the caller
    /// is reported. An id that is not among the caller's operations gives an empty answer rather than an error.
    /// </summary>
    /// <example>b2f3e9a4-7c15-4d8e-9f60-3a1c5e7d0b42</example>
    [FromQuery(Name = "id")]
    public string Id { get; set; }
}

/// <summary>
/// The kind of operation to report on.
/// </summary>
public class FileOperationResultRequestDto : FileOperationResultRequestBaseDto
{
    /// <summary>
    /// The kind of operation the answer is limited to. Only the kinds that have a queue of their own ever carry
    /// records — a copy, a deletion, a download, a mark-as-read and a duplication — and moves cannot be read through
    /// this route at all, because its address belongs to another operation.
    /// </summary>
    /// <example>2</example>
    [FromRoute(Name = "operationType")]
    public required FileOperationType OperationType { get; set; }
}