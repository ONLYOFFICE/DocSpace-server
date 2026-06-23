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

namespace ASC.Core.Common.Quota;
public interface ITenantQuotaFeatureChecker
{
    Task CheckUsed(TenantQuota value);

    string GetExceptionMessage(long size);
}


public abstract class TenantQuotaFeatureChecker<T, T1>(ITenantQuotaFeatureStat<T, T1> tenantQuotaFeatureStatistic,
        TenantManager tenantManager)
    : ITenantQuotaFeatureChecker
    where T : TenantQuotaFeature<T1>
    where T1 : IComparable<T1>
{
    protected readonly ITenantQuotaFeatureStat<T, T1> _tenantQuotaFeatureStatistic = tenantQuotaFeatureStatistic;

    public abstract string GetExceptionMessage(long size);

    public virtual async Task CheckUsed(TenantQuota quota)
    {
        var used = await _tenantQuotaFeatureStatistic.GetValueAsync();
        Check(quota, used);
    }

    public virtual async Task CheckAddAsync(int tenantId, T1 newValue)
    {
        var quota = await tenantManager.GetTenantQuotaAsync(tenantId);
        Check(quota, newValue);
    }

    protected async Task CheckAddAsync(T1 newValue)
    {
        await CheckAddAsync(tenantManager.GetCurrentTenantId(), newValue);
    }

    private void Check(TenantQuota quota, T1 newValue)
    {
        var val = quota.GetFeature<T>().Value;

        if (newValue.CompareTo(val) > 0)
        {
            throw new TenantQuotaException(GetExceptionMessage((long)Convert.ChangeType(val, typeof(long))));
        }
    }
}

public abstract class TenantQuotaFeatureCheckerCount<T>(ITenantQuotaFeatureStat<T, int> tenantQuotaFeatureStatistic,
        TenantManager tenantManager)
    : TenantQuotaFeatureChecker<T, int>(tenantQuotaFeatureStatistic, tenantManager)
    where T : TenantQuotaFeature<int>
{
    public async Task CheckAppend()
    {
        await CheckAddAsync(await _tenantQuotaFeatureStatistic.GetValueAsync() + 1);
    }
}