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
/// Which of the ONLYOFFICE help and community entries the interface may offer, installation-wide.
/// </summary>
public class AdditionalWhiteLabelSettingsDto
{
    /// <summary>
    /// Whether the sample documents that ONLYOFFICE ships may be placed in a new user's Documents. Unlike the link
    /// flags below it depends on nothing that has to be configured, so its built-in value is always `true`.
    /// </summary>
    /// <example>true</example>
    public required bool StartDocsEnabled { get; set; }

    /// <summary>
    /// Whether the interface may offer the Help Center entry. It is `false` both when the entry was switched off
    /// for the installation and when the installation configures no Help Center address at all; the addresses
    /// themselves are not part of this answer and arrive in `externalResources` of `GET api/2.0/settings`.
    /// </summary>
    /// <example>true</example>
    public required bool HelpCenterEnabled { get; set; }

    /// <summary>
    /// Whether the interface may offer the Feedback and Support entry, `false` for the same two reasons as
    /// `helpCenterEnabled`.
    /// </summary>
    /// <example>true</example>
    public required bool FeedbackAndSupportEnabled { get; set; }

    /// <summary>
    /// Whether the interface may offer the user forum entry, `false` for the same two reasons as
    /// `helpCenterEnabled`.
    /// </summary>
    /// <example>true</example>
    public required bool UserForumEnabled { get; set; }

    /// <summary>
    /// Whether the interface may offer the Video Guides entry, `false` for the same two reasons as
    /// `helpCenterEnabled`.
    /// </summary>
    /// <example>true</example>
    public required bool VideoGuidesEnabled { get; set; }

    /// <summary>
    /// Whether the interface may offer the License Agreements entry, `false` for the same two reasons as
    /// `helpCenterEnabled`.
    /// </summary>
    /// <example>true</example>
    public required bool LicenseAgreementsEnabled { get; set; }

    /// <summary>
    /// Whether all six flags still hold the values the installation starts out with. It turns `false` as soon as
    /// one of them is saved differently and `true` again after `DELETE api/2.0/settings/rebranding/additional`.
    /// Because a link flag starts out off when no address is configured for it, `true` does not mean every entry
    /// is on.
    /// </summary>
    /// <example>false</example>
    public required bool IsDefault { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class AdditionalWhiteLabelSettingsMapper(ExternalResourceSettingsHelper externalResourceSettingsHelper)
{
    [MapPropertyFromSource(nameof(AdditionalWhiteLabelSettingsDto.IsDefault), Use = nameof(MapIsDefault))]
    public partial AdditionalWhiteLabelSettingsDto Map(AdditionalWhiteLabelSettings source);

    private bool MapIsDefault(AdditionalWhiteLabelSettings source)
    {
        source.ExternalResourceSettingsHelper ??= externalResourceSettingsHelper;

        var defaultSettings = source.GetDefault();

        return source.StartDocsEnabled == defaultSettings.StartDocsEnabled &&
                           source.HelpCenterEnabled == defaultSettings.HelpCenterEnabled &&
                           source.FeedbackAndSupportEnabled == defaultSettings.FeedbackAndSupportEnabled &&
                           source.UserForumEnabled == defaultSettings.UserForumEnabled &&
                           source.VideoGuidesEnabled == defaultSettings.VideoGuidesEnabled &&
                           source.LicenseAgreementsEnabled == defaultSettings.LicenseAgreementsEnabled;
    }
}