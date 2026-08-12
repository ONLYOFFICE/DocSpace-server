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

namespace ASC.Data.Storage;

[Singleton]
public class TenantQuotaConfig(IConfiguration configuration)
{
    // Same config key/default as SetupInfo.AvailableFileSize (ASC.Web.Core is not referenceable from this layer).
    public long AvailableFileSize { get; } = long.TryParse(configuration["web:available-file-size"], out var value)
        ? value
        : Constants.DefaultAvailableFileSize;
}

[Transient]
public class TenantQuotaController(TenantManager tenantManager, AuthContext authContext, SettingsManager settingsManager, QuotaSocketManager quotaSocketManager,
        TenantQuotaFeatureChecker<MaxFileSizeFeature, long> maxFileSizeChecker,
        TenantQuotaFeatureChecker<MaxTotalSizeFeature, long> maxTotalSizeChecker,
        TenantQuotaConfig tenantQuotaConfig)
    : IQuotaController
{
    // CurrentSize is a mutable running total shared by every QuotaUsed*/QuotaUser* call. Some storage
    // operations fan those calls out concurrently (e.g. FileDao batched delete, S3 CopyDirectoryAsync),
    // so both the lazy seeding and the +=/read must be serialized. The lock only ever guards the
    // in-memory arithmetic below - never the DB write or storage I/O (an await under lock is a compile
    // error), so it does not serialize the parts that make the fan-out worthwhile.
    private readonly Lock _sizeLock = new();
    private long _currentSize;

    private long CurrentSize
    {
        get
        {
            lock (_sizeLock)
            {
                EnsureCurrentSizeSeeded();
                return _currentSize;
            }
        }
    }

    // must be called under _sizeLock
    private void EnsureCurrentSizeSeeded()
    {
        if (!_lazyCurrentSize.IsValueCreated)
        {
            _currentSize = _lazyCurrentSize.Value;
        }
    }

    private void AddCurrentSize(long delta)
    {
        lock (_sizeLock)
        {
            EnsureCurrentSizeSeeded();
            _currentSize += delta;
        }
    }

    // Single-threaded warm-up: materializes the DB running total before a concurrent fan-out so the
    // lock above only ever wraps arithmetic, never the blocking Lazy DB query.
    public void PreloadCurrentSize()
    {
        _ = CurrentSize;
    }

    private int _tenant;
    private Lazy<long> _lazyCurrentSize;
    public string ExcludePattern { get; set; }

    public void Init(int tenant, string excludePattern = null)
    {
        _tenant = tenant;
        _lazyCurrentSize = new Lazy<long>(() => tenantManager.FindTenantQuotaRowsAsync(tenant).Result
            .Where(r => UsedInQuota(r.Tag))
            .Sum(r => r.Counter));
        ExcludePattern = excludePattern;
    }
    public async Task QuotaUserUsedAddAsync(string module, string domain, string dataTag, long size, Guid ownerId, bool quotaCheckFileSize = true, bool allowQuotaGrace = false)
    {
        size = Math.Abs(size);
        if (UsedInQuota(dataTag))
        {
            var result = await QuotaUsedCheckAsync(size, quotaCheckFileSize, ownerId, allowQuotaGrace);
            AddCurrentSize(size);
            if (result == QuotaCheckResult.QuotaExceeded)
            {
                await quotaSocketManager.TenantQuotaExceededAsync();
            }
        }
        await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, ownerId != Guid.Empty ? ownerId : authContext.CurrentAccount.ID);

    }
    public async Task QuotaUsedAddAsync(string module, string domain, string dataTag, long size, bool quotaCheckFileSize = true)
    {
        await QuotaUsedAddAsync(module, domain, dataTag, size, Guid.Empty, quotaCheckFileSize);
    }
    public async Task QuotaUsedAddAsync(string module, string domain, string dataTag, long size, Guid ownerId, bool quotaCheckFileSize = true, bool allowQuotaGrace = false)
    {
        size = Math.Abs(size);
        if (UsedInQuota(dataTag))
        {
            var result = await QuotaUsedCheckAsync(size, quotaCheckFileSize, ownerId, allowQuotaGrace);
            AddCurrentSize(size);
            if (result == QuotaCheckResult.QuotaExceeded)
            {
                await quotaSocketManager.TenantQuotaExceededAsync();
            }
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
            AddCurrentSize(size);
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
            AddCurrentSize(size);
        }
        await SetTenantQuotaRowAsync(module, domain, size, dataTag, true, ownerId != Guid.Empty ? ownerId : authContext.CurrentAccount.ID);

    }

    public async Task QuotaUsedSetAsync(string module, string domain, string dataTag, long size)
    {
        size = Math.Max(0, size);
        if (UsedInQuota(dataTag))
        {
            AddCurrentSize(size);
        }

        await SetTenantQuotaRowAsync(module, domain, size, dataTag, false, Guid.Empty);
    }

    public async Task QuotaUsedCheckAsync(long size, Guid ownedId, bool allowQuotaGrace = false)
    {
        await QuotaUsedCheckAsync(size, true, ownedId, allowQuotaGrace);
    }

    public async Task<QuotaCheckResult> QuotaUsedCheckAsync(long size, bool quotaCheckFileSize, Guid ownerId, bool allowQuotaGrace = false)
    {
        var result = QuotaCheckResult.Ok;

        var quota = await tenantManager.GetTenantQuotaAsync(_tenant);
        if (quota != null)
        {
            if (quota.MaxFileSize != 0 && quotaCheckFileSize)
            {
                await maxFileSizeChecker.CheckAddAsync(_tenant, size);
            }

            if (quota.MaxTotalSize != 0)
            {
                result = await CheckMaxTotalSizeAsync(quota, CurrentSize + size, allowQuotaGrace);
            }
        }
        var tenantQuotaSetting = await settingsManager.LoadAsync<TenantQuotaSettings>();
        if (!tenantQuotaSetting.EnableQuota)
        {
            return result;
        }

        if ((CurrentSize + size > 2 * tenantQuotaSetting.Quota )
                || CurrentSize > tenantQuotaSetting.Quota)
        {
            throw new TenantQuotaException(
                maxTotalSizeChecker.GetExceptionMessage(tenantQuotaSetting.Quota));
        }

        if (CurrentSize + size > tenantQuotaSetting.Quota)
        {
            return QuotaCheckResult.QuotaExceeded;
        }

        return result;
    }

    // Enforces the portal (tariff) MaxTotalSize quota. Throws on a hard failure; otherwise returns whether
    // the write is a soft overshoot within the grace (QuotaExceeded) or fits (Ok).
    // Strict for non-file-save writes (avatars, thumbnails, backups, ...); editor saves (allowQuotaGrace)
    // may overshoot within tenantQuotaConfig.AvailableFileSize, but only once - hard-fail once already over
    // the limit or beyond the grace.
    private async Task<QuotaCheckResult> CheckMaxTotalSizeAsync(TenantQuota quota, long newTotal, bool allowQuotaGrace)
    {
        if (!allowQuotaGrace || CurrentSize > quota.MaxTotalSize || newTotal > quota.MaxTotalSize + tenantQuotaConfig.AvailableFileSize)
        {
            await maxTotalSizeChecker.CheckAddAsync(_tenant, newTotal); // throws when newTotal exceeds MaxTotalSize
            return QuotaCheckResult.Ok;
        }

        // soft overshoot within grace: caller fires the socket notification. The file-save path passes
        // allowQuotaGrace and serializes this via a tenant-scoped lock (see
        // FileDao.TryAllowTenantQuotaGraceAsync) so it is consumed once.
        return newTotal > quota.MaxTotalSize ? QuotaCheckResult.QuotaExceeded : QuotaCheckResult.Ok;
    }

    public enum QuotaCheckResult
    {
        Ok,
        QuotaExceeded
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
