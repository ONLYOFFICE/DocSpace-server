// (c) Copyright Ascensio System SIA 2009-2024
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
/// The generic ID request parameters.
/// </summary>
public class IdRequestDto<T>
{
    /// <summary>
    /// The ID.
    /// </summary>
    [FromRoute(Name = "id")]
    public T Id { get; set; }
}

/// <summary>
/// The login event request ID parameters.
/// </summary>
public class LoginEvenrIdRequestDto
{
    /// <summary>
    /// The login event ID.
    /// </summary>
    [FromRoute(Name = "loginEventId")]
    public int Id { get; set; }
}

/// <summary>
/// The user request ID parameters.
/// </summary>
public class UserIdRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    [FromRoute(Name = "userId")]
    public Guid Id { get; set; }
}

/// <summary>
/// The user request ID parameters.
/// </summary>
public class UserIDRequestDto
{
    /// <summary>
    /// The user ID.
    /// </summary>
    [FromRoute(Name = "userID")]
    public Guid Id { get; set; }
}

/// <summary>
/// The product ID request parameters.
/// </summary>
public class ProductIdRequestDto
{
    /// <summary>
    /// The product ID.
    /// </summary>
    [FromRoute(Name = "productid")]
    public Guid ProductId { get; set; }
}

/// <summary>
/// The user product ID request parameters.
/// </summary>
public class UserProductIdsRequestDto
{
    /// <summary>
    /// The product ID.
    /// </summary>
    [FromQuery(Name = "productid")]
    public Guid ProductId { get; set; }

    /// <summary>
    /// The user ID.
    /// </summary>
    [FromQuery(Name = "userid")]
    public Guid UserId { get; set; }
}

/// <summary>
/// The migrator name request parameters.
/// </summary>
public class MigratorNameRequestDto
{
    /// <summary>
    /// The migrator name.
    /// </summary>
    [FromRoute(Name = "migratorName")]
    public string MigratorName { get; set; }
}