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

namespace ASC.Web.Api.ApiModels.RequestsDto;

/// <summary>
/// The body of a company details change for the installation's branding.
/// </summary>
public class CompanyWhiteLabelSettingsRequestDto
{
    /// <summary>
    /// The company details to store.
    /// </summary>
    /// <example>{"companyName": "ONLYOFFICE", "site": "https://www.onlyoffice.com", "email": "support@onlyoffice.com", "address": "Lubanas st. 125a-25", "phone": "+7 843 2271372", "hideAbout": false}</example>
    public SaveCompanyInfoRequest Settings { get; set; }
}

/// <summary>
/// The company the installation is branded for, as shown on the About page and in letters.
/// </summary>
public class SaveCompanyInfoRequest
{
    /// <summary>
    /// The company name.
    /// </summary>
    /// <example>ONLYOFFICE</example>
    [StringLength(255)]
    public string CompanyName { get; set; }

    /// <summary>
    /// The company website, as an absolute URL.
    /// </summary>
    /// <example>https://www.onlyoffice.com</example>
    [Url]
    [StringLength(255)]
    public string Site { get; set; }

    /// <summary>
    /// The contact email address.
    /// </summary>
    /// <example>support@onlyoffice.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The postal address.
    /// </summary>
    /// <example>Lubanas st. 125a-25</example>
    [StringLength(255)]
    public string Address { get; set; }

    /// <summary>
    /// The contact phone number.
    /// </summary>
    /// <example>+7 843 2271372</example>
    [Phone]
    [StringLength(255)]
    public string Phone { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: saved details are never those of the licensor, so the server always stores `false`.
    /// </summary>
    /// <example>false</example>
    [JsonPropertyName("IsLicensor")]
    public bool IsLicensor { get; set; }

    /// <summary>
    /// Whether the About page is hidden.
    /// </summary>
    /// <example>false</example>
    public bool HideAbout { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>2026-01-01T10:00:00</example>
    public DateTime LastModified { get; set; }
}

/// <summary>
/// The body of a change to the help and community resources the interface offers.
/// </summary>
public class AdditionalWhiteLabelSettingsRequestDto
{
    /// <summary>
    /// The resource flags to store.
    /// </summary>
    /// <example>{"startDocsEnabled": true, "helpCenterEnabled": true, "feedbackAndSupportEnabled": true, "userForumEnabled": true, "videoGuidesEnabled": true, "licenseAgreementsEnabled": true}</example>
    public SaveAdditionalResourcesRequest Settings { get; set; }
}

/// <summary>
/// Which help and community resources the interface links to.
/// </summary>
public class SaveAdditionalResourcesRequest
{
    /// <summary>
    /// Whether the getting-started documents are offered.
    /// </summary>
    /// <example>true</example>
    public bool StartDocsEnabled { get; set; }

    /// <summary>
    /// Whether the help center is linked.
    /// </summary>
    /// <example>true</example>
    public bool HelpCenterEnabled { get; set; }

    /// <summary>
    /// Whether the feedback and support link is shown.
    /// </summary>
    /// <example>true</example>
    public bool FeedbackAndSupportEnabled { get; set; }

    /// <summary>
    /// Whether the user forum is linked.
    /// </summary>
    /// <example>true</example>
    public bool UserForumEnabled { get; set; }

    /// <summary>
    /// Whether the video guides are linked.
    /// </summary>
    /// <example>true</example>
    public bool VideoGuidesEnabled { get; set; }

    /// <summary>
    /// Whether the license agreements are linked.
    /// </summary>
    /// <example>true</example>
    public bool LicenseAgreementsEnabled { get; set; }

    /// <summary>
    /// Accepted for compatibility with earlier clients and not read: the server keeps its own value.
    /// </summary>
    /// <example>2026-01-01T10:00:00</example>
    public DateTime LastModified { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class SaveAdditionalResourcesRequestMapper
{
    [MapperIgnoreSource(nameof(SaveAdditionalResourcesRequest.LastModified))]
    public static partial AdditionalWhiteLabelSettings Map(this SaveAdditionalResourcesRequest source);
}

/// <summary>
/// The body of a change to the branding of the portal's letters.
/// </summary>
public class MailWhiteLabelSettingsRequestDto
{
    /// <summary>
    /// The letter branding to store.
    /// </summary>
    /// <example>{"footerEnabled": true, "footerSocialEnabled": true}</example>
    public SaveMailFooterRequest Settings { get; set; }
}

/// <summary>
/// Which footers the portal's letters carry.
/// </summary>
public class SaveMailFooterRequest
{
    /// <summary>
    /// Whether letters carry the footer.
    /// </summary>
    /// <example>true</example>
    public bool FooterEnabled { get; set; }

    /// <summary>
    /// Whether the footer carries the social network links.
    /// </summary>
    /// <example>true</example>
    public bool FooterSocialEnabled { get; set; }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class SaveMailFooterRequestMapper
{
    public static partial MailWhiteLabelSettings Map(this SaveMailFooterRequest source);
}
