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

namespace ASC.Web.Core.WhiteLabel;

[Singleton]
public class ExternalResourceSettingsHelper(IConfiguration configuration)
{
    public readonly string DefaultCultureName = "default";

    public Dictionary<string, Dictionary<string, string>> CultureSpecificEntries =
        configuration.GetSection("externalresources").Get<Dictionary<string, Dictionary<string, string>>>();

    public string GetDefaultFullEntry(string key, bool regional = true)
    {
        var value = GetEntry(key, DefaultCultureName);

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.StartsWith('/'))
        {
            var baseValue = GetBaseEntry(key, DefaultCultureName);
            value = $"{baseValue}{value}";
        }

        return regional ? BaseCommonLinkUtility.GetRegionalUrl(value, null) : value;
    }

    public string GetFullEntry(string key, CultureInfo culture = null, bool regional = true)
    {
        culture = culture ?? CultureInfo.CurrentCulture;

        var value = GetEntry(key, culture.Name);

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (value.StartsWith('/'))
        {
            var baseValue = GetBaseEntry(key, culture.Name);
            value = $"{baseValue}{value}";
        }

        return regional ? BaseCommonLinkUtility.GetRegionalUrl(value, culture.TwoLetterISOLanguageName) : value;
    }

    public string GetEntry(string key, string cultureName)
    {
        return CultureSpecificEntries.GetValueOrDefault(cultureName)?.GetValueOrDefault(key) ??
               CultureSpecificEntries.GetValueOrDefault(DefaultCultureName)?.GetValueOrDefault(key);
    }

    private string GetBaseEntry(string key, string cultureName)
    {
        var index = key.IndexOf('_');
        if (index == -1)
        {
            return null;
        }

        var baseKey = key.Substring(0, index);

        return GetEntry(baseKey, cultureName);
    }
}