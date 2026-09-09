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
/// The same credentials as an ordinary sign-in, plus the one-time code that completes it.
/// </summary>
public class AuthWithCodeRequestsDto: AuthRequestsDto
{
    /// <summary>
    /// The one-time code from the SMS the portal sent or from the authenticator app, whichever second factor the
    /// portal has enabled for this user. It is single-use and expires; a wrong, empty or expired value fails the
    /// sign-in and counts against the brute-force limit.
    /// </summary>
    /// <example>123456</example>
    public string Code { get; set; }
}

/// <summary>
/// The credentials a sign-in is attempted with: a portal password, a confirmation key, or a third-party account.
/// </summary>

public class AuthRequestsDto
{
    /// <summary>
    /// The account signing in, given as its email address or its portal user name. It is required for a password
    /// sign-in and ignored when the credentials are a confirmation key or a third-party account.
    /// </summary>
    /// <example>user@example.com</example>
    public string UserName { get; set; }

    /// <summary>
    /// The password in the clear. Send either this or `passwordHash`, never both; hashing it in the client with the
    /// parameters from `GET api/2.0/settings?withpassword=true` and sending `passwordHash` instead keeps the plain
    /// password off the wire.
    /// </summary>
    /// <example>SecurePassword123!</example>
    public string Password { get; set; }

    /// <summary>
    /// The password already hashed in the client. It has to be produced with the `salt`, iteration count and hash
    /// size that `GET api/2.0/settings?withpassword=true` publishes, or the portal cannot recognise it; a value sent
    /// here takes the place of `password`.
    /// </summary>
    /// <example>5f4dcc3b5aa765d61d8327deb882cf99</example>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The third-party identity provider the account is being signed in through, by its internal key such as
    /// `google` or `linkedin`. Sending it switches the call to a third-party sign-in, which needs `accessToken` or
    /// `serializedProfile` and is only allowed on a self-hosted installation or a tariff that includes third-party
    /// sign-in.
    /// </summary>
    /// <example>google</example>
    public string Provider { get; set; }

    /// <summary>
    /// The access token the provider named in `provider` issued for the account, passed on unchanged for the portal
    /// to verify with that provider. The portal then matches the address it gets back against its own accounts, so a
    /// valid token for an address unknown here is answered as no such user.
    /// </summary>
    /// <example>ya29.a0AfH6SMBx...</example>
    public string AccessToken { get; set; }

    /// <summary>
    /// The third-party profile already fetched and serialised by the caller, as an alternative to `accessToken` for
    /// a provider whose profile the client holds. It identifies the account by the address it carries.
    /// </summary>
    /// <example>{"name":"John Doe","email":"john@example.com"}</example>
    public string SerializedProfile { get; set; }

    /// <summary>
    /// The OAuth authorization code obtained from the provider, for a flow that has not been exchanged for an access
    /// token yet. It is recorded with the sign-in rather than replacing `accessToken`.
    /// </summary>
    /// <example>4/0AY0e-g7...</example>
    public string CodeOAuth { get; set; }

    /// <summary>
    /// Whether the issued token is tied to the browser session. When it is, the answer carries no `expires` and the
    /// token dies with the session; otherwise it lives for the portal session lifetime.
    /// </summary>
    /// <example>true</example>
    public bool Session { get; set; }

    /// <summary>
    /// The confirmation link data, as a third way to identify the account beside a password and a third-party
    /// account. Send it when the sign-in comes from a link the portal mailed, in which case `userName` and the
    /// password fields are not read.
    /// </summary>
    /// <example>{"email": "user@example.com", "key": "abc123def456", "first": true}</example>
    public ConfirmData ConfirmData { get; set; }

    /// <summary>
    /// Which CAPTCHA service the proof in `recaptchaResponse` came from. It has to match the service the
    /// installation is configured with, which `GET api/2.0/settings` publishes together with the site key.
    /// </summary>
    /// <example>GoogleRecaptchaV2</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The token the CAPTCHA widget produced in the browser, passed on unchanged for the portal to verify. It is
    /// only demanded once repeated failures have made the portal ask for a challenge, and it is single-use, so a
    /// retry needs a freshly solved one.
    /// </summary>
    /// <example>03AGdBq25...</example>
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// The language the sign-in messages and any letter that follows are written in, as a culture name such as
    /// `en-US`. A culture the installation does not have falls back to the portal language.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }
}

/// <summary>
/// The phone number a user going through phone activation registers for SMS codes.
/// </summary>
/// <example>
/// {
///   "mobilePhone": "+1234567890"
/// }
/// </example>
public class MobileRequestsDto
{
    /// <summary>
    /// The number the SMS codes are sent to, in international form with the leading `+` and no spaces. It is stored
    /// as not yet activated and only becomes the confirmed number once a code sent to it is accepted; an already
    /// activated number is not replaced this way and has to be erased first.
    /// </summary>
    /// <example>+1234567890</example>
    public string MobilePhone { get; set; }
}

/// <summary>
/// The confirmation link a sign-in is authorised with, in place of a password.
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
    /// The address the confirmation link was issued for. It has to be the same address the key was signed with, and
    /// a value that is not an email address fails the request with 400.
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    public string Email { get; set; }

    /// <summary>
    /// Whether the link is being followed for the first time, taken from the `first` parameter of the confirmation
    /// URL. It is part of what the key was signed over, so passing a different value invalidates the key rather than
    /// changing behaviour.
    /// </summary>
    /// <example>true</example>
    public bool? First { get; set; }

    /// <summary>
    /// The `key` parameter of the confirmation URL, copied verbatim. It is bound to the address and to the moment it
    /// was issued, so it stops being accepted once the portal email key lifetime has passed.
    /// </summary>
    /// <example>abc123def456</example>
    public string Key { get; set; }
}
