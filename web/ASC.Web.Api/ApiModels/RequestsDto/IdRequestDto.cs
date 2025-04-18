// (c) Copyright Ascensio System SIA 2009-2025
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The request parameters for handling the ID-based requests.
/// </summary>
public class IdRequestDto<T>
{
    /// <summary>
    /// The ID extracted from the route parameters.
    /// </summary>
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
    [FromQuery(Name = "productid")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// The user ID extracted from the query parameters.
    /// </summary>
    [FromQuery(Name = "userid")]
    public Guid UserId { get; set; }
}

/// <summary>
/// The request parameters for handling the migrator-related requests using a string identifier.
/// </summary>
public class MigratorNameRequestDto
{
    /// <summary>
    /// The migrator name extracted from the route parameters.
    /// </summary>
    [FromRoute(Name = "migratorName")]
    public required string MigratorName { get; set; }
}