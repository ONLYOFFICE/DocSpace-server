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

namespace ASC.ApiSystem.Models;

/// <summary>
/// Request parameters for portal
/// </summary>
public class TenantModel : IModel
{
    /// <summary>
    /// Portal name
    /// </summary>
    public string PortalName { get; set; }

    /// <summary>
    /// Tenant id
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// Affiliate id
    /// </summary>
    [StringLength(255)]
    public string AffiliateId { get; set; }

    /// <summary>
    /// Partner id
    /// </summary>
    [StringLength(255)]
    public string PartnerId { get; set; }

    /// <summary>
    /// Campaign
    /// </summary>
    public string Campaign { get; set; }

    /// <summary>
    /// First name
    /// </summary>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// Industry
    /// </summary>
    public int Industry { get; set; }

    /// <summary>
    /// Language
    /// </summary>
    [StringLength(7)]
    public string Language { get; set; }


    /// <summary>
    /// Last name
    /// </summary>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// Module
    /// </summary>
    [StringLength(38)]
    public string Module { get; set; }

    /// <summary>
    /// Password
    /// </summary>
    //todo: delete after www update
    [StringLength(PasswordSettingsManager.MaxLength)]
    public string Password { get; set; }

    /// <summary>
    /// Password hash
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// Phone
    /// </summary>
    [StringLength(32)]
    public string Phone { get; set; }

    /// <summary>
    /// Recaptcha response
    /// </summary>
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// Recaptcha type
    /// </summary>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// Region
    /// </summary>
    [StringLength(20)]
    public string Region { get; set; }

    /// <summary>
    /// AWS region
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string AWSRegion { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public TenantStatus Status { get; set; }

    /// <summary>
    /// Skip welcome
    /// </summary>
    public bool SkipWelcome { get; set; }

    /// <summary>
    /// TimeZone name
    /// </summary>
    [StringLength(255)]
    public string TimeZoneName { get; set; }

    /// <summary>
    /// Spam
    /// </summary>
    public bool Spam { get; set; }

    /// <summary>
    /// Calls
    /// </summary>
    public bool Calls { get; set; }

    /// <summary>
    /// App key
    /// </summary>
    public string AppKey { get; set; }

    /// <summary>
    /// Limited access space
    /// </summary>
    public bool LimitedAccessSpace { get; set; }
}
