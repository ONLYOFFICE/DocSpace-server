// (c) Copyright Ascensio System SIA 2009-2026
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
