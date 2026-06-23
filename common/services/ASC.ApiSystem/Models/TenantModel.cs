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

namespace ASC.ApiSystem.Models;

/// <summary>
/// The request parameters for managing a portal.
/// </summary>
public class TenantModel : IModel
{
    /// <summary>
    /// The portal name.
    /// </summary>
    /// <example>myportal</example>
    public string PortalName { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    /// <example>1</example>
    public int? TenantId { get; set; }

    /// <summary>
    /// The affiliate ID.
    /// </summary>
    /// <example>af123456</example>
    [StringLength(255)]
    public string AffiliateId { get; set; }

    /// <summary>
    /// The partner ID.
    /// </summary>
    /// <example>partner123</example>
    [StringLength(255)]
    public string PartnerId { get; set; }

    /// <summary>
    /// The portal campaign.
    /// </summary>
    /// <example>personal</example>
    public string Campaign { get; set; }

    /// <summary>
    /// The first name of the portal owner.
    /// </summary>
    /// <example>John</example>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The email address of the portal owner.
    /// </summary>
    /// <example>john.doe@example.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The tenant industry.
    /// </summary>
    /// <example>0</example>
    public int Industry { get; set; }

    /// <summary>
    /// The portal language.
    /// </summary>
    /// <example>en-US</example>
    [StringLength(7)]
    public string Language { get; set; }


    /// <summary>
    /// The last name of the portal owner.
    /// </summary>
    /// <example>Doe</example>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// The name for the storage module to be configured.
    /// </summary>
    /// <example>files</example>
    [StringLength(38)]
    public string Module { get; set; }

    /// <summary>
    /// The password of the portal owner.
    /// </summary>
    /// <example>yourPassword1!</example>
    //todo: delete after www update
    [StringLength(PasswordSettingsManager.MaxLength)]
    public string Password { get; set; }

    /// <summary>
    /// The password hash.
    /// </summary>
    /// <example>VGhpcyBpcyBhIHRlc3Q=</example>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The phone numberr of the portal owner.
    /// </summary>
    /// <example>+15551234567</example>
    [StringLength(32)]
    public string Phone { get; set; }

    /// <summary>
    /// The user's response to the CAPTCHA challenge.
    /// </summary>
    /// <example>03AGdBq24rvY...</example>
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// The type of CAPTCHA validation used.
    /// </summary>
    /// <example>0</example>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The portal region.
    /// </summary>
    /// <example>us-east</example>
    [StringLength(20)]
    public string Region { get; set; }

    /// <summary>
    /// The portal AWS region.
    /// </summary>
    /// <example>us-east-1</example>
    [JsonPropertyName("awsRegion")]
    public string AWSRegion { get; set; }

    /// <summary>
    /// The tenant status.
    /// </summary>
    /// <example>0</example>
    public TenantStatus Status { get; set; }

    /// <summary>
    /// Specifies whether to send the welcome email to the user or not.
    /// </summary>
    /// <example>false</example>
    public bool SkipWelcome { get; set; }

    /// <summary>
    /// The portal time zone name.
    /// </summary>
    /// <example>UTC</example>
    [StringLength(255)]
    public string TimeZoneName { get; set; }

    /// <summary>
    /// Specifies if the ONLYOFFICE newsletter is allowed or not.
    /// </summary>
    /// <example>true</example>
    public bool Spam { get; set; }

    /// <summary>
    /// Specifies if the calls are available for the current tenant or not.
    /// </summary>
    /// <example>true</example>
    public bool Calls { get; set; }

    /// <summary>
    /// The application key.
    /// </summary>
    /// <example>app-key-123</example>
    public string AppKey { get; set; }

    /// <summary>
    /// Specifies whether the access to the space management is limited or not.
    /// </summary>
    /// <example>false</example>
    public bool LimitedAccessSpace { get; set; }

    /// <summary>
    /// The serialized third party profile.
    /// </summary>
    /// <example>{"id":"12345","provider":"google"}</example>
    public string ThirdPartyProfile { get; set; }
}