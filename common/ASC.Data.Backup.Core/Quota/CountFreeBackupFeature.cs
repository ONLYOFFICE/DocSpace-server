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
        var to = DateTime.UtcNow.AddSeconds(1);
        var from = to.AddMonths(-1);
        var result = await backupRepository.GetBackupsCountAsync(tenantId, false, from, to);
        return result;
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