// (c) Copyright Ascensio System SIA 2009-2025
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

namespace ASC.Core.Tenants;

[Scope]
internal class TenantQuotaPriceResolver(TenantManager tenantManager, RegionHelper regionHelper)
{
    public string ResolvePriceCurrencySymbol(DbQuota source)
    {
        var (_, currencySymbol, _) = Resolve(source);

        return currencySymbol;
    }
    
    public string ResolveISOCurrencySymbol(DbQuota source)
    {
        var (_, _, isoCurrencySymbol) = Resolve(source);

        return isoCurrencySymbol;
    }
    
    public decimal ResolvePrice(DbQuota source)
    {
        var (price, _, _) = Resolve(source);

        return price;
    }
    
    private (decimal, string, string) Resolve(DbQuota source)
    {
        var priceInfo = tenantManager.GetProductPriceInfo(source.ProductId, source.Wallet);

        if (priceInfo != null)
        {
            var currentRegion = regionHelper.GetCurrentRegionInfoAsync(new Dictionary<string, Dictionary<string, decimal>> { { source.ProductId, priceInfo } }).Result;

            if (priceInfo.TryGetValue(currentRegion.ISOCurrencySymbol, out var resolve))
            {
                return (resolve, currentRegion.CurrencySymbol, currentRegion.ISOCurrencySymbol);
            }
        }

        var defaultRegion = regionHelper.GetDefaultRegionInfo();
        return (source.Price, defaultRegion.CurrencySymbol, defaultRegion.ISOCurrencySymbol);
    }
}