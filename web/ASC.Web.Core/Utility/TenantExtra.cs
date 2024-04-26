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

    public bool Saas
    {
        get => tenantExtraConfig.Saas;
    }

    public bool Enterprise
    {
        get => tenantExtraConfig.Enterprise;
    }

    public bool Opensource
    {
        get => tenantExtraConfig.Opensource;
    }

    private async Task<bool> EnterprisePaidAsync(bool withRequestToPaymentSystem = true)
    {
        return Enterprise && (await GetCurrentTariffAsync(withRequestToPaymentSystem)).State < TariffState.NotPaid;
    }

    public async Task<Tariff> GetCurrentTariffAsync(bool withRequestToPaymentSystem = true, bool refresh = false)
    {
        return await tariffService.GetTariffAsync(await tenantManager.GetCurrentTenantIdAsync(), withRequestToPaymentSystem, refresh);
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
        
        return tariff.State >= TariffState.NotPaid || Enterprise && !(await EnterprisePaidAsync(withRequestToPaymentSystem)) && tariff.LicenseDate == DateTime.MaxValue;
    }

    public async Task DemandAccessSpacePermissionAsync()
    {
        if (!coreBaseSettings.Standalone || (await settingsManager.LoadAsync<TenantAccessSpaceSettings>()).LimitedAccessSpace)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }
    }
}
