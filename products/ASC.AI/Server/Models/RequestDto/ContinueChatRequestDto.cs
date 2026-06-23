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

namespace ASC.AI.Models.RequestDto;

/// <summary>
/// Request to continue an existing AI chat session with a new message.
/// </summary>
public class ContinueChatRequestDto
{
    /// <summary>
    /// The unique identifier of the existing AI chat session to continue.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "chatId")]
    public required Guid ChatId { get; set; }

    /// <summary>
    /// The message and optional file attachments.
    /// </summary>
    /// <example>{"message": "Summarize this document for me", "contextFolderId": 123}</example>
    [FromBody]
    public required ContinueChatBody Body { get; set; }
}

/// <summary>
/// Parameters for continuing an AI chat session.
/// </summary>
public class ContinueChatBody
{
    /// <summary>
    /// The user message to append to the conversation.
    /// </summary>
    /// <example>Summarize this document for me</example>
    public required string Message { get; set; }

    /// <summary>
    /// The optional collection of file identifiers to attach as context for the AI model.
    /// </summary>
    /// <example>123</example>
    public int ContextFolderId { get; set; }
    /// <summary>The list of attached files.</summary>
    /// <example>[{"id": 1, "type": "file"}]</example>
    public IEnumerable<JsonElement>? Files { get; set; }
}