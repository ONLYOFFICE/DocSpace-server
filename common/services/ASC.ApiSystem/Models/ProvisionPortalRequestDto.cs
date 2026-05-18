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
/// The request parameters for provisioning a portal with an OAuth provider.
/// </summary>
public class ProvisionPortalRequestDto : TenantModel
{
    /// <summary>
    /// The OAuth provider configuration.
    /// </summary>
    /// <example>
    /// {
    /// "name": "provider_name",
    /// "clientId": "abc123",
    /// "clientSecret": "secret",
    /// "baseUrl": "https://provider.example.com",
    /// "redirectUri": "https://provider.example.com/oauth",
    /// "cspDomain": "https://provider.example.com"
    /// }
    /// </example>
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
