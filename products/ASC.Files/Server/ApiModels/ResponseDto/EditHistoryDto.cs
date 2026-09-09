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
/// One saved revision of a file, as the editing service recorded it.
/// </summary>
public class EditHistoryDto
{
    /// <summary>
    /// The file the revision belongs to; every entry of one history carries the same value.
    /// </summary>
    /// <example>123</example>
    public int ID { get; set; }

    /// <summary>
    /// The document key of this revision, which the editing service uses to tell the revisions of a file apart and to
    /// reuse the copy it has cached. Hand it back unchanged when asking the editor for this revision.
    /// </summary>
    /// <example>doc-key-abc123</example>
    public string Key { get; set; }

    /// <summary>
    /// The number of the revision, counting up from 1 in the order the revisions were saved. It is the value the
    /// operations that show the changes of a revision or restore it expect.
    /// </summary>
    /// <example>2</example>
    public int Version { get; set; }

    /// <summary>
    /// Groups the revisions written by one editing session: entries sharing this number were saved while the same
    /// session was open, which is how a client collapses a long list of revisions into the versions a person would
    /// recognise.
    /// </summary>
    /// <example>1</example>
    public int VersionGroup { get; set; }

    /// <summary>
    /// The account that saved the revision. A revision saved by an account that no longer exists, or through an
    /// anonymous link, is reported as a guest.
    /// </summary>
    /// <example>{"id": "00000000-0000-0000-0000-000000000000", "name": "John Doe"}</example>
    public EditHistoryAuthor User { get; set; }

    /// <summary>
    /// When the revision was saved, written with the offset of the portal's time zone rather than as plain UTC. The
    /// times of one history are consistent with each other, so order and display the revisions by them.
    /// </summary>
    /// <example>2021-01-01T00:00:00.0000000Z</example>
    public ApiDateTime Created { get; set; }

    /// <summary>
    /// The change record the editing service stored for this revision, as the raw JSON it was written in, and empty
    /// for a revision the portal has no record for - one uploaded as a whole file, for instance. `changes` is the
    /// same record already parsed.
    /// </summary>
    /// <example>Changes history text</example>
    public string ChangesHistory { get; set; }

    /// <summary>
    /// The single changes this revision introduced - who made each of them and when - taken from the stored change
    /// record. It comes back empty both for a revision whose changes were never recorded and for one whose record is
    /// in a format the portal no longer reads, so an empty list is not proof that nothing changed.
    /// </summary>
    /// <example>[{"user": {"id": "123", "name": "John Doe"}, "created": "2021-01-01T00:00:00Z"}]</example>
    public List<EditHistoryChangesWrapper> Changes { get; set; }

    /// <summary>
    /// The build of the editing service that wrote the change record of this revision, taken from the record itself;
    /// empty when the portal holds no record for the revision.
    /// </summary>
    /// <example>8.0.1</example>
    public string ServerVersion { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class EditHistoryMapper(ApiDateTimeHelper apiDateTimeHelper, UserManager userManager, DisplayUserSettingsHelper displayUserSettingsHelper)
{
    [UserMapping(Default = true)]
    public EditHistoryDto MapToDto(EditHistory editHistory)
    {
        var result = Map(editHistory);

        result.Changes = editHistory.Changes.Select(r => new EditHistoryChangesWrapper(r, apiDateTimeHelper)).ToList();
        result.ChangesHistory = editHistory.ChangesString;
        result.Created = apiDateTimeHelper.Get(editHistory.ModifiedOn);
        result.User = new EditHistoryAuthor(userManager, displayUserSettingsHelper) { Id = editHistory.ModifiedBy.ToString() };

        return result;
    }

    [MapperIgnoreTarget(nameof(EditHistoryDto.Changes))]
    [MapperIgnoreTarget(nameof(EditHistoryDto.ChangesHistory))]
    [MapperIgnoreTarget(nameof(EditHistoryDto.Created))]
    [MapperIgnoreTarget(nameof(EditHistoryDto.User))]
    private partial EditHistoryDto Map(EditHistory source);
}

/// <summary>
/// One single change inside a saved revision of a file.
/// </summary>
public class EditHistoryChangesWrapper(EditHistoryChanges historyChanges, ApiDateTimeHelper apiDateTimeHelper)
{
    /// <summary>
    /// The account that made this change, as the editing service reported it; an account it could not name is
    /// reported as a guest.
    /// </summary>
    public EditHistoryAuthor User { get; set; } = historyChanges.Author;

    /// <summary>
    /// When this change was made, written with the offset of the portal's time zone rather than as plain UTC.
    /// </summary>
    public ApiDateTime Created { get; set; } = apiDateTimeHelper.Get(historyChanges.Date);

    /// <summary>
    /// The SHA-256 hash of the document as it stood after this change, where the editing service recorded one, so
    /// that a client can check a stored copy against the change it claims to hold. Empty when the change record
    /// carries no hash.
    /// </summary>
    /// <example>9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08</example>
    public string DocumentSha256 { get; set; } = historyChanges.DocumentSha256;
}
