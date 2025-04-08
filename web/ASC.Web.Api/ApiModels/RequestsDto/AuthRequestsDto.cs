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
/// The parameters required for the user authentication requests.
/// </summary>
public class AuthRequestsDto
{
    /// <summary>
    /// The username or email used for authentication.
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// The password in plain text for user authentication.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// The hashed password for secure verification.
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The type of authentication provider (e.g., internal, Google, Azure).
    /// </summary>
    public string Provider { get; set; }

    /// <summary>
    /// The access token used for authentication with external providers.
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// The serialized user profile data, if applicable.
    /// </summary>
    public string SerializedProfile { get; set; }

    /// <summary>
    /// The code for two-factor authentication.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// The authorization code used for obtaining OAuth tokens.
    /// </summary>
    public string CodeOAuth { get; set; }

    /// <summary>
    /// Specifies whether the authentication is session-based.
    /// </summary>
    public bool Session { get; set; }

    /// <summary>
    /// The additional confirmation data required for authentication.
    /// </summary>
    public ConfirmData ConfirmData { get; set; }

    /// <summary>
    /// The type of CAPTCHA validation used.
    /// </summary>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The user's response to the CAPTCHA challenge.
    /// </summary>
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// The culture code for localization during authentication.
    /// </summary>
    public string Culture { get; set; }
}

/// <summary>
/// The parameters required for the mobile phone verification.
/// </summary>
public class MobileRequestsDto
{
    /// <summary>
    /// The user's mobile phone number.
    /// </summary>
    public string MobilePhone { get; set; }
}

/// <summary>
/// The additional confirmation data required for authentication.
/// </summary>
public class ConfirmData
{
    /// <summary>
    /// The email address to confirm the user's identity.
    /// </summary>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// Specifies whether this is the first access to the user's account.
    /// </summary>
    public bool? First { get; set; }

    /// <summary>
    /// The unique confirmation key for validating user identity.
    /// </summary>
    public string Key { get; set; }
}
