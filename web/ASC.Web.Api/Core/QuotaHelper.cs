﻿// (c) Copyright Ascensio System SIA 2009-2024
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

using ASC.Core.Common.Quota.Features;

namespace ASC.Web.Api.Core;

[Scope]
public class QuotaHelper(
    TenantManager tenantManager,
    IServiceProvider serviceProvider,
    CoreBaseSettings coreBaseSettings,
    SettingsManager settingsManager,
    UserManager userManager, 
    AuthContext authContext)
{
    public async IAsyncEnumerable<QuotaDto> GetQuotasAsync()
    {
        var quotaList = await tenantManager.GetTenantQuotasAsync(false);
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        
        foreach (var quota in quotaList)
        {
            yield return await ToQuotaDto(quota, userType);
        }
    }

    public async Task<QuotaDto> GetCurrentQuotaAsync(bool refresh = false, bool getUsed = true)
    {
        var quota = await tenantManager.GetCurrentTenantQuotaAsync(refresh);
        var userType = await userManager.GetUserTypeAsync(authContext.CurrentAccount.ID);
        return await ToQuotaDto(quota, userType, getUsed);
    }

    private async Task<QuotaDto> ToQuotaDto(TenantQuota quota, EmployeeType employeeType, bool getUsed = false)
    {
        var features = await GetFeatures(quota, employeeType, getUsed).ToListAsync();

        var result =  new QuotaDto
        {
            Id = quota.TenantId,
            Title = Resource.ResourceManager.GetString($"Tariffs_{quota.Name}"),

            NonProfit = quota.NonProfit,
            Free = quota.Free,
            Trial = quota.Trial,

            Price = new PriceDto
            {
                Value = quota.Price,
                CurrencySymbol = quota.PriceCurrencySymbol
            },

            Features = features
        };

        if (coreBaseSettings.Standalone || (await tenantManager.GetCurrentTenantQuotaAsync()).Statistic)
        {
            var tenantUserQuotaSettingsTask = settingsManager.LoadAsync<TenantUserQuotaSettings>();
            var tenantRoomQuotaSettingsTask = settingsManager.LoadAsync<TenantRoomQuotaSettings>();
            var tenantQuotaSettingsTask = settingsManager.LoadAsync<TenantQuotaSettings>();

            await Task.WhenAll(tenantUserQuotaSettingsTask, tenantRoomQuotaSettingsTask, tenantQuotaSettingsTask);

            result.UsersQuota = await tenantUserQuotaSettingsTask;
            result.RoomsQuota = await tenantRoomQuotaSettingsTask;
            result.TenantCustomQuota = await tenantQuotaSettingsTask;
        }

        return result;
    }

    private async IAsyncEnumerable<TenantQuotaFeatureDto> GetFeatures(TenantQuota quota, EmployeeType employeeType, bool getUsed)
    {
        var assembly = GetType().Assembly;

        foreach (var feature in quota.TenantQuotaFeatures.
                    Where(r =>
                        {
                            if (r.Standalone)
                            {
                                return coreBaseSettings.Standalone;
                            }

                            if (r.EmployeeType == EmployeeType.DocSpaceAdmin && employeeType != EmployeeType.DocSpaceAdmin)
                            {
                                return false;
                            }
                            
                            return r.Visible;
                        })
                    .OrderBy(r => r.Order))
        {
            var result = new TenantQuotaFeatureDto
            {
                Title = Resource.ResourceManager.GetString($"TariffsFeature_{feature.Name}")
            };

            if (feature.Paid)
            {
                result.PriceTitle = Resource.ResourceManager.GetString($"TariffsFeature_{feature.Name}_price_count");
            }

            result.Id = feature.Name;

            object used = null;
            var availableFeature = true;
            var isUsedAvailable = employeeType != EmployeeType.Guest && 
                                  (employeeType != EmployeeType.User || 
                                   feature.Name == MaxTotalSizeFeature.MaxTotalSizeFeatureName);

            if (feature is TenantQuotaFeatureSize size)
            {
                result.Value = size.Value == long.MaxValue ? -1 : size.Value;
                result.Type = "size";
                result.Title = string.Format(result.Title, FileSizeComment.FilesSizeToString((long)result.Value));

                await GetStat<long>();
            }
            else if (feature is TenantQuotaFeatureCount count)
            {
                result.Value = count.Value == int.MaxValue ? -1 : count.Value;
                result.Type = "count";

                await GetStat<int>();
            }
            else if (feature is TenantQuotaFeatureFlag flag)
            {
                result.Value = flag.Value;
                result.Type = "flag";

                availableFeature = flag.Value;
            }

            if (getUsed)
            {
                if (used != null)
                {
                    result.Used = new FeatureUsedDto
                    {
                        Value = isUsedAvailable ? used : null,
                        Title = Resource.ResourceManager.GetString($"TariffsFeature_used_{feature.Name}")
                    };
                }
            }
            else
            {
                var img = assembly.GetManifestResourceStream($"{assembly.GetName().Name}.img.{feature.Name}.svg");

                if (availableFeature && img != null)
                {
                    try
                    {
                        using var memoryStream = new MemoryStream();
                        await img.CopyToAsync(memoryStream);
                        result.Image = Encoding.UTF8.GetString(memoryStream.ToArray());
                    }
                    catch (Exception)
                    {

                    }
                }
            }

            yield return result;

            async Task GetStat<T>()
            {
                var statisticProvider = (ITenantQuotaFeatureStat<T>)serviceProvider.GetService(typeof(ITenantQuotaFeatureStat<,>).MakeGenericType(feature.GetType(), typeof(T)));

                if (statisticProvider != null)
                {
                    used = await statisticProvider.GetValueAsync();
                }
            }
        }
    }
}
