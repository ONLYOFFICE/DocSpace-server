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
/// The settings of an external link to a file.
/// </summary>
public class FileLinkRequest
{
    /// <summary>
    /// The link to rewrite, as reported by `GET api/2.0/files/file/{id}/links`. An identifier that is not yet in use,
    /// the empty one included, creates a link instead.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid LinkId { get; set; }

    /// <summary>
    /// The rights the link grants to whoever follows it. The value that denies everything revokes the link.
    /// </summary>
    /// <example>1</example>
    public FileShare Access { get; set; }

    /// <summary>
    /// The moment the link stops working, read in the time zone of the portal. A date more than a few years ahead is
    /// rejected as an invalid request; left out, the link does not expire on its own.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// The name the link carries in the sharing list of the file, for the people who manage it; it is not shown to
    /// whoever follows the link.
    /// </summary>
    /// <example>My Document</example>
    [StringLength(255)]
    public string Title { get; set; }

    /// <summary>
    /// Who may follow the link: `true` admits only accounts that are signed in to the portal, `false` admits anybody
    /// who has the address.
    /// </summary>
    /// <example>false</example>
    public bool Internal { get; set; }

    /// <summary>
    /// Whether this link becomes the primary link of the file - the one the "Copy link" action of a client hands out.
    /// A file has one primary link at a time.
    /// </summary>
    /// <example>true</example>
    public bool Primary { get; set; }

    /// <summary>
    /// What a visitor may do with the content: `true` leaves them with viewing in the browser, `false` lets them
    /// download and print it as their rights allow.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The secret a visitor has to type before the file opens; left out, the link opens without one.
    /// </summary>
    /// <example>p@ssw0rd</example>
    [StringLength(255)]
    public string Password { get; set; }
}


/// <summary>
/// The request that creates, rewrites or revokes an external link to a file.
/// </summary>
public class FileLinkRequestDto<T>
{
    /// <summary>
    /// The file the link points at.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The settings of the link. They are applied in full, so a field left out is reset rather than kept.
    /// </summary>
    /// <example>
    /// {"linkId": "00000000-0000-0000-0000-000000000000", "access": 2, "title": "Review link",
    /// "internal": false, "primary": true, "denyDownload": false}
    /// </example>
    [FromBody]
    public required FileLinkRequest File { get; set; }
}