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
/// The identifier of the object an operation addresses, taken from the route.
/// </summary>
public class IdRequestDto<T>
{
    /// <summary>
    /// The identifier of the object the operation acts on, as the listing operation of that kind of object reports
    /// it. It has to match the shape the route declares - a GUID where the route is typed as one - since a value of
    /// another shape does not match the route at all and is answered as not found.
    /// </summary>
    /// <example>1</example>
    [FromRoute(Name = "id")]
    public required T Id { get; set; }
}

/// <summary>
/// Which recorded sign-in an operation addresses.
/// </summary>
public class LoginEvenrIdRequestDto
{
    /// <summary>
    /// The sign-in to act on, by login event ID. Take it from the `id` of an item of
    /// `GET api/2.0/security/activeconnections`, which also marks the connection the caller is using, so a client
    /// can avoid picking its own.
    /// </summary>
    /// <example>12345</example>
    [FromRoute(Name = "loginEventId")]
    public required int Id { get; set; }
}

/// <summary>
/// Which portal account an operation addresses, by the `userId` route placeholder.
/// </summary>
public class UserIdRequestDto
{
    /// <summary>
    /// The portal account the operation acts on, by user ID as `GET api/2.0/people` reports it. Acting on an account
    /// other than the caller's own generally needs administrator rights.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userId")]
    public required Guid Id { get; set; }
}

/// <summary>
/// Which portal account an operation addresses, by the `userID` route placeholder.
/// </summary>
public class UserIDRequestDto
{
    /// <summary>
    /// The portal account the operation acts on, by user ID as `GET api/2.0/people` reports it. An ID belonging to
    /// no account of this portal and an ID of an internal system account are both answered as not found.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "userID")]
    public required Guid Id { get; set; }
}

/// <summary>
/// Which portal module an operation addresses.
/// </summary>
public class ProductIdRequestDto
{
    /// <summary>
    /// The module the operation acts on, by module GUID. The all-zero GUID stands for the portal itself rather than
    /// for a single module, and a GUID that names no module group is answered with an empty result instead of a
    /// failure.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromRoute(Name = "productid")]
    public required Guid ProductId { get; set; }
}

/// <summary>
/// The module and the account an operation is asked about together.
/// </summary>
public class UserProductIdsRequestDto
{
    /// <summary>
    /// The module being asked about, by module GUID. The all-zero GUID asks about the portal itself rather than a
    /// single module.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "productid")]
    public required Guid ProductId { get; set; }

    /// <summary>
    /// The account being asked about, by portal user ID. An ID that names no account is answered as a plain negative
    /// rather than a failure, so a negative answer does not prove the account exists.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000000</example>
    [FromQuery(Name = "userid")]
    public required Guid UserId { get; set; }
}

/// <summary>
/// Which import source the migration job reads its backup with.
/// </summary>
public class MigratorNameRequestDto
{
    /// <summary>
    /// The migrator that knows the format of the uploaded backup. It has to be one of the names
    /// `GET api/2.0/migration/list` reports for this installation, spelled exactly as listed.
    /// </summary>
    /// <example>GoogleWorkspace</example>
    [FromRoute(Name = "migratorName")]
    public required string MigratorName { get; set; }
}
