// (c) Copyright Ascensio System SIA 2009-2026
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