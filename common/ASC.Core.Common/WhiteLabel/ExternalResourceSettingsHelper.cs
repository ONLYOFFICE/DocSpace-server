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

namespace ASC.Core.Common.WhiteLabel;

[Singleton]
public class ExternalResourceSettingsHelper(IConfiguration configuration)
{
    public readonly ExternalResource Api = new(configuration.GetSection("externalresources:api").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource Common = new(configuration.GetSection("externalresources:common").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource Forum = new(configuration.GetSection("externalresources:forum").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource Helpcenter = new(configuration.GetSection("externalresources:helpcenter").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource Integrations = new(configuration.GetSection("externalresources:integrations").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource Site = new(configuration.GetSection("externalresources:site").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource SocialNetworks = new(configuration.GetSection("externalresources:socialnetworks").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource Support = new(configuration.GetSection("externalresources:support").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
    public readonly ExternalResource Videoguides = new(configuration.GetSection("externalresources:videoguides").Get<Dictionary<string, CultureSpecificExternalResource>>() ?? []);
}

public class ExternalResource(Dictionary<string, CultureSpecificExternalResource> cultureSpecificExternalResources)
{
    private const string DefaultCultureName = "default";

    public string GetDomain(string cultureName)
    {
        return cultureSpecificExternalResources.GetValueOrDefault(cultureName)?.Domain ??
               cultureSpecificExternalResources.GetValueOrDefault(DefaultCultureName)?.Domain;
    }

    public string GetDefaultRegionalDomain()
    {
        var value = GetDomain(DefaultCultureName);

        return BaseCommonLinkUtility.GetRegionalUrl(value, null);
    }

    public string GetRegionalDomain(CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var value = GetDomain(culture.Name);

        return BaseCommonLinkUtility.GetRegionalUrl(value, culture.TwoLetterISOLanguageName);
    }

    public string GetEntry(string key, string cultureName)
    {
        return cultureSpecificExternalResources.GetValueOrDefault(cultureName)?.Entries?.GetValueOrDefault(key) ??
               cultureSpecificExternalResources.GetValueOrDefault(DefaultCultureName)?.Entries?.GetValueOrDefault(key);
    }

    public string GetDefaultRegionalFullEntry(string key)
    {
        var value = GetEntry(key, DefaultCultureName);

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.StartsWith('/'))
        {
            var domain = GetDomain(DefaultCultureName);
            value = $"{domain}{value}";
        }

        return BaseCommonLinkUtility.GetRegionalUrl(value, null);
    }

    public string GetRegionalFullEntry(string key, CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var value = GetEntry(key, culture.Name);

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.StartsWith('/'))
        {
            var domain = GetDomain(culture.Name);
            value = $"{domain}{value}";
        }

        return BaseCommonLinkUtility.GetRegionalUrl(value, culture.TwoLetterISOLanguageName);
    }

    public CultureSpecificExternalResource GetCultureSpecificExternalResource(CultureInfo culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        var defaultExternalResource = cultureSpecificExternalResources.GetValueOrDefault(DefaultCultureName);
        var specificExternalResource = cultureSpecificExternalResources.GetValueOrDefault(culture.Name);

        var result = new CultureSpecificExternalResource
        {
            Domain = BaseCommonLinkUtility.GetRegionalUrl(specificExternalResource?.Domain ?? defaultExternalResource?.Domain, culture.TwoLetterISOLanguageName),
            Entries = defaultExternalResource?.Entries?.ToDictionary(entry => entry.Key, entry => entry.Value)
        };

        if (specificExternalResource == null)
        {
            return result;
        }

        if (result.Entries == null)
        {
            result.Entries = specificExternalResource.Entries?.ToDictionary(entry => entry.Key, entry => entry.Value);
            return result;
        }

        foreach (var entry in specificExternalResource.Entries)
        {
            result.Entries[entry.Key] = entry.Value;
        }

        return result;
    }
}

/// <summary>
/// The external resource parameters.
/// </summary>
public class CultureSpecificExternalResource
{
    /// <summary>
    /// The external resource domain.
    /// </summary>
    /// <example>example.com</example>
    public string Domain { get; set; }

    /// <summary>
    /// The external resource entries.
    /// </summary>
    /// <example>
    /// {
    ///   "welcomeMessage": "Welcome",
    ///   "logoutButton": "Log out"
    /// }
    /// </example>
    public Dictionary<string, string> Entries { get; set; }
}
