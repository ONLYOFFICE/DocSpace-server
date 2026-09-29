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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The external resources settings.
/// </summary>
public class ExternalResourcesDto
{
    /// <summary>
    /// The link to the administration panel. It is returned only to the full administrators of a licensed (Enterprise) server (standalone) portal.
    /// </summary>
    public ExternalResourceDto AdminPanel { get; init; }

    /// <summary>
    /// The link to the product API.
    /// </summary>
    public ExternalResourceDto Api { get; init; }

    /// <summary>
    /// The link to the common product information.
    /// </summary>
    public ExternalResourceDto Common { get; init; }

    /// <summary>
    /// The link to the forum.
    /// </summary>
    public ExternalResourceDto Forum { get; init; }

    /// <summary>
    /// The link to the Help Center.
    /// </summary>
    public ExternalResourceDto Helpcenter { get; init; }

    /// <summary>
    /// The link to the product integrations.
    /// </summary>
    public ExternalResourceDto Integrations { get; init; }

    /// <summary>
    /// The link to the product website.
    /// </summary>
    public ExternalResourceDto Site { get; init; }

    /// <summary>
    /// The link to the product social nerworks.
    /// </summary>
    public ExternalResourceDto SocialNetworks { get; init; }

    /// <summary>
    /// The link to the product support.
    /// </summary>
    public ExternalResourceDto Support { get; init; }

    /// <summary>
    /// The link to the video guides.
    /// </summary>
    public ExternalResourceDto Videoguides { get; init; }
}

/// <summary>
/// The external resource parameters.
/// </summary>
public class ExternalResourceDto
{
    /// <summary>
    /// The external resource domain.
    /// </summary>
    /// <example>example.com</example>
    public string Domain { get; init; }

    /// <summary>
    /// The external resource entries.
    /// </summary>
    /// <example>
    /// {
    ///   "welcomeMessage": "Welcome",
    ///   "logoutButton": "Log out"
    /// }
    /// </example>
    public Dictionary<string, string> Entries { get; init; }
}

[Scope]
public class ExternalResourcesDtoHelper(ExternalResourceSettingsHelper helper, TenantExtraConfig tenantExtraConfig)
{
    public ExternalResourcesDto Get(AdditionalWhiteLabelSettings whiteLabelSettings, bool isDocSpaceAdmin, CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        return new ExternalResourcesDto
        {
            AdminPanel = tenantExtraConfig.Enterprise && isDocSpaceAdmin ? helper.AdminPanel.GetCultureSpecificExternalResource(culture).Map() : null,
            Api = helper.Api.GetCultureSpecificExternalResource(culture).Map(),
            Common = helper.Common.GetCultureSpecificExternalResource(culture).Map(),
            Forum = whiteLabelSettings.UserForumEnabled ? helper.Forum.GetCultureSpecificExternalResource(culture).Map() : null,
            Helpcenter = whiteLabelSettings.HelpCenterEnabled ? helper.Helpcenter.GetCultureSpecificExternalResource(culture).Map() : null,
            Integrations = helper.Integrations.GetCultureSpecificExternalResource(culture).Map(),
            Site = helper.Site.GetCultureSpecificExternalResource(culture).Map(),
            SocialNetworks = helper.SocialNetworks.GetCultureSpecificExternalResource(culture).Map(),
            Support = whiteLabelSettings.FeedbackAndSupportEnabled ? helper.Support.GetCultureSpecificExternalResource(culture).Map() : null,
            Videoguides = whiteLabelSettings.VideoGuidesEnabled ? helper.Videoguides.GetCultureSpecificExternalResource(culture).Map() : null
        };
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class ExternalResourceDtoMapper
{
    public static partial ExternalResourceDto Map(this CultureSpecificExternalResource source);
}
