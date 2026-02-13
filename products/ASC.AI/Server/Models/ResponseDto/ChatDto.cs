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

namespace ASC.AI.Models.ResponseDto;

/// <summary>
/// The chat session information.
/// </summary>
public record ChatDto(Guid Id, string Title, ApiDateTime CreatedOn, ApiDateTime ModifiedOn, EmployeeDto CreatedBy)
{
    /// <summary>
    /// The chat session ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid Id { get; } = Id;

    /// <summary>
    /// The chat session title.
    /// </summary>
    /// <example>Project discussion</example>
    public string Title { get; } = Title;

    /// <summary>
    /// The date and time when the chat session was created.
    /// </summary>
    /// <example>2025-06-15T10:30:00.0000000Z</example>
    public ApiDateTime CreatedOn { get; } = CreatedOn;

    /// <summary>
    /// The date and time when the chat session was last modified.
    /// </summary>
    /// <example>2025-06-15T12:45:00.0000000Z</example>
    public ApiDateTime ModifiedOn { get; } = ModifiedOn;

    /// <summary>
    /// The user who created the chat session.
    /// </summary>
    public EmployeeDto CreatedBy { get; } = CreatedBy;
}

public static class ChatDtoExtensions
{
    public static async Task<ChatDto> ToDtoAsync(this ChatSession chatSession, EmployeeDtoHelper employeeDtoHelper,
        ApiDateTimeHelper dateTimeHelper)
    {
        var createdOn = dateTimeHelper.Get(chatSession.CreatedOn);
        var modifiedOn = dateTimeHelper.Get(chatSession.ModifiedOn);
        var employeeDto = await employeeDtoHelper.GetAsync(chatSession.UserId);

        return new ChatDto(chatSession.Id, chatSession.Title, createdOn, modifiedOn, employeeDto);
    }
}