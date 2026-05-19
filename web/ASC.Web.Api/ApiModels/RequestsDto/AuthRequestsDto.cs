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

namespace ASC.Web.Api.ApiModel.RequestsDto;

/// <summary>
/// The parameters required for the user two-factor authentication requests.
/// </summary>
public class AuthWithCodeRequestsDto: AuthRequestsDto
{
    /// <summary>
    /// The code for two-factor authentication.
    /// </summary>
    /// <example>123456</example>
    public string Code { get; set; }
}

/// <summary>
/// The parameters required for the user authentication requests.
/// </summary>

public class AuthRequestsDto
{
    /// <summary>
    /// The username or email used for authentication.
    /// </summary>
    /// <example>user@example.com</example>
    public string UserName { get; set; }

    /// <summary>
    /// The password in plain text for user authentication.
    /// </summary>
    /// <example>SecurePassword123!</example>
    public string Password { get; set; }

    /// <summary>
    /// The hashed password for secure verification.
    /// </summary>
    /// <example>5f4dcc3b5aa765d61d8327deb882cf99</example>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The type of authentication provider (e.g., internal, Google, Azure).
    /// </summary>
    /// <example>google</example>
    public string Provider { get; set; }

    /// <summary>
    /// The access token used for authentication with external providers.
    /// </summary>
    /// <example>ya29.a0AfH6SMBx...</example>
    public string AccessToken { get; set; }

    /// <summary>
    /// The serialized user profile data, if applicable.
    /// </summary>
    /// <example>{"name":"John Doe","email":"john@example.com"}</example>
    public string SerializedProfile { get; set; }

    /// <summary>
    /// The authorization code used for obtaining OAuth tokens.
    /// </summary>
    /// <example>4/0AY0e-g7...</example>
    public string CodeOAuth { get; set; }

    /// <summary>
    /// Specifies whether the authentication is session-based.
    /// </summary>
    /// <example>true</example>
    public bool Session { get; set; }

    /// <summary>
    /// The additional confirmation data required for authentication.
    /// </summary>
    /// <example>{"email": "user@example.com", "key": "abc123def456", "first": true}</example>
    public ConfirmData ConfirmData { get; set; }

    /// <summary>
    /// The type of CAPTCHA validation used.
    /// </summary>
    /// <example>GoogleRecaptchaV2</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The user's response to the CAPTCHA challenge.
    /// </summary>
    /// <example>03AGdBq25...</example>
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// The culture code for localization during authentication.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }
}

/// <summary>
/// The parameters required for the mobile phone verification.
/// </summary>
/// <example>
/// {
///   "mobilePhone": "+1234567890"
/// }
/// </example>
public class MobileRequestsDto
{
    /// <summary>
    /// The user's mobile phone number.
    /// </summary>
    /// <example>+1234567890</example>
    public string MobilePhone { get; set; }
}

/// <summary>
/// The additional confirmation data required for authentication.
/// </summary>
/// <example>
/// {
///   "email": "user@example.com",
///   "key": "abc123def456",
///   "first": true
/// }
/// </example>
public class ConfirmData
{
    /// <summary>
    /// The email address to confirm the user's identity.
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// Specifies whether this is the first access to the user's account.
    /// </summary>
    /// <example>true</example>
    public bool? First { get; set; }

    /// <summary>
    /// The unique confirmation key for validating user identity.
    /// </summary>
    /// <example>abc123def456</example>
    public string Key { get; set; }
}