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
/// The mention notification to send: what to say, whom to tell and where in the document the mention sits.
/// </summary>
public class MentionMessageRequest
{
    /// <summary>
    /// The place in the document the notification link should open at, as the editor reports it when the mention is
    /// made. Left out, the link opens the file at its beginning.
    /// </summary>
    /// <example>{"action": {"data": "section-42", "type": "comment"}}</example>
    public ActionLinkRequest ActionLink { get; set; }

    /// <summary>
    /// The addresses to notify. Only an address that belongs to a portal account receives a mail; an unknown address
    /// is skipped, and the answer then carries the access list of the file so that the client can invite its owner.
    /// </summary>
    /// <example>["user1@example.com", "user2@example.com"]</example>
    public List<string> Emails { get; set; }

    /// <summary>
    /// The note shown next to the link in the mail. Only its first 200 characters are sent, and a value longer than
    /// the field allows is refused.
    /// </summary>
    /// <example>Please take a look at the second paragraph</example>
    [StringLength(255)]
    public string Message { get; set; }
}

/// <summary>
/// The request that names the file a mention was made in, and the notification to send.
/// </summary>
public class MentionMessageRequestDto<T>
{
    /// <summary>
    /// The file the mention was made in. A file stored on the portal is numbered, while a file in a connected
    /// third-party account is named by an opaque string.
    /// </summary>
    /// <example>10</example>
    [FromRoute(Name = "fileId")]
    public T FileId { get; set; }

    /// <summary>
    /// The notification to send.
    /// </summary>
    [FromBody]
    public MentionMessageRequest MentionMessage { get; set; }
}

/// <summary>
/// The place inside a document that a link should open at.
/// </summary>
public class ActionLinkRequest
{
    /// <summary>
    /// The anchor itself. It is passed on to the editor unchanged, so it has to be the value the editor produced for
    /// the comment or the mention it points at.
    /// </summary>
    /// <example>{"data": "section-42", "type": "comment"}</example>
    [JsonPropertyName("action")]
    public ActionLinkActionRequest Action { get; set; }
}

/// <summary>
/// An anchor inside a document, as the editor writes it.
/// </summary>
public class ActionLinkActionRequest
{
    /// <summary>
    /// The anchor value produced by the editor, opaque to the portal: it names the comment, the mention or the
    /// place the document is scrolled to.
    /// </summary>
    /// <example>section-42</example>
    [JsonPropertyName("data")]
    [StringLength(256)]
    public string Data { get; set; }

    /// <summary>
    /// What the anchor points at, as the editor names it - a comment thread, for instance.
    /// </summary>
    /// <example>comment</example>
    [JsonPropertyName("type")]
    [StringLength(128)]
    public string Type { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class ActionLinkRequestMapper
{
    public static partial ActionLinkConfig Map(this ActionLinkRequest source);
}
