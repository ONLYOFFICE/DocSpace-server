// (c) Copyright Ascensio System SIA 2009-2024
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
        culture = culture ?? CultureInfo.CurrentCulture;

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
        culture = culture ?? CultureInfo.CurrentCulture;

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
        culture = culture ?? CultureInfo.CurrentCulture;

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
            result.Entries = specificExternalResource?.Entries?.ToDictionary(entry => entry.Key, entry => entry.Value);
            return result;
        }

        foreach (var entry in specificExternalResource.Entries)
        {
            result.Entries[entry.Key] = entry.Value;
        }

        return result;
    }
}

public class CultureSpecificExternalResource
{
    public string Domain { get; set; }
    public Dictionary<string, string> Entries { get; set; }
}