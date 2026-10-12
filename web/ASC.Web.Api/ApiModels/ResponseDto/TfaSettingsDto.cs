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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// One two-factor authentication method the portal offers, with the portal-wide state of that method.
/// </summary>
/// <example>
/// {
///   "id": "app",
///   "title": "Authenticator app",
///   "enabled": true,
///   "available": true,
///   "trustedIps": ["192.0.2.0/24"],
///   "mandatoryUsers": ["00000000-0000-0000-0000-000000000000"],
///   "mandatoryGroups": ["00000000-0000-0000-0000-000000000000"]
/// }
/// </example>
public class TfaSettingsDto
{
    /// <summary>
    /// Which method this entry describes: `sms` for a code sent by text message, `app` for a code from an
    /// authenticator application. It is the value `PUT api/2.0/settings/tfaapp` takes as its `type`, and no other
    /// value ever appears here.
    /// </summary>
    /// <example>app</example>
    public required string Id { get; set; }

    /// <summary>
    /// The label for the method in the portal language, meant for a button or a radio option. It is not stable
    /// enough to branch on - match `id` for that.
    /// </summary>
    /// <example>Authenticator app</example>
    public required string Title { get; set; }

    /// <summary>
    /// Whether this method is the portal's current policy. At most one entry can have it set, and none has it
    /// while the portal challenges nobody. It says nothing about the caller's own account, which may be exempt
    /// through `trustedIps` or forced through `mandatoryUsers`.
    /// </summary>
    /// <example>true</example>
    public required bool Enabled { get; set; }

    /// <summary>
    /// Whether the method could be switched on at all. For `sms` it is `false` until the installation has a
    /// working SMS provider, so a method can be offered here and still be impossible to enable; for `app` it is
    /// always `true`.
    /// </summary>
    /// <example>true</example>
    public required bool Available { get; set; }

    /// <summary>
    /// The addresses that skip the challenge, each either a single address, a `from-to` pair or a CIDR range. It
    /// is empty when no address is exempt, which means every account is challenged.
    /// </summary>
    /// <example>["192.0.2.0/24"]</example>
    public List<string> TrustedIps { get; set; }

    /// <summary>
    /// The accounts that are challenged even from a trusted address, by user ID. Empty means the exemption in
    /// `trustedIps` holds for everyone.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryUsers { get; set; }

    /// <summary>
    /// The groups whose members are challenged even from a trusted address, by group ID, with the same reading of
    /// an empty list as `mandatoryUsers`.
    /// </summary>
    /// <example>["00000000-0000-0000-0000-000000000000"]</example>
    public List<Guid> MandatoryGroups { get; set; }
}

/// <summary>
/// The confirmation link the caller has to follow to pass the two-factor step, and the cookie it depends on.
/// </summary>
/// <example>
/// {
///   "url": "https://example.com/confirm?type=TfaAuth&amp;key=abc123",
///   "cookieName": "asc_confirm_key_TfaAuth",
///   "cookieValue": "1234567890.abcdef"
/// }
/// </example>
public class TfaConfirmDataDto
{
    /// <summary>
    /// The link to open. Its `type` shows which step it is: phone activation or phone authorization for the SMS
    /// method, and authenticator activation or re-verification for the application method. The whole body is empty
    /// when the portal requires no second factor of the caller.
    /// </summary>
    /// <example>https://example.com/confirm?type=TfaAuth&amp;key=abc123</example>
    public string Url { get; set; }

    /// <summary>
    /// The name of the confirmation cookie the link is validated against. It is filled in only for the
    /// authenticator-application method; the SMS method returns `url` alone.
    /// </summary>
    /// <example>asc_confirm_key_TfaAuth</example>
    public string CookieName { get; set; }

    /// <summary>
    /// The value of that cookie. The call already set it on the response, so it is repeated here only for a client
    /// that does not keep cookies of its own; it is filled in under the same condition as `cookieName`, and a
    /// later call to this operation replaces it.
    /// </summary>
    /// <example>1234567890.abcdef</example>
    public string CookieValue { get; set; }
}

/// <summary>
/// One backup code of the caller's authenticator credential.
/// </summary>
/// <example>
/// {
///   "isUsed": true,
///   "code": "123456"
/// }
/// </example>
public class TfaAppCodeDto
{
    /// <summary>
    /// Whether the code has already been spent. A spent code is kept in the list but is no longer accepted, so
    /// count the entries where this is `false` to know how many fallbacks remain.
    /// </summary>
    /// <example>true</example>
    public bool IsUsed { get; set; }

    /// <summary>
    /// The code itself, in the form it is typed at sign-in - six characters with the default configuration. It is
    /// stored encrypted and decrypted for this answer, so this is the one place a caller can read it.
    /// </summary>
    /// <example>123456</example>
    public string Code { get; set; }
}
