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

namespace ASC.Data.Storage.Migration;

[Singleton]
public class ServiceClientListener
{
    private readonly ICacheNotify<MigrationProgress> _progressMigrationNotify;
    private readonly ICache _cache;

    public ServiceClientListener(
        ICacheNotify<MigrationProgress> progressMigrationNotify,
        ICache cache)
    {
        _progressMigrationNotify = progressMigrationNotify;
        _cache = cache;

        ProgressListening();
    }

    public MigrationProgress GetProgress(int tenantId)
    {
        return _cache.Get<MigrationProgress>(GetCacheKey(tenantId));
    }

    private void ProgressListening()
    {
        _progressMigrationNotify.Subscribe(n =>
        {
            var migrationProgress = new MigrationProgress
            {
                TenantId = n.TenantId,
                Progress = n.Progress,
                IsCompleted = n.IsCompleted,
                Error = n.Error
            };

            _cache.Insert(GetCacheKey(n.TenantId), migrationProgress, DateTime.MaxValue);
        },
           CacheNotifyAction.Insert);
    }

    private string GetCacheKey(int tenantId)
    {
        return typeof(MigrationProgress).FullName + tenantId;
    }
}

[Scope]
public class ServiceClient(ServiceClientListener serviceClientListener,
        ICacheNotify<MigrationCache> cacheMigrationNotify,
        ICacheNotify<MigrationUploadCdn> uploadCdnMigrationNotify)
    : IService
{
    public ServiceClientListener ServiceClientListener { get; } = serviceClientListener;
    public ICacheNotify<MigrationCache> CacheMigrationNotify { get; } = cacheMigrationNotify;
    public ICacheNotify<MigrationUploadCdn> UploadCdnMigrationNotify { get; } = uploadCdnMigrationNotify;

    public async Task MigrateAsync(int tenant, StorageSettings storageSettings)
    {
        var storSettings = new StorSettings { Id = StorageSettings.ID.ToString(), Module = storageSettings.Module };

        await CacheMigrationNotify.PublishAsync(new MigrationCache
        {
            TenantId = tenant,
            StorSettings = storSettings
        }, CacheNotifyAction.Insert);
    }

    public async Task UploadCdnAsync(int tenantId, string relativePath, string mappedPath, CdnStorageSettings settings = null)
    {
        var cdnStorSettings = new CdnStorSettings { Id = CdnStorageSettings.ID.ToString(), Module = settings.Module };

        await UploadCdnMigrationNotify.PublishAsync(new MigrationUploadCdn
        {
            Tenant = tenantId,
            RelativePath = relativePath,
            MappedPath = mappedPath,
            CdnStorSettings = cdnStorSettings
        }, CacheNotifyAction.Insert);
    }

    public double GetProgress(int tenant)
    {
        var migrationProgress = ServiceClientListener.GetProgress(tenant);

        return migrationProgress.Progress;
    }

    public async Task StopMigrateAsync()
    {
        await CacheMigrationNotify.PublishAsync(new MigrationCache(), CacheNotifyAction.InsertOrUpdate);
    }
}