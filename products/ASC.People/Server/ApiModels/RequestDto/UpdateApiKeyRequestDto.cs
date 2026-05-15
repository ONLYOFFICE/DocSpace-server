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

namespace ASC.People.ApiModels.RequestDto;


/// <summary>
/// The request parameters for updating an existing API key.
/// </summary>
public class UpdateApiKeyRequest
{
    /// <summary>
    /// The new name for the API key.
    /// </summary>
    /// <example>Updated API Key</example>
    [StringLength(30, ErrorMessage = "Incorrect name. Length must be less than 30")]
    public string Name { get; set; }

    /// <summary>
    /// The new list of permissions for the API key.
    /// </summary>
    /// <example>["read", "write", "delete"]</example>
    public List<string> Permissions { get; set; }

    /// <summary>
    /// Indicates whether the API key should be active or not.
    /// </summary>
    /// <example>true</example>
    public bool? IsActive { get; set; }
}

/// <summary>
/// The request parameters for updating an existing API key.
/// </summary>
public class UpdateApiKeyRequestDto
{
    /// <summary>
    /// The unique identifier of the API key to update.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "keyId")]
    public required Guid KeyId { get; set; }

    /// <summary>
    /// The request parameters for updating an existing API key.
    /// </summary>
    /// <example>{"name":"Updated Key","permissions":["read"],"isActive":true}</example>
    [FromBody]
    public required UpdateApiKeyRequest Changed { get; set; }
}