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

using ASC.Core.Common.Quota;
using ASC.Core.Common.Quota.Features;

namespace ASC.Data.Backup.Core.Quota;

[Scope]
public class CountFreeBackupChecker(
    ITenantQuotaFeatureStat<CountFreeBackupFeature, int> tenantQuotaFeatureStatistic,
    CoreBaseSettings coreBaseSettings,
    TenantManager tenantManager,
    ITariffService tariffService)
    : TenantQuotaFeatureCheckerCount<CountFreeBackupFeature>(tenantQuotaFeatureStatistic, tenantManager)
{
    public override string GetExceptionMessage(long size)
    {
        return string.Format(Resource.TariffsFeature_free_backup_exception, size);
    }

    public override Task CheckUsed(TenantQuota quota)
    {
        //do not throw exception during the transition period from free to paid backup
        //TODO: remove method a month after release
        return Task.CompletedTask;
    }

    public override async Task CheckAddAsync(int tenantId, int newValue)
    {
        if (coreBaseSettings.Standalone)
        {
            return;
        }

        var tariff = await tariffService.GetTariffAsync(tenantId);
        if (tariff.State == TariffState.NotPaid)
        {
            return;
        }

        await base.CheckAddAsync(tenantId, newValue);
    }
}

[Scope]
public class CountFreeBackupStatistic(IServiceProvider serviceProvider) : ITenantQuotaFeatureStat<CountFreeBackupFeature, int>
{
    public async Task<int> GetValueAsync()
    {
        var tenantManager = serviceProvider.GetService<TenantManager>();
        var backupRepository = serviceProvider.GetService<BackupRepository>();
        var tenantId = tenantManager.GetCurrentTenantId();
        var (from, to) = BackupPeriodHelper.GetCurrentMonthRange();
        var result = await backupRepository.GetBackupsCountAsync(tenantId, false, from, to);
        return result;
    }
}

public static class BackupPeriodHelper
{
    public static (DateTime From, DateTime To) GetCurrentMonthRange()
    {
        var now = DateTime.UtcNow;
        var from = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = now.AddSeconds(1);
        return (from, to);
    }
}

public static class FreeBackupQuotaFeatureRegister
{
    public static void RegisterFreeBackupQuotaFeature(this IServiceCollection services)
    {
        services.AddScoped<ITenantQuotaFeatureChecker, CountFreeBackupChecker>();
        services.AddScoped<TenantQuotaFeatureChecker<CountFreeBackupFeature, int>, CountFreeBackupChecker>();
        services.AddScoped<ITenantQuotaFeatureStat<CountFreeBackupFeature, int>, CountFreeBackupStatistic>();
    }
}