﻿// (c) Copyright Ascensio System SIA 2009-2024
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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// Team template parameters
/// </summary>
public class SchemaBaseRequestsDto
{
    /// <summary>
    /// Team template ID
    /// </summary>
    public string Id { get; init; }
}

/// <summary>
/// Team template parameters
/// </summary>
public class SchemaRequestsDto : SchemaBaseRequestsDto
{
    /// <summary>
    /// Team template name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// User caption
    /// </summary>
    public string UserCaption { get; init; }

    /// <summary>
    /// Users caption
    /// </summary>
    public string UsersCaption { get; init; }

    /// <summary>
    /// Group caption
    /// </summary>
    public string GroupCaption { get; init; }

    /// <summary>
    /// Groups caption
    /// </summary>
    public string GroupsCaption { get; init; }

    /// <summary>
    /// User status caption
    /// </summary>
    public string UserPostCaption { get; init; }

    /// <summary>
    /// Registration date caption
    /// </summary>
    public string RegDateCaption { get; init; }

    /// <summary>
    /// Group lead caption
    /// </summary>
    public string GroupHeadCaption { get; init; }

    /// <summary>
    /// Guest caption
    /// </summary>
    public string GuestCaption { get; init; }

    /// <summary>
    /// Guests caption
    /// </summary>
    public string GuestsCaption { get; init; }
}
