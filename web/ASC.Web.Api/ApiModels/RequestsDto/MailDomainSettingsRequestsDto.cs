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
/// Which email domains the portal treats as already verified, and how their users join.
/// </summary>
public class MailDomainSettingsRequestsDto
{
    /// <summary>
    /// How trusted domains are decided: no domain is trusted, every domain is, or only the ones listed in `domains`.
    /// Only the custom mode reads `domains`; under the other two the list is ignored rather than refused.
    /// </summary>
    /// <example>All</example>
    public required TenantTrustedDomainsType Type { get; set; }

    /// <summary>
    /// The trusted domains, as bare hostnames such as `example.com` without a scheme or an `@`. This is the whole
    /// list that is to hold afterwards and not a list of additions. Each entry is lowercased before it is stored,
    /// and one entry that is not a valid hostname - or an empty list in the custom mode - fails the whole call
    /// without saving anything.
    /// </summary>
    /// <example>["example.com", "company.com"]</example>
    public required List<string> Domains { get; set; }

    /// <summary>
    /// What a user joining through a trusted domain becomes: `true` admits them as a guest, `false` as a full
    /// member. It applies to joins made from now on and does not change anybody who has already joined.
    /// </summary>
    /// <example>false</example>
    public required bool InviteUsersAsVisitors { get; set; }
}

/// <summary>
/// Who is invited to join the portal, and in which language the invitation is written.
/// </summary>
public class AdminMessageBaseSettingsRequestsDto
{
    /// <summary>
    /// The address the join link is sent to. It has to be a well-formed ASCII address rather than an
    /// internationalized one, must not already belong to a member of the portal, and, where the portal trusts named
    /// domains only, has to end with one of them; any of these faults is refused with 400.
    /// </summary>
    /// <example>admin@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; set; }

    /// <summary>
    /// The language the letter is written in, as a culture name such as `en-US`. A culture the installation does not
    /// have falls back to the portal language rather than failing the call.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }
}

/// <summary>
/// The message sent to the portal administrators, with the CAPTCHA proof that a person wrote it.
/// </summary>
public class AdminMessageSettingsRequestsDto
{
    /// <summary>
    /// What the sender wants to tell the portal administrators. Markup is stripped before the letter is written, so
    /// a body that carries nothing but markup counts as empty and is refused with 400.
    /// </summary>
    /// <example>Hello, this is a test message from the administrator.</example>
    [StringLength(255)]
    public required string Message { get; set; }

    /// <summary>
    /// The address the sender can be answered at, which the letter is signed with. It has to be a well-formed email
    /// address.
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; set; }

    /// <summary>
    /// The language the letter is written in, as a culture name such as `en-US`. A culture the installation does not
    /// have falls back to the portal language rather than failing the call.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }

    /// <summary>
    /// Which CAPTCHA service the proof in `recaptchaResponse` came from. It has to match the service the
    /// installation is configured with, which `GET api/2.0/capabilities` reports; the default value means the
    /// installation is left to decide.
    /// </summary>
    /// <example>Default</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The token the CAPTCHA widget produced in the browser, passed on unchanged for the portal to verify with the
    /// CAPTCHA service. It is single-use and short-lived, so it cannot be reused for a second message.
    /// </summary>
    /// <example>03AGdBq24PBCbwiDRaS...</example>
    public string RecaptchaResponse { get; set; }
}

/// <summary>
/// Whether the sign-in page offers the form for writing to the portal administrators.
/// </summary>
public class TurnOnAdminMessageSettingsRequestDto
{
    /// <summary>
    /// Whether the form is offered. Switching it off hides the form for everybody and makes the operation that
    /// submits it refuse new messages; letters already sent are untouched.
    /// </summary>
    /// <example>true</example>
    public bool TurnOn { get; set; }
}
