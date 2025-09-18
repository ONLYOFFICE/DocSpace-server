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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The request parameters for the team template identification.
/// </summary>
public class SchemaBaseRequestsDto
{
    /// <summary>
    /// The team template ID.
    /// </summary>
    public required string Id { get; init; }
}

/// <summary>
/// The request parameters for the comprehensive team template configuration.
/// </summary>
public class SchemaRequestsDto
{
    /// <summary>
    /// The team template ID.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The display name for the team template.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The label for the single user references.
    /// </summary>
    public string UserCaption { get; init; }

    /// <summary>
    /// The label for the multiple user references.
    /// </summary>
    public string UsersCaption { get; init; }

    /// <summary>
    /// The label for the single group references.
    /// </summary>
    public string GroupCaption { get; init; }

    /// <summary>
    /// The label for the multiple group references.
    /// </summary>
    public string GroupsCaption { get; init; }

    /// <summary>
    /// The label for the user position or status.
    /// </summary>
    public string UserPostCaption { get; init; }

    /// <summary>
    /// The label for the member registration date.
    /// </summary>
    public string RegDateCaption { get; init; }

    /// <summary>
    /// The label for the group leader position.
    /// </summary>
    public string GroupHeadCaption { get; init; }

    /// <summary>
    /// The label for the single guest/external user references.
    /// </summary>
    public string GuestCaption { get; init; }

    /// <summary>
    /// The label for the multiple guest/external user references.
    /// </summary>
    public string GuestsCaption { get; init; }
}
