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
/// The company white label settings wrapper.
/// </summary>
public class CompanyWhiteLabelSettingsWrapper
{
    /// <summary>
    /// The company white label settings.
    /// </summary>
    /// <example>{"companyName": "ONLYOFFICE", "site": "https://www.onlyoffice.com", "email": "support@onlyoffice.com", "address": "Lubanas st. 125a-25", "phone": "+7 843 2271372", "isLicensor": true}</example>
    public CompanyWhiteLabelSettings Settings { get; set; }
}

/// <summary>
/// The company white label settings.
/// </summary>
public class CompanyWhiteLabelSettings : ISettings<CompanyWhiteLabelSettings>
{
    /// <summary>
    /// The core settings.
    /// </summary>
    public CoreSettings CoreSettings;

    /// <summary>
    /// The company name.
    /// </summary>
    /// <example>ONLYOFFICE</example>
    [StringLength(255)]
    public string CompanyName { get; set; }

    /// <summary>
    /// The company site.
    /// </summary>
    /// <example>https://www.onlyoffice.com</example>
    [Url]
    [StringLength(255)]
    public string Site { get; set; }

    /// <summary>
    /// The company email address.
    /// </summary>
    /// <example>support@onlyoffice.com</example>
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    /// <summary>
    /// The company address.
    /// </summary>
    /// <example>Lubanas st. 125a-25</example>
    [StringLength(255)]
    public string Address { get; set; }

    /// <summary>
    /// The company phone number.
    /// </summary>
    /// <example>+7 843 2271372</example>
    [Phone]
    [StringLength(255)]
    public string Phone { get; set; }

    /// <summary>
    /// Specifies if a company is a licensor or not.
    /// </summary>
    /// <example>true</example>
    [JsonPropertyName("IsLicensor")]
    public bool IsLicensor { get; set; }

    /// <summary>
    /// Specifies if the About page is visible or not
    /// </summary>
    /// <example>false</example>
    public bool HideAbout { get; set; }

    public CompanyWhiteLabelSettings(CoreSettings coreSettings)
    {
        CoreSettings = coreSettings;
    }

    public CompanyWhiteLabelSettings()
    {

    }

    #region ISettings Members

    public static Guid ID => new("{C3C5A846-01A3-476D-A962-1CFD78C04ADB}");


    public CompanyWhiteLabelSettings GetDefault()
    {
        var settings = CoreSettings.GetSetting("CompanyWhiteLabelSettings");

        var result = string.IsNullOrEmpty(settings) ? new CompanyWhiteLabelSettings(CoreSettings) : JsonSerializer.Deserialize<CompanyWhiteLabelSettings>(settings);

        result.CoreSettings = CoreSettings;

        return result;
    }

    /// <summary>
    /// The timestamp indicating when the settings were last modified.
    /// </summary>
    /// <example>1990-01-01T00:00:00Z</example>
    public DateTime LastModified { get; set; }

    #endregion
}

[Scope]
public class CompanyWhiteLabelSettingsHelper(CoreSettings coreSettings, SettingsManager settingsManager)
{
    public async Task<CompanyWhiteLabelSettings> InstanceAsync()
    {
        return await settingsManager.LoadForDefaultTenantAsync<CompanyWhiteLabelSettings>();
    }

    public bool IsDefault(CompanyWhiteLabelSettings settings)
    {
        settings.CoreSettings = coreSettings;
        var defaultSettings = settings.GetDefault();

        return settings.CompanyName == defaultSettings.CompanyName &&
                settings.Site == defaultSettings.Site &&
                settings.Email == defaultSettings.Email &&
                settings.Address == defaultSettings.Address &&
                settings.Phone == defaultSettings.Phone &&
                settings.IsLicensor == defaultSettings.IsLicensor &&
                settings.HideAbout == defaultSettings.HideAbout;
    }
}
