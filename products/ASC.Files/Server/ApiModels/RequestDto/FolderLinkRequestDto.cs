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
/// The folder link parameters.
/// </summary>
public class FolderLinkRequest
{
    /// <summary>
    /// The folder link ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    public Guid LinkId { get; set; }

    /// <summary>
    /// The link sharing rights.
    /// </summary>
    /// <example>1</example>
    public FileShare Access { get; set; }

    /// <summary>
    /// The link expiration date.
    /// </summary>
    /// <example>2021-01-01T00:00:00Z</example>
    public ApiDateTime ExpirationDate { get; set; }

    /// <summary>
    /// The link name.
    /// </summary>
    /// <example>My Document</example>
    [StringLength(255)]
    public string Title { get; set; }

    /// <summary>
    /// The link password.
    /// </summary>
    /// <example>p@ssw0rd</example>
    [StringLength(255)]
    public string Password { get; set; }

    /// <summary>
    /// Specifies if downloading the file from the link is disabled or not.
    /// </summary>
    /// <example>false</example>
    public bool DenyDownload { get; set; }

    /// <summary>
    /// The link scope, whether it is internal or not.
    /// </summary>
    /// <example>false</example>
    public bool Internal { get; set; }

    /// <summary>
    /// Specifies whether the folder link is primary or not.
    /// </summary>
    /// <example>true</example>
    public bool Primary { get; set; }
}

/// <summary>
/// The request parameters for accessing the folder link.
/// </summary>
public class FolderLinkRequestDto<T>
{
    /// <summary>
    /// The folder ID.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }

    /// <summary>
    /// The folder link parameters.
    /// </summary>
    [FromBody]
    public required FolderLinkRequest FolderLink { get; set; }
}