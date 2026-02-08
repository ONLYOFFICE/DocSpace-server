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
/// The request parameters for linking accounts.
/// </summary>
public class LinkAccountRequestDto
{
    /// <summary>
    /// The third-party profile in the serialized format.
    /// </summary>
    /// <example>{"provider":"Google","id":"123456"}</example>
    public string SerializedProfile { get; set; }
}

/// <summary>
/// The request parameters for creating a third-party account.
/// </summary>
public class SignupAccountRequestDto
{
    /// <summary>
    /// The user type.
    /// </summary>
    /// <example>1</example>
    public EmployeeType? EmployeeType { get; set; }

    /// <summary>
    /// The user first name.
    /// </summary>
    /// <example>John</example>
    public string FirstName { get; set; }

    /// <summary>
    /// The user last name.
    /// </summary>
    /// <example>Doe</example>
    public string LastName { get; set; }

    /// <summary>
    /// The user email address.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// The user password hash.
    /// </summary>
    /// <example>$2a$10$abcdefghijklmnopqrstuv</example>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The user link key.
    /// </summary>
    /// <example>invite_key_123456</example>
    public required string Key { get; set; }

    /// <summary>
    /// The user culture code.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }

    /// <summary>
    /// The third-party profile in the serialized format
    /// </summary>
    /// <example>{"provider":"Google","id":"123456"}</example>
    public required string SerializedProfile { get; set; }
}