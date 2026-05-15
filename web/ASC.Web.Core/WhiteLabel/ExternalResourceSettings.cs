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

[Scope]
public class ExternalResourceSettings(ExternalResourceSettingsHelper helper)
{
    public CultureSpecificExternalResources GetCultureSpecificExternalResources(CultureInfo culture = null, AdditionalWhiteLabelSettings whiteLabelSettings = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        return new()
        {
            Api = helper.Api.GetCultureSpecificExternalResource(culture),
            Common = helper.Common.GetCultureSpecificExternalResource(culture),
            Forum = whiteLabelSettings.UserForumEnabled ? helper.Forum.GetCultureSpecificExternalResource(culture) : null,
            Helpcenter = whiteLabelSettings.HelpCenterEnabled ? helper.Helpcenter.GetCultureSpecificExternalResource(culture) : null,
            Integrations = helper.Integrations.GetCultureSpecificExternalResource(culture),
            Site = helper.Site.GetCultureSpecificExternalResource(culture),
            SocialNetworks = helper.SocialNetworks.GetCultureSpecificExternalResource(culture),
            Support = whiteLabelSettings.FeedbackAndSupportEnabled ? helper.Support.GetCultureSpecificExternalResource(culture) : null,
            Videoguides = whiteLabelSettings.VideoGuidesEnabled ? helper.Videoguides.GetCultureSpecificExternalResource(culture) : null
        };
    }
}

/// <summary>
/// The external resources settings.
/// </summary>
public class CultureSpecificExternalResources
{
    /// <summary>
    /// The link to the product API.
    /// </summary>
    public CultureSpecificExternalResource Api { get; set; }

    /// <summary>
    /// The link to the common product information.
    /// </summary>
    public CultureSpecificExternalResource Common { get; set; }

    /// <summary>
    /// The link to the forum.
    /// </summary>
    public CultureSpecificExternalResource Forum { get; set; }

    /// <summary>
    /// The link to the Help Center.
    /// </summary>
    public CultureSpecificExternalResource Helpcenter { get; set; }

    /// <summary>
    /// The link to the product integrations.
    /// </summary>
    public CultureSpecificExternalResource Integrations { get; set; }

    /// <summary>
    /// The link to the product website.
    /// </summary>
    public CultureSpecificExternalResource Site { get; set; }

    /// <summary>
    /// The link to the product social nerworks.
    /// </summary>
    public CultureSpecificExternalResource SocialNetworks { get; set; }

    /// <summary>
    /// The link to the product support.
    /// </summary>
    public CultureSpecificExternalResource Support { get; set; }

    /// <summary>
    /// The link to the video guides.
    /// </summary>
    public CultureSpecificExternalResource Videoguides { get; set; }
}
