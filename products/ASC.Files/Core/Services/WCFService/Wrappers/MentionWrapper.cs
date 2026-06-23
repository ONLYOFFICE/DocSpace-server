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
/// The parameters of a user mentioned in a message.
/// </summary>
public class MentionWrapper
{
    internal MentionWrapper() { }

    /// <summary>
    /// The user information.
    /// </summary>
    /// <example>{"id": "00000000-0000-0000-0000-000000000000", "firstName": "John", "lastName": "Doe"}</example>
    public UserInfo User { get; internal set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    public string Email { get; internal set; }

    /// <summary>
    /// The user unique identification.
    /// </summary>
    /// <example>user_0001</example>
    public string Id { get; internal set; }

    /// <summary>
    /// The path to the user's avatar.
    /// </summary>
    /// <example>https://portal.example.com/avatar/user_0001.png</example>
    public string Image { get; internal set; }

    /// <summary>
    /// Specifies whether the user has the access to the file where they are mentioned.
    /// </summary>
    /// <example>true</example>
    public bool HasAccess { get; internal set; }

    /// <summary>
    /// The user full name.
    /// </summary>
    /// <example>John Doe</example>
    public string Name { get; internal set; }
}

/// <summary>
/// The mention message parameters.
/// </summary>
public class MentionMessageWrapper
{
    /// <summary>
    /// The config parameter which contains the information about the action in the document that will be scrolled to.
    /// </summary>
    /// <example>{"action": {"data": "section-42", "type": "scroll"}}</example>
    public ActionLinkConfig ActionLink { get; set; }

    /// <summary>
    /// A list of emails that will receive the mention message.
    /// </summary>
    /// <example>["user1@example.com", "user2@example.com"]</example>
    public List<string> Emails { get; set; }

    /// <summary>
    /// The mention message.
    /// </summary>
    /// <example>Hello</example>
    [StringLength(255)]
    public string Message { get; set; }
}

/// <summary>
/// The request parameters for sending the mention message.
/// </summary>
public class MentionMessageWrapperRequestDto<T>
{
    /// <summary>
    /// The file ID with the mention message.
    /// </summary>
    /// <example>file-id</example>
    [FromRoute(Name = "fileId")]
    public T FileId { get; set; }

    /// <summary>
    /// The mention message.
    /// </summary>
    [FromBody]
    public MentionMessageWrapper MentionMessage { get; set; }
}