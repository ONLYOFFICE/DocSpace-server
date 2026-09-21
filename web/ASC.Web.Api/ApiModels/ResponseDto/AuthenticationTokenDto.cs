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

namespace ASC.Web.Api.ApiModel.ResponseDto;

/// <summary>
/// The outcome of a sign-in attempt: either the authentication token, or the second factor still to be passed.
/// </summary>
public class AuthenticationTokenDto
{
    /// <summary>
    /// The token to put in the `Authorization` header of later calls. It is empty whenever a second factor is
    /// still outstanding, which is what `sms` or `tfa` then says; the same token is also set as a portal cookie by
    /// the call that issued it, so a browser client does not have to carry it itself.
    /// </summary>
    /// <example>abcde12345</example>
    public string Token { get; set; }

    /// <summary>
    /// When the token stops being accepted. It stays at its zero value when `session=true` tied the token to the
    /// browser session instead of to a fixed moment. On the two operations that only send an SMS it carries a
    /// different meaning: there is no token, and this is the moment the code that was just sent expires.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime Expires { get; set; }

    /// <summary>
    /// Whether an SMS code is the second factor in play. Next to an empty `token` it means the code has to be sent
    /// to `POST api/2.0/authentication/{code}` before a token is issued; next to a filled `token` it means the
    /// code just accepted was an SMS one.
    /// </summary>
    /// <example>true</example>
    public bool Sms { get; set; }

    /// <summary>
    /// The stored phone number with its middle digits masked, filled in only while `sms` is set and a number is
    /// already activated for the user. It is there to be shown to the person signing in, not to be sent back.
    /// </summary>
    /// <example>+1***1234</example>
    public string PhoneNoise { get; set; }

    /// <summary>
    /// Whether an authenticator app is the second factor in play, with the same two readings as `sms`.
    /// </summary>
    /// <example>true</example>
    public bool Tfa { get; set; }

    /// <summary>
    /// The secret to enrol in an authenticator app, in the manual-entry form. It is filled in only while `tfa` is
    /// set and the app has not been connected yet, which is the one moment the secret is handed out; once the app
    /// is connected it stays empty. `GET api/2.0/settings/tfaapp/setup` returns the same secret with a QR code.
    /// </summary>
    /// <example>JBSWY3DPEHPK3PXP</example>
    public string TfaKey { get; set; }

    /// <summary>
    /// The confirmation link the client has to open to get past the second factor. It points at phone activation
    /// while no number is activated, at authenticator-app activation while the app is not connected, and at the
    /// plain code prompt once either is in place. It is empty in an answer that already carries a token.
    /// </summary>
    /// <example>https://example.com/confirm?token=abc123</example>
    [Url]
    public string ConfirmUrl { get; set; }
}