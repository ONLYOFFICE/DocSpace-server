// (c) Copyright Ascensio System SIA 2009-2025
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
/// The request parameters for managing a portal.
/// </summary>
public class TenantModel : IModel
{
    /// <summary>
    /// The portal name.
    /// </summary>
    public string PortalName { get; set; }

    /// <summary>
    /// The tenant ID.
    /// </summary>
    public int? TenantId { get; set; }

    /// <summary>
    /// The affiliate ID.
    /// </summary>
    [StringLength(255)]
    public string AffiliateId { get; set; }

    /// <summary>
    /// The partner ID.
    /// </summary>
    [StringLength(255)]
    public string PartnerId { get; set; }

    /// <summary>
    /// The portal campaign.
    /// </summary>
    public string Campaign { get; set; }

    /// <summary>
    /// The first name of the portal owner.
    /// </summary>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The email address of the portal owner.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The tenant industry.
    /// </summary>
    public int Industry { get; set; }

    /// <summary>
    /// The portal language.
    /// </summary>
    [StringLength(7)]
    public string Language { get; set; }


    /// <summary>
    /// The last name of the portal owner.
    /// </summary>
    [StringLength(255)]
    public string LastName { get; set; }

    /// <summary>
    /// The name for the storage module to be configured.
    /// </summary>
    [StringLength(38)]
    public string Module { get; set; }

    /// <summary>
    /// The password of the portal owner.
    /// </summary>
    //todo: delete after www update
    [StringLength(PasswordSettingsManager.MaxLength)]
    public string Password { get; set; }

    /// <summary>
    /// The password hash.
    /// </summary>
    public string PasswordHash { get; set; }

    /// <summary>
    /// The phone numberr of the portal owner.
    /// </summary>
    [StringLength(32)]
    public string Phone { get; set; }

    /// <summary>
    /// The user's response to the CAPTCHA challenge.
    /// </summary>
    public string RecaptchaResponse { get; set; }

    /// <summary>
    /// The type of CAPTCHA validation used.
    /// </summary>
    public RecaptchaType RecaptchaType { get; set; }

    /// <summary>
    /// The portal region.
    /// </summary>
    [StringLength(20)]
    public string Region { get; set; }

    /// <summary>
    /// The portal AWS region.
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string AWSRegion { get; set; }

    /// <summary>
    /// The tenant status.
    /// </summary>
    public TenantStatus Status { get; set; }

    /// <summary>
    /// Specifies whether to send the welcome email to the user or not.
    /// </summary>
    public bool SkipWelcome { get; set; }

    /// <summary>
    /// The portal time zone name.
    /// </summary>
    [StringLength(255)]
    public string TimeZoneName { get; set; }

    /// <summary>
    /// Specifies if the ONLYOFFICE newsletter is allowed or not.
    /// </summary>
    public bool Spam { get; set; }

    /// <summary>
    /// Specifies if the calls are available for the current tenant or not.
    /// </summary>
    public bool Calls { get; set; }

    /// <summary>
    /// The application key.
    /// </summary>
    public string AppKey { get; set; }

    /// <summary>
    /// Specifies whether the access to the space management is limited or not.
    /// </summary>
    public bool LimitedAccessSpace { get; set; }
}
