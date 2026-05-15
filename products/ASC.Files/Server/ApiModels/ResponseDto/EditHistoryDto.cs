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
/// The file editing history parameters.
/// </summary>
public class EditHistoryDto
{
    /// <summary>
    /// The document ID.
    /// </summary>
    /// <example>123</example>
    public int ID { get; set; }

    /// <summary>
    /// The document identifier used to unambiguously identify the document file.
    /// </summary>
    /// <example>doc-key-abc123</example>
    public string Key { get; set; }

    /// <summary>
    /// The document version number.
    /// </summary>
    /// <example>2</example>
    public int Version { get; set; }

    /// <summary>
    /// The document version group.
    /// </summary>
    /// <example>1</example>
    public int VersionGroup { get; set; }

    /// <summary>
    /// The user who updated a file.
    /// </summary>
    /// <example>{"id": "00000000-0000-0000-0000-000000000000", "name": "John Doe"}</example>
    public EditHistoryAuthor User { get; set; }

    /// <summary>
    /// The document version creation date.
    /// </summary>
    /// <example>2021-01-01T00:00:00.0000000Z</example>
    public ApiDateTime Created { get; set; }

    /// <summary>
    /// The file history changes in the string format.
    /// </summary>
    /// <example>Changes history text</example>
    public string ChangesHistory { get; set; }

    /// <summary>
    /// The list of file history changes.
    /// </summary>
    /// <example>[{"user": {"id": "123", "name": "John Doe"}, "created": "2021-01-01T00:00:00Z"}]</example>
    public List<EditHistoryChangesWrapper> Changes { get; set; }

    /// <summary>
    /// The current server version number.
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
/// The parameters of the file editing history.
/// </summary>
public class EditHistoryChangesWrapper(EditHistoryChanges historyChanges, ApiDateTimeHelper apiDateTimeHelper)
{
    /// <summary>
    /// The user who edited the file.
    /// </summary>
    public EditHistoryAuthor User { get; set; } = historyChanges.Author;

    /// <summary>
    /// The creation date and time of the file version.
    /// </summary>
    public ApiDateTime Created { get; set; } = apiDateTimeHelper.Get(historyChanges.Date);

    /// <summary>
    /// The document hash generated by the SHA-256 algorithm.
    /// </summary>
    /// <example>a1b2c3d4e5f6g7h8i9j0</example>
    public string DocumentSha256 { get; set; } = historyChanges.DocumentSha256;
}
