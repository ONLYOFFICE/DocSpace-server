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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The request parameters for handling the ID-based requests.
/// </summary>
public class IdRequestDto<T>
{
    /// <summary>
    /// The ID extracted from the route parameters.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }
}

/// <summary>
/// The parameters for handling the login event-related requests.
/// </summary>
public class LoginEvenrIdRequestDto
{
    /// <summary>
    /// The ID of the specific login event.
    /// </summary>
    /// <example>12345</example>
    [FromRoute(Name = "loginEventId")]
    public required int Id { get; set; }
}

/// <summary>
/// The parameters for handling the user-related requests using a GUID identifier.
/// </summary>
public class UserIdRequestDto
{
    /// <summary>
    /// The user ID extracted from the route parameters.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userId")]
    public required Guid Id { get; set; }
}

/// <summary>
/// The parameters for handling the user-related requests using a GUID identifier.
/// </summary>
public class UserIDRequestDto
{
    /// <summary>
    /// The user ID extracted from the route parameters.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userID")]
    public required Guid Id { get; set; }
}

/// <summary>
/// The requests for handling the product-related requests.
/// </summary>
public class ProductIdRequestDto
{
    /// <summary>
    /// The ID of the product extracted from the route parameters.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "productid")]
    public required Guid ProductId { get; set; }
}

/// <summary>
/// The parameters for handling requests that require both user and product identifiers.
/// </summary>
public class UserProductIdsRequestDto
{
    /// <summary>
    /// The ID of the product extracted from the query parameters.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "productid")]
    public required Guid ProductId { get; set; }

    /// <summary>
    /// The user ID extracted from the query parameters.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userid")]
    public required Guid UserId { get; set; }
}

/// <summary>
/// The request parameters for handling the migrator-related requests using a string identifier.
/// </summary>
public class MigratorNameRequestDto
{
    /// <summary>
    /// The migrator name extracted from the route parameters.
    /// </summary>
    /// <example>GoogleWorkspace</example>
    [FromRoute(Name = "migratorName")]
    public required string MigratorName { get; set; }
}