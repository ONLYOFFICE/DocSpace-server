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
    /// The account itself. Service-side only: the API publishes it as <c>MentionDto</c> with an <c>EmployeeDto</c> in
    /// its place, so none of the account's private fields leave the server.
    /// </summary>
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

