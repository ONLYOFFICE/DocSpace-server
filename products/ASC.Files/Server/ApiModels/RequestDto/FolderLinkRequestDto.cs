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

namespace ASC.Files.ApiModels.RequestDto;

/// <summary>
/// The external link of a folder, as it is to be created or rewritten.
/// </summary>
public class FolderLinkRequest
{
    /// <summary>
    /// Which link the request addresses: the identifier of an existing link rewrites that link, while an identifier
    /// that is not in use, the empty one included, creates a new link. Take an existing identifier from
    /// `GET api/2.0/files/folder/{id}/links`.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid LinkId { get; set; }

    /// <summary>
    /// The rights a visitor following the link is given. The value that grants nothing revokes the link instead of
    /// setting it, and the answer is then empty.
    /// </summary>
    /// <example>1</example>
    public FileShare Access { get; set; }

    /// <summary>
    /// The moment the link stops working, sent as an ISO-8601 stamp. A moment that lies in the past is ignored,
    /// and leaving the field out gives the link no expiry.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// The name the link is listed under for the people who manage the folder; a visitor following it never sees the
    /// name.
    /// </summary>
    /// <example>Public link</example>
    [StringLength(255)]
    public string Title { get; set; }

    /// <summary>
    /// The secret a visitor has to enter before the link opens. Leave it out for a link that opens without one; the
    /// secret itself is never given back, only the fact that one is set.
    /// </summary>
    /// <example>p@ssw0rd</example>
    [StringLength(255)]
    public string Password { get; set; }

    /// <summary>
    /// Whether visitors are left with viewing alone: with true downloading and copying through the link are blocked,
    /// with false they are allowed.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// Whether the link admits signed-in portal members only: with true a visitor has to sign in before the link
    /// opens, with false anyone holding the address may follow it.
    /// </summary>
    /// <example>false</example>
    public bool Internal { get; set; }

    /// <summary>
    /// Whether this link becomes the primary link of the folder, the one the "Copy link" action of a client hands
    /// out; a folder has one primary link at a time.
    /// </summary>
    /// <example>true</example>
    public bool Primary { get; set; }
}

/// <summary>
/// The request that creates, rewrites or revokes an external link of one folder.
/// </summary>
public class FolderLinkRequestDto<T>
{
    /// <summary>
    /// The folder or room the link belongs to.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The link and the way it is to be shaped.
    /// </summary>
    /// <example>{"access": 1, "title": "Public link", "internal": false, "denyDownload": false}</example>
    [FromBody]
    public required FolderLinkRequest FolderLink { get; set; }
}