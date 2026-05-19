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

namespace ASC.Core;

[Scope]
public class RegionHelper(TenantManager tenantManager,
        IHttpContextAccessor httpContextAccessor,
        GeolocationHelper geolocationHelper,
        UserManager userManager)
{
    public async Task<RegionInfo> GetCurrentRegionInfoAsync(IDictionary<string, Dictionary<string, decimal>> priceInfo = null)
    {
        var geoInfo = await geolocationHelper.GetIPGeolocationFromHttpContextAsync();
        var countryCode = httpContextAccessor.HttpContext?.Request.Query["country"];
        var currentRegion = GetRegionInfo(countryCode);

        if (currentRegion == null && geoInfo != null)
        {
            currentRegion = GetRegionInfo(geoInfo.Key);
        }

        if (currentRegion == null)
        {
            var tenant = tenantManager.GetCurrentTenant(false);
            if (tenant != null)
            {
                var owner = await userManager.GetUsersAsync(tenant.OwnerId);
                var culture = string.IsNullOrEmpty(owner.CultureName) ? tenant.GetCulture() : owner.GetCulture();
                currentRegion = GetRegionInfo(culture.Name);
            }
        }

        var defaultRegion = GetDefaultRegionInfo();
        if (currentRegion == null || currentRegion.Name.Equals(defaultRegion.Name))
        {
            return defaultRegion;
        }

        if (geoInfo != null && !string.IsNullOrEmpty(geoInfo.Continent) && geoInfo.Continent == "EU")
        {
            currentRegion = GetRegionInfo("ES");
        }

        priceInfo ??= await tenantManager.GetProductPriceInfoAsync();
        if (priceInfo.Values.Any(value => value.ContainsKey(currentRegion.ISOCurrencySymbol)))
        {
            return currentRegion;
        }

        return defaultRegion;
    }

    public RegionInfo GetDefaultRegionInfo()
    {
        return GetRegionInfo("US");
    }

    public async Task<string> GetCurrencyFromRequestAsync()
    {
        var defaultRegion = GetDefaultRegionInfo();
        var geoinfo = await geolocationHelper.GetIPGeolocationFromHttpContextAsync();

        if (!string.IsNullOrEmpty(geoinfo.Key))
        {
            try
            {
                var currentRegion = new RegionInfo(geoinfo.Key);

                if (!currentRegion.Name.Equals(defaultRegion.Name))
                {
                    var priceInfo = await tenantManager.GetProductPriceInfoAsync();

                    if (priceInfo.Values.Any(value => value.ContainsKey(currentRegion.ISOCurrencySymbol)))
                    {
                        return currentRegion.ISOCurrencySymbol;
                    }

                    if (!string.IsNullOrEmpty(geoinfo.Continent) && geoinfo.Continent == "EU")
                    {
                        return "EUR";
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        return defaultRegion.ISOCurrencySymbol;
    }

    private RegionInfo GetRegionInfo(string isoTwoLetterCountryCode)
    {
        RegionInfo regionInfo = null;

        if (!string.IsNullOrEmpty(isoTwoLetterCountryCode))
        {
            try
            {
                regionInfo = new RegionInfo(isoTwoLetterCountryCode);
            }
            catch
            {
            }
        }

        return regionInfo;
    }
}