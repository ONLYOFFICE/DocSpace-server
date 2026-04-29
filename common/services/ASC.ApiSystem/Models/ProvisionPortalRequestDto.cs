// (c) Copyright Ascensio System SIA 2009-2026
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
/// The request parameters for provisioning a portal with an OAuth provider.
/// </summary>
public class ProvisionPortalRequestDto
{
    /// <summary>
    /// The email address of the portal owner.
    /// </summary>
    /// <example>admin@example.com</example>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The first name of the portal owner.
    /// </summary>
    /// <example>John</example>
    [StringLength(255)]
    public string FirstName { get; set; }

    /// <summary>
    /// The last name of the portal owner.
    /// </summary>
    /// <example>Doe</example>
    [StringLength(255)]
    public string LastName { get; set; }

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
    /// The application key.
    /// </summary>
    /// <example>app-key-123</example>
    public string AppKey { get; set; }

    /// <summary>
    /// The OAuth provider configuration.
    /// </summary>
    /// <example>{"name":"provider_name","clientId":"abc123","clientSecret":"secret"}</example>
    [Required]
    public ProvisionProviderDto Provider { get; set; }
}

/// <summary>
/// The OAuth provider configuration for portal provisioning.
/// </summary>
public class ProvisionProviderDto
{
    /// <summary>
    /// The provider name.
    /// </summary>
    /// <example>provider_name</example>
    [Required]
    [StringLength(64)]
    public string Name { get; set; }

    /// <summary>
    /// The OAuth client ID.
    /// </summary>
    /// <example>abc123</example>
    [Required]
    public string ClientId { get; set; }

    /// <summary>
    /// The OAuth client secret.
    /// </summary>
    /// <example>secret</example>
    [Required]
    public string ClientSecret { get; set; }

    /// <summary>
    /// The base URL of the provider instance.
    /// </summary>
    /// <example>https://provider.example.com</example>
    public string BaseUrl { get; set; }

    /// <summary>
    /// The OAuth redirect URI registered on the provider side.
    /// </summary>
    /// <example>https://provider.example.com/oauth</example>
    public string RedirectUri { get; set; }

    /// <summary>
    /// The domain to add to the portal's Content Security Policy whitelist.
    /// </summary>
    /// <example>https://provider.example.com</example>
    public string CspDomain { get; set; }
}
