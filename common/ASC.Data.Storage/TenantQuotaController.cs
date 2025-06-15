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

namespace ASC.Data.Storage;

[Transient]
public class TenantQuotaController(TenantManager tenantManager, AuthContext authContext, SettingsManager settingsManager, QuotaSocketManager quotaSocketManager,
        TenantQuotaFeatureChecker<MaxFileSizeFeature, long> maxFileSizeChecker,
        TenantQuotaFeatureChecker<MaxTotalSizeFeature, long> maxTotalSizeChecker)
    : IQuotaController
{
    private long CurrentSize
    {
        get
        {
            if (!_lazyCurrentSize.IsValueCreated)
            {
                return _currentSize = _lazyCurrentSize.Value;
            }

            return _currentSize;
        }
        set => _currentSize = value;
    }

    private int _tenant;
    private Lazy<long> _lazyCurrentSize;
    private long _currentSize;
    public string ExcludePattern { get; set; }

    public void Init(int tenant, string excludePattern = null)
    {
        _tenant = tenant;
        _lazyCurrentSize = new Lazy<long>(() => tenantManager.FindTenantQuotaRowsAsync(tenant).Result
            .Where(r => UsedInQuota(r.Tag))
            .Sum(r => r.Counter));
        ExcludePattern = excludePattern;
    }
    public async Task QuotaUserUsedAddAsync(string module, string domain, string dataTag, long size, Guid ownerId, bool quotaCheckFileSize = true)
    {
        size = Math.Abs(size);
        if (UsedInQuota(dataTag))
        {
            await QuotaUsedCheckAsync(size, quotaCheckFileSize, ownerId);
            CurrentSize += size;
        }
        await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, ownerId != Guid.Empty ? ownerId : authContext.CurrentAccount.ID);
        
    }
    public async Task QuotaUsedAddAsync(string module, string domain, string dataTag, long size, bool quotaCheckFileSize = true)
    {
        await QuotaUsedAddAsync(module, domain, dataTag, size, Guid.Empty, quotaCheckFileSize);
    }
    public async Task QuotaUsedAddAsync(string module, string domain, string dataTag, long size, Guid ownerId, bool quotaCheckFileSize = true)
    {
        size = Math.Abs(size);
        if (UsedInQuota(dataTag))
        {
            await QuotaUsedCheckAsync(size, quotaCheckFileSize, ownerId);
            CurrentSize += size;
        }

        await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, Guid.Empty);
        if (ownerId != Core.Configuration.Constants.CoreSystem.ID)
        {
            await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, ownerId != Guid.Empty ? ownerId : authContext.CurrentAccount.ID);
        }
    }
    public async Task QuotaUsedDeleteAsync(string module, string domain, string dataTag, long size)
    {
        await QuotaUsedDeleteAsync(module, domain, dataTag, size, Guid.Empty);
    }

    public async Task QuotaUsedDeleteAsync(string module, string domain, string dataTag, long size, Guid ownerId)
    {
        size = -Math.Abs(size);
        if (UsedInQuota(dataTag))
        {
            CurrentSize += size;
        }

        await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, Guid.Empty);
        if (ownerId != Core.Configuration.Constants.CoreSystem.ID)
        {
            await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, ownerId != Guid.Empty ? ownerId : authContext.CurrentAccount.ID);
        }
    }

    public async Task QuotaUserUsedDeleteAsync(string module, string domain, string dataTag, long size, Guid ownerId)
    {
        size = -Math.Abs(size);
        if (UsedInQuota(dataTag))
        {
            CurrentSize += size;
        }
        await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, ownerId != Guid.Empty ? ownerId : authContext.CurrentAccount.ID);

    }

    public async Task QuotaUsedSetAsync(string module, string domain, string dataTag, long size)
    {
        size = Math.Max(0, size);
        if (UsedInQuota(dataTag))
        {
            CurrentSize += size;
        }

        await SetTenantQuotaRowAsync(module, domain, size, dataTag, false, Guid.Empty);
    }

    public async Task QuotaUsedCheckAsync(long size, Guid ownedId)
    {
        await QuotaUsedCheckAsync(size, true, ownedId);
    }

    public async Task QuotaUsedCheckAsync(long size, bool quotaCheckFileSize, Guid ownerId)
    {
        var quota = await tenantManager.GetTenantQuotaAsync(_tenant);
        if (quota != null)
        {
            if (quota.MaxFileSize != 0 && quotaCheckFileSize)
            {
                await maxFileSizeChecker.CheckAddAsync(_tenant, size);
            }

            if (quota.MaxTotalSize != 0)
            {
                await maxTotalSizeChecker.CheckAddAsync(_tenant, CurrentSize + size);
            }
        }
        var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
        if (tenantQuotaSetting.EnableQuota)
        {
            if (tenantQuotaSetting.Quota < CurrentSize + size)
            {
                if ((tenantQuotaSetting.Quota * 2 < CurrentSize + size) || tenantQuotaSetting.Quota < CurrentSize)
                {
                    throw new TenantQuotaException(maxTotalSizeChecker.GetExceptionMessage(tenantQuotaSetting.Quota));
                }
                await quotaSocketManager.TenantQuotaExceededAsync();
            }
        }
    }


    private async Task SetTenantQuotaRowAsync(string module, string domain, long size, string dataTag, bool exchange, Guid userId)
    {
        await tenantManager.SetTenantQuotaRowAsync(
            new TenantQuotaRow { TenantId = _tenant, Path = $"/{module}/{domain}", Counter = size, Tag = dataTag, UserId = userId, LastModified = DateTime.UtcNow },
            exchange);

    }

    private bool UsedInQuota(string tag)
    {
        return !string.IsNullOrEmpty(tag) && new Guid(tag) != Guid.Empty;
    }
}
