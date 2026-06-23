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

namespace ASC.Web.Studio.Utility;

[Scope]
public class TenantExtra(
    TenantManager tenantManager,
    ITariffService tariffService,
    CoreBaseSettings coreBaseSettings,
    LicenseReader licenseReader,
    SetupInfo setupInfo,
    SettingsManager settingsManager,
    TenantExtraConfig tenantExtraConfig,
    CountPaidUserStatistic countPaidUserStatistic,
    MaxTotalSizeStatistic maxTotalSizeStatistic)
{
    public async Task<bool> GetEnableTariffSettings()
    {
        return
            SetupInfo.IsVisibleSettings<TariffSettings>()
            && !(await settingsManager.LoadAsync<TenantAccessSettings>()).Anyone
            && (!coreBaseSettings.Standalone || !string.IsNullOrEmpty(licenseReader.LicensePath))
            && string.IsNullOrEmpty(setupInfo.AmiMetaUrl);
    }

    public bool Saas => tenantExtraConfig.Saas;

    public bool Enterprise => tenantExtraConfig.Enterprise;

    public bool Developer => tenantExtraConfig.Developer;

    public bool Opensource => tenantExtraConfig.Opensource;

    private async Task<bool> EnterprisePaidAsync(bool withRequestToPaymentSystem = true)
    {
        return Enterprise && (await GetCurrentTariffAsync(withRequestToPaymentSystem)).State < TariffState.NotPaid;
    }

    public async Task<Tariff> GetCurrentTariffAsync(bool withRequestToPaymentSystem = true, bool refresh = false)
    {
        return await tariffService.GetTariffAsync(tenantManager.GetCurrentTenantId(), withRequestToPaymentSystem, refresh);
    }

    public async Task<IEnumerable<TenantQuota>> GetTenantQuotasAsync()
    {
        return await tenantManager.GetTenantQuotasAsync();
    }


    public async Task<TenantQuota> GetRightQuota()
    {
        var usedSpace = await maxTotalSizeStatistic.GetValueAsync();
        var needUsersCount = await countPaidUserStatistic.GetValueAsync();
        var quotas = await GetTenantQuotasAsync();

        return quotas.OrderBy(q => q.CountUser)
                     .ThenBy(q => q.Year)
                     .FirstOrDefault(q =>
                                     q.CountUser > needUsersCount
                                     && q.MaxTotalSize > usedSpace
                                     && !q.Free
                                     && !q.Trial);
    }

    public async Task<bool> IsNotPaidAsync(bool withRequestToPaymentSystem = true)
    {
        if (!await GetEnableTariffSettings())
        {
            return false;
        }

        var tariff = await GetCurrentTariffAsync(withRequestToPaymentSystem);

        return tariff.State >= TariffState.NotPaid || Enterprise && !await EnterprisePaidAsync(withRequestToPaymentSystem) && tariff.LicenseDate == DateTime.MaxValue;
    }

    public async Task DemandAccessSpacePermissionAsync()
    {
        if (!coreBaseSettings.Standalone || (await settingsManager.LoadAsync<TenantAccessSpaceSettings>()).LimitedAccessSpace)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }
    }
}