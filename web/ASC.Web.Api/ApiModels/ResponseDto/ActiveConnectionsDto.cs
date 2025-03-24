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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The active connections parameters.
/// </summary>
public class ActiveConnectionsDto
{
    /// <summary>
    /// The login event.
    /// </summary>
    public int LoginEvent { get; set; }

    /// <summary>
    /// The active connections items.
    /// </summary>
    public List<ActiveConnectionsItemDto> Items { get; set; }
}

/// <summary>
/// The active connections item parameters.
/// </summary>
public class ActiveConnectionsItemDto
{
    /// <summary>
    /// The active connections item ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The active connections item tenant ID.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// The active connections item user ID.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Specifies if the item has mobile or not.
    /// </summary>
    public bool Mobile {  get; set; }

    /// <summary>
    /// The Ip address of the active connections item.
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// The country of the active connections item.
    /// </summary>
    public string Country { get; set; }

    /// <summary>
    /// The city of the active connections item.
    /// </summary>
    public string City { get; set; }

    /// <summary>
    /// The browser of the active connections item.
    /// </summary>
    public string Browser { get; set; }

    /// <summary>
    /// The platform of the active connections item.
    /// </summary>
    public string Platform { get; set; }

    /// <summary>
    /// The date of the active connections item.
    /// </summary>
    public ApiDateTime Date { get; set; }

    /// <summary>
    /// The page of the active connections item.
    /// </summary>
    public string Page { get; set; }
}