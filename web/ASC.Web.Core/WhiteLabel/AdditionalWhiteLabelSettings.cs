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

namespace ASC.Web.Core.WhiteLabel;

/// <summary>
/// The additional white label settings wrapper.
/// </summary>
public class AdditionalWhiteLabelSettingsWrapper
{
    /// <summary>
    /// The additional white label settings.
    /// </summary>
    /// <example>{"startDocsEnabled": true, "helpCenterEnabled": true, "feedbackAndSupportEnabled": true, "userForumEnabled": true, "videoGuidesEnabled": true, "licenseAgreementsEnabled": true}</example>
    public AdditionalWhiteLabelSettings Settings { get; set; }
}

/// <summary>
/// The additional white label settings.
/// </summary>
public class AdditionalWhiteLabelSettings : ISettings<AdditionalWhiteLabelSettings>
{
    /// <summary>
    /// The external resource settings helper.
    /// </summary>
    public ExternalResourceSettingsHelper ExternalResourceSettingsHelper;

    /// <summary>
    /// Specifies if the sample documents are displayed or hidden.
    /// </summary>
    /// <example>true</example>
    public bool StartDocsEnabled { get; init; }

    /// <summary>
    /// Specifies if the Help Center link is available or not.
    /// </summary>
    /// <example>true</example>
    public bool HelpCenterEnabled { get; init; }

    /// <summary>
    /// Specifies if the "Feedback &amp; Support" link is available or not.
    /// </summary>
    /// <example>true</example>
    public bool FeedbackAndSupportEnabled { get; init; }

    /// <summary>
    /// Specifies if the user forum is available or not.
    /// </summary>
    /// <example>true</example>
    public bool UserForumEnabled { get; init; }

    /// <summary>
    /// Specifies if the Video Guides link is available or not.
    /// </summary>
    /// <example>true</example>
    public bool VideoGuidesEnabled { get; init; }

    /// <summary>
    /// Specifies if the License Agreements link is available or not.
    /// </summary>
    /// <example>true</example>
    public bool LicenseAgreementsEnabled { get; init; }
    
    public static Guid ID => new("{0108422F-C05D-488E-B271-30C4032494DA}");

    public AdditionalWhiteLabelSettings(ExternalResourceSettingsHelper externalResourceSettingsHelper)
    {
        ExternalResourceSettingsHelper = externalResourceSettingsHelper;
    }

    public AdditionalWhiteLabelSettings() { }

    public AdditionalWhiteLabelSettings GetDefault()
    {
        return new AdditionalWhiteLabelSettings(ExternalResourceSettingsHelper)
        {
            StartDocsEnabled = true,
            HelpCenterEnabled = !string.IsNullOrWhiteSpace(ExternalResourceSettingsHelper?.Helpcenter.GetDefaultRegionalDomain()),
            FeedbackAndSupportEnabled = !string.IsNullOrWhiteSpace(ExternalResourceSettingsHelper?.Support.GetDefaultRegionalDomain()),
            UserForumEnabled = !string.IsNullOrWhiteSpace(ExternalResourceSettingsHelper?.Forum.GetDefaultRegionalDomain()),
            VideoGuidesEnabled = !string.IsNullOrWhiteSpace(ExternalResourceSettingsHelper?.Videoguides.GetDefaultRegionalDomain()),
            LicenseAgreementsEnabled = !string.IsNullOrWhiteSpace(ExternalResourceSettingsHelper?.Common.GetDefaultRegionalFullEntry("license"))
        };
    }
    
    /// <summary>
    /// The timestamp indicating when the settings were last modified.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastModified { get; set; }
}