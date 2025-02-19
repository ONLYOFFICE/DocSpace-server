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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// Authentication request parameters
/// </summary>
public class AuthRequestsDto
{
    /// <summary>
    /// Username / email
    /// </summary>
    [OpenApiDescription("Username / email")]
    public string UserName { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    [OpenApiDescription("Password")]
    public string Password { get; set; }

    /// <summary>
    /// Password hash
    /// </summary>
    [OpenApiDescription("Password hash")]
    public string PasswordHash { get; set; }

    /// <summary>
    /// Provider type
    /// </summary>
    [OpenApiDescription("Provider type")]
    public string Provider { get; set; }

    /// <summary>
    /// Provider access token
    /// </summary>
    [OpenApiDescription("Provider access token")]
    public string AccessToken { get; set; }

    /// <summary>
    /// Serialized user profile
    /// </summary>
    [OpenApiDescription("Serialized user profile")]
    public string SerializedProfile { get; set; }

    /// <summary>
    /// Two-factor authentication code
    /// </summary>
    [OpenApiDescription("Two-factor authentication code")]
    public string Code { get; set; }

    /// <summary>
    /// Code for getting a token
    /// </summary>
    [OpenApiDescription("Code for getting a token")]
    public string CodeOAuth { get; set; }

    /// <summary>
    /// Session based authentication or not
    /// </summary>
    [OpenApiDescription("Session based authentication or not")]
    public bool Session { get; set; }

    /// <summary>
    /// Confirmation data
    /// </summary>
    [OpenApiDescription("Confirmation data")]
    public ConfirmData ConfirmData { get; set; }

    /// <summary>
    /// Type of captcha
    /// </summary>
    [OpenApiDescription("Type of captcha")]
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// reCAPTCHA response
    /// </summary>
    [OpenApiDescription("reCAPTCHA response")]
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// Culture
    /// </summary>
    [OpenApiDescription("Culture")]
    public string Culture { get; set; }
}

/// <summary>
/// Mobile phone request parameters
/// </summary>
public class MobileRequestsDto
{
    /// <summary>
    /// Mobile phone
    /// </summary>
    [OpenApiDescription("Mobile phone")]
    public string MobilePhone { get; set; }
}

public class ConfirmData
{
    /// <summary>
    /// Email address
    /// </summary>
    [EmailAddress]
    [OpenApiDescription("Email address")]
    public string Email { get; set; }

    /// <summary>
    /// Access an account for the first time or not
    /// </summary>
    [OpenApiDescription("Access an account for the first time or not")]
    public bool? First { get; set; }

    /// <summary>
    /// Key
    /// </summary>
    [OpenApiDescription("Key")]
    public string Key { get; set; }
}
