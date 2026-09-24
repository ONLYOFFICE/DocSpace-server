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

namespace ASC.Web.Files.Services.WCFService;

/// <summary>
/// A user the editor may offer: to be mentioned in a comment, or to be picked when protecting a document.
/// </summary>
public class MentionWrapper
{
    internal MentionWrapper() { }

    /// <summary>
    /// The account itself, in the shape the people listings use.
    /// </summary>
    /// <example>{"id": "00000000-0000-0000-0000-000000000000", "firstName": "John", "lastName": "Doe"}</example>
    public UserInfo User { get; internal set; }

    /// <summary>
    /// Where a mention notification for this user is delivered.
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    public string Email { get; internal set; }

    /// <summary>
    /// The account id as text, the same value the account object carries; it is what identifies the user in a sharing
    /// request built from this list.
    /// </summary>
    /// <example>user_0001</example>
    public string Id { get; internal set; }

    /// <summary>
    /// An absolute address of the medium-sized avatar. A generated default avatar is reported when the user never
    /// uploaded one, so the field is never empty.
    /// </summary>
    /// <example>https://portal.example.com/avatar/user_0001.png</example>
    public string Image { get; internal set; }

    /// <summary>
    /// Not filled in by the operations that return this list: it always comes back false. Whether a user can already
    /// open the document has to be read from the sharing settings of the file.
    /// </summary>
    /// <example>true</example>
    public bool HasAccess { get; internal set; }

    /// <summary>
    /// The name to display, assembled the way the portal is configured to show names.
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; internal set; }
}

/// <summary>
/// The mention notification to send: what to say, whom to tell and where in the document the mention sits.
/// </summary>
public class MentionMessageWrapper
{
    /// <summary>
    /// The place in the document the notification link should open at, as the editor reports it when the mention is
    /// made. Left out, the link opens the file at its beginning.
    /// </summary>
    /// <example>{"action": {"data": "section-42", "type": "comment"}}</example>
    public ActionLinkConfig ActionLink { get; set; }

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
public class MentionMessageWrapperRequestDto<T>
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
    public MentionMessageWrapper MentionMessage { get; set; }
}