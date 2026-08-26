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

namespace ASC.Files.Core.Services.DocumentBuilderService;

/// <summary>
/// The shared report header: the portal logo, theme colors, the company/generation-date values
/// printed above every generated report, and the single date cell format reports use. The reporting
/// period is report-specific and is therefore assembled by the caller rather than carried here.
/// <see cref="LogoWidthMm"/>/<see cref="LogoHeightMm"/> are the box the scripts draw the logo into.
/// </summary>
public sealed record ReportHeader(
    string LogoSrc,
    double LogoWidthMm,
    double LogoHeightMm,
    int[] MainBgColor,
    int[] LightBgColor,
    int[] MainFontColor,
    string Company,
    string DateGenerated,
    string LongDateFormat);

[Scope]
public class ReportHeaderService(
    SettingsManager settingsManager,
    TenantLogoManager tenantLogoManager,
    TenantUtil tenantUtil)
{
    // The logo is drawn into a fixed box, so the box has to keep the proportions the logo was
    // authored at or it comes out stretched. Both logos are laid out at the same 10 mm height, so
    // the width follows from the source size:
    //   built-in    177 x 24 px (client's logo/lightsmall.svg)          -> 177 * 10 / 24
    //   white-label 422 x 48 px (TenantWhiteLabelSettings.LogoLightSmallSize) -> 422 * 10 / 48
    private const double DefaultLogoWidthMm = 73.8;
    private const double BrandedLogoWidthMm = 87.9;
    private const double LogoHeightMm = 10;

    public async Task<ReportHeader> BuildAsync(CultureInfo culture)
    {
        var logoText = await tenantLogoManager.GetLogoTextAsync();

        // the document builder currently cannot embed a logo referenced by URL, so we inline it
        // as a base64 data URI. Once the builder's image handling is fixed, switch back to the URL:
        var logoSrc = await tenantLogoManager.GetTopLogoDataUriAsync()
                      ?? await tenantLogoManager.GetTopLogoAbsoluteUrlAsync();

        var logoWidthMm = await tenantLogoManager.IsDefaultTopLogoAsync() ? DefaultLogoWidthMm : BrandedLogoWidthMm;

        var customColorThemesSettings = await settingsManager.LoadAsync<CustomColorThemesSettings>();
        var selectedColorTheme = customColorThemesSettings.Themes.First(x => x.Id == customColorThemesSettings.Selected);

        // Every date that reaches a report goes through ConvertNumerals rather than
        // CultureInfo.InvariantCulture, so all reports carry one representation of a date. On ar-SA
        // and ar-lb portals that means Arabic-Indic digits, which the document builder parses.
        return new ReportHeader(
            logoSrc,
            logoWidthMm,
            LogoHeightMm,
            DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 1),
            DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Main.Accent, 0.08),
            DocumentBuilderScriptHelper.ConvertHtmlColorToRgb(selectedColorTheme.Text.Accent, 1),
            logoText,
            tenantUtil.DateTimeNow().ConvertNumerals("G"),
            DocumentBuilderScriptHelper.GetLongDateTimeFormat(culture));
    }
}
