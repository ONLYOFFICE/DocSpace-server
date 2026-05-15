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
/// The request parameters for configuring trusted mail domains and visitor invitation settings.
/// </summary>
public class MailDomainSettingsRequestsDto
{
    /// <summary>
    /// Defines how trusted domains are handled and validated.
    /// </summary>
    /// <example>All</example>
    public required TenantTrustedDomainsType Type { get; set; }

    /// <summary>
    /// The list of authorized email domains that are considered trusted.
    /// </summary>
    /// <example>["example.com", "company.com"]</example>
    public required List<string> Domains { get; set; }

    /// <summary>
    /// Specifies the default permission level for the invited users (visitors or not).
    /// </summary>
    /// <example>false</example>
    public required bool InviteUsersAsVisitors { get; set; }
}

/// <summary>
/// The request parameters for the administrator message configuration.
/// </summary>
public class AdminMessageBaseSettingsRequestsDto
{
    /// <summary>
    /// The email address used for sending administrator messages.
    /// </summary>
    /// <example>admin@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; set; }

    /// <summary>
    /// The locale identifier for message localization.
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }
}

/// <summary>
/// The request parameters for configuring the administrator message content.
/// </summary>
public class AdminMessageSettingsRequestsDto
{
    /// <summary>
    /// The content of the administrator message to be sent.
    /// </summary>
    /// <example>Hello, this is a test message from the administrator.</example>
    [StringLength(255)]
    public required string Message { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    /// <example>user@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; set; }

    /// <summary>
    /// Culture
    /// </summary>
    /// <example>en-US</example>
    public string Culture { get; set; }

    /// <summary>
    /// The type of CAPTCHA validation used.
    /// </summary>
    /// <example>Default</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The user's response to the CAPTCHA challenge.
    /// </summary>
    /// <example>03AGdBq24PBCbwiDRaS...</example>
    public string RecaptchaResponse { get; set; }
}

/// <summary>
/// The request parameters for enabling or disabling administrator messaging system.
/// </summary>
public class TurnOnAdminMessageSettingsRequestDto
{
    /// <summary>
    /// The global switch for the administrator messaging functionality.
    /// </summary>
    /// <example>true</example>
    public bool TurnOn { get; set; }
}