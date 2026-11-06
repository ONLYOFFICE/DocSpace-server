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

using Amazon;

using ASC.Data.Backup.Services;
using ASC.Data.Storage.Encryption.IntegrationEvents.Events;

namespace ASC.Web.Api.Controllers.Settings;

public class StorageController(
    ILoggerFactory loggerFactory,
        ServiceClient serviceClient,
        MessageService messageService,
        SecurityContext securityContext,
        StudioNotifyService studioNotifyService,
        TenantManager tenantManager,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        StorageSettingsHelper storageSettingsHelper,
        IWebHostEnvironment webHostEnvironment,
        ConsumerFactory consumerFactory,
        IFusionCache fusionCache,
        IEventBus eventBus,
        EncryptionSettingsHelper encryptionSettingsHelper,
        BackupService backupService,
        ICacheNotify<DeleteSchedule> cacheDeleteSchedule,
        EncryptionWorker encryptionWorker,
        IDistributedLockProvider distributedLockProvider,
        TenantExtra tenantExtra)
    : BaseSettingsController(fusionCache, webItemManager)
{
    private readonly ILogger _log = loggerFactory.CreateLogger("ASC.Api");

    /// <remarks>
    /// Returns the third-party storages that can keep the portal data, such as Amazon S3 or Google Cloud Storage. The
    /// built-in local storage is not in the list. If no entry is `current`, the portal keeps its data in the local
    /// storage. Each entry shows its keys and whether the server has them set (`isSet`). Keys of the current storage
    /// come from the saved settings. Keys of other storages come from the provider configuration and are empty if
    /// nobody set them. Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have.
    /// Other callers get 403. The call also returns 403 if the installation is not self-hosted or limits the access
    /// space. The call is read-only. To switch the storage, use `PUT api/2.0/settings/storage`. To return to the local
    /// storage, use `DELETE api/2.0/settings/storage`. For the CDN and backup storages, use
    /// `GET api/2.0/settings/storage/cdn` and `GET api/2.0/settings/storage/backup`.
    /// </remarks>
    /// <summary>
    /// Get the portal storages
    /// </summary>
    /// <path>api/2.0/settings/storage</path>
    /// <collection>list</collection>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The storages available to the portal, each marked as configured and as currently in use", typeof(List<StorageDto>))]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpGet("storage")]
    public async Task<List<StorageDto>> GetAllStorages()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantExtra.DemandAccessSpacePermissionAsync();

        var current = await settingsManager.LoadAsync<StorageSettings>();
        var consumers = consumerFactory.GetAll<DataStoreConsumer>();
        List<StorageDto> result = [];
        foreach (var consumer in consumers)
        {
            result.Add(await StorageDto.StorageWrapperInit(consumer, current));
        }
        return result;
    }

    /// <remarks>
    /// Returns the progress of moving the portal data to another storage, as a percentage from 0 to 100. To start the
    /// move, use `PUT api/2.0/settings/storage` or `DELETE api/2.0/settings/storage`. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call works even if the
    /// portal is not paid. The call is read-only. This is the call to poll. Repeat it until the value reaches 100. Then
    /// the portal works from the new storage. If the server has no migration for the portal, the call fails with a
    /// server error. On an installation that is not self-hosted, the call returns -1. The value does not show migration
    /// errors. If it stops growing, check the portal state with `GET api/2.0/portal`.
    /// </remarks>
    /// <summary>
    /// Get the storage migration progress
    /// </summary>
    /// <path>api/2.0/settings/storage/progress</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "Migration progress as a percentage, or -1 where storage migration is not offered", typeof(double))]
    [SwaggerResponse(403, "The caller has no portal-settings right")]
    [AllowNotPayment]
    [HttpGet("storage/progress")]
    public async Task<double> GetStorageProgress()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!coreBaseSettings.Standalone)
        {
            return -1;
        }

        var tenant = tenantManager.GetCurrentTenant();
        return serviceClient.GetProgress(tenant.Id);
    }

    /// <remarks>
    /// Starts encryption of the local storage of the installation, or decryption if the data is already encrypted. The
    /// saved encryption state sets the direction. The run covers all portals of the installation. First, reset a
    /// third-party storage with `DELETE api/2.0/settings/storage` and a CDN with `DELETE api/2.0/settings/storage/cdn`.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call also returns 403 if the installation is not self-hosted or limits the access space. The call
    /// queues work and deletes the backup schedules of all portals. Every portal stays unavailable until the run ends.
    /// Repeat `GET api/2.0/settings/encryption/progress` until it returns 100. Do not repeat this call during a run.
    /// The server generates the password and never returns it. To email every user before the portals go down, set
    /// `notifyUsers` to true. The call returns `true` when the run is queued. In a custom build, it returns `false` and
    /// starts nothing. The call returns 415 if the installation does not offer encryption. It also returns 415 if the
    /// current portal uses a third-party storage or CDN. If a backup of any portal is running, the call returns 500. If
    /// the portal is not paid, the call returns 402.
    /// </remarks>
    /// <summary>
    /// Start the storage encryption
    /// </summary>
    /// <path>api/2.0/settings/encryption/start</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "True when the encryption job has been queued; false in a build where storage encryption is switched off", typeof(bool))]
    [SwaggerResponse(402, "The portal pricing plan does not include storage encryption")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow storage encryption")]
    [SwaggerResponse(415, "Storage encryption is not available on this installation, or the storage or the CDN of the current portal is a third-party provider")]
    [SwaggerResponse(500, "A backup of one of the portals is still running")]
    [HttpPost("encryption/start")]
    public async Task<bool> StartStorageEncryption(StorageEncryptionRequestsDto inDto)
    {
        if (coreBaseSettings.CustomMode)
        {
            return false;
        }

        await using (await distributedLockProvider.TryAcquireFairLockAsync("start_storage_encryption"))
        {
            var activeTenants = await tenantManager.GetTenantsAsync();

            if (activeTenants.Count > 0)
            {
                await StartEncryptionAsync(inDto.NotifyUsers);
            }
        }

        return true;
    }

    private async Task StartEncryptionAsync(bool notifyUsers)
    {
        if (!SetupInfo.IsVisibleSettings<EncryptionSettings>())
        {
            throw new NotSupportedException();
        }

        if (!coreBaseSettings.Standalone)
        {
            throw new SecurityException(Resource.ErrorAccessDenied);
        }

        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantExtra.DemandAccessSpacePermissionAsync();

        var storages = await GetAllStorages();

        if (storages.Exists(s => s.Current))
        {
            throw new NotSupportedException();
        }

        var cdnStorages = await GetAllCdnStorages();

        if (cdnStorages.Exists(s => s.Current))
        {
            throw new NotSupportedException();
        }

        var tenants = await tenantManager.GetTenantsAsync();

        foreach (var tenant in tenants)
        {
            var progress = await backupService.GetBackupProgressAsync(tenant.Id);
            if (progress is { IsCompleted: false })
            {
                throw new Exception();
            }
        }

        foreach (var tenant in tenants)
        {
            await cacheDeleteSchedule.PublishAsync(new DeleteSchedule { TenantId = tenant.Id }, CacheNotifyAction.Insert);
        }

        var settings = await encryptionSettingsHelper.LoadAsync();

        settings.NotifyUsers = notifyUsers;

        if (settings.Status == EncryptionStatus.Decrypted)
        {
            settings.Status = EncryptionStatus.EncryptionStarted;
            settings.Password = encryptionSettingsHelper.GeneratePassword(32, 16);
        }
        else if (settings.Status == EncryptionStatus.Encrypted)
        {
            settings.Status = EncryptionStatus.DecryptionStarted;
        }

        messageService.Send(settings.Status == EncryptionStatus.EncryptionStarted ? MessageAction.StartStorageEncryption : MessageAction.StartStorageDecryption);

        var serverRootPath = commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

        foreach (var tenant in tenants)
        {
            tenantManager.SetCurrentTenant(tenant);

            if (notifyUsers)
            {
                if (settings.Status == EncryptionStatus.EncryptionStarted)
                {
                    await studioNotifyService.SendStorageEncryptionStartAsync(serverRootPath);
                }
                else
                {
                    await studioNotifyService.SendStorageDecryptionStartAsync(serverRootPath);
                }
            }

            tenant.SetStatus(TenantStatus.Encryption);
            await tenantManager.SaveTenantAsync(tenant);
        }

        await encryptionSettingsHelper.SaveAsync(settings);

        await eventBus.PublishAsync(new DataStorageEncryptionIntegrationEvent
        (
              encryptionSettings: new EncryptionSettings
              {
                  NotifyUsers = settings.NotifyUsers,
                  Password = settings.Password,
                  Status = settings.Status
              },
              serverRootPath: serverRootPath,
              createBy: securityContext.CurrentAccount.ID,
              tenantId: tenantManager.GetCurrentTenantId()

        ));
    }

    /// <remarks>
    /// Returns the encryption state of the installation storage and whether users get an email when a run starts. The
    /// response never includes the encryption password. Requires the EditPortalSettings permission, which the portal
    /// owner and DocSpace admins have. Other callers get an empty body, not an error. The call also returns an empty
    /// body if the installation is not self-hosted. The same happens if it has no encryption or limits the access
    /// space. It returns an empty body on any other error too. So an empty body does not mean that encryption is off.
    /// The call is read-only. To follow a run, use `GET api/2.0/settings/encryption/progress`. To start one, use
    /// `POST api/2.0/settings/encryption/start`.
    /// </remarks>
    /// <summary>
    /// Get the storage encryption settings
    /// </summary>
    /// <path>api/2.0/settings/encryption/settings</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "The encryption status and the notify-users flag; empty on a custom-mode installation, or when the caller has no portal-settings right, the installation hides storage encryption or does not grant unrestricted space access, or the settings cannot be read", typeof(EncryptionSettingsDto))]
    [HttpGet("encryption/settings")]
    public async Task<EncryptionSettingsDto> GetStorageEncryptionSettings()
    {
        try
        {
            if (coreBaseSettings.CustomMode)
            {
                return null;
            }

            if (!SetupInfo.IsVisibleSettings<EncryptionSettings>())
            {
                throw new NotSupportedException();
            }

            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

            await tenantExtra.DemandAccessSpacePermissionAsync();

            var settings = await encryptionSettingsHelper.LoadAsync();

            return settings.Map();
        }
        catch (Exception e)
        {
            _log.ErrorGetStorageEncryptionSettings(e);
            return null;
        }
    }

    /// <remarks>
    /// Returns the progress of the encryption or decryption run of the installation storage, as a percentage from 0
    /// to 100. To start a run, use `POST api/2.0/settings/encryption/start`. To see its direction, use
    /// `GET api/2.0/settings/encryption/settings`. Any signed-in user can call it. The call is read-only. This is the
    /// call to poll. Repeat it until the value reaches 100. The server keeps the final value for one minute after the
    /// run ends. After that, and when no run exists, the response is empty. So poll at least once a minute. In a custom
    /// build, the call returns -1. The call returns 415 if the installation does not offer encryption or is not
    /// self-hosted.
    /// </remarks>
    /// <summary>
    /// Get the storage encryption progress
    /// </summary>
    /// <path>api/2.0/settings/encryption/progress</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "Encryption or decryption progress as a percentage, or empty when no run is in flight", typeof(double?))]
    [SwaggerResponse(415, "Storage encryption is not available on this installation")]
    [HttpGet("encryption/progress")]
    public async Task<double?> GetStorageEncryptionProgress()
    {
        if (coreBaseSettings.CustomMode)
        {
            return -1;
        }

        if (!SetupInfo.IsVisibleSettings<EncryptionSettings>())
        {
            throw new NotSupportedException();
        }

        if (!coreBaseSettings.Standalone)
        {
            throw new NotSupportedException();
        }

        return await encryptionWorker.GetEncryptionProgress();
    }

    /// <remarks>
    /// Switches the portal to another storage and starts moving the portal data to it. Set `module` to an entry of
    /// `GET api/2.0/settings/storage` and `props` to the keys of that provider. Requires the EditPortalSettings
    /// permission, which the portal owner and DocSpace admins have. Other callers get 403. The call also returns 403 if
    /// the installation is not self-hosted or limits the access space. The call queues work. The portal stays
    /// unavailable until the move ends. Repeat `GET api/2.0/settings/storage/progress` until it returns 100. Do not
    /// send another switch during a move. If `module` is already the current storage, the call changes nothing and
    /// returns the current settings. Otherwise, it returns the requested settings, not the state of the move. If the
    /// provider of `module` is not set on the server (`isSet` is false), the call returns 400. To return to the local
    /// storage, use `DELETE api/2.0/settings/storage`. To select a CDN, use `PUT api/2.0/settings/storage/cdn`.
    /// </remarks>
    /// <summary>
    /// Switch the portal storage
    /// </summary>
    /// <path>api/2.0/settings/storage</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The saved storage configuration; migration of the portal data to it has been started", typeof(StorageSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `module`, or the requested storage module is unknown or not configured on this installation")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpPut("storage")]
    public async Task<StorageSettingsDto> UpdateStorage(StorageRequestsDto inDto)
    {
        try
        {
            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

            await tenantExtra.DemandAccessSpacePermissionAsync();

            var consumer = consumerFactory.GetByKey(inDto.Module);
            if (!await consumer.GetIsSetAsync())
            {
                throw new ArgumentException("module");
            }

            var settings = await settingsManager.LoadAsync<StorageSettings>();
            if (settings.Module == inDto.Module)
            {
                return settings.Map();
            }

            settings.Module = inDto.Module;
            settings.Props = inDto.Props.ToDictionary(r => r.Key, b => b.Value);

            await StartMigrateAsync(settings);
            return settings.Map();
        }
        catch (Exception e)
        {
            _log.ErrorUpdateStorage(e);
            throw;
        }
    }

    /// <remarks>
    /// Clears the third-party storage of the portal and starts moving the portal data back to the local storage.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call also returns 403 if the installation is not self-hosted or limits the access space. The call
    /// queues work and returns no body. The portal stays unavailable until the move ends. Repeat
    /// `GET api/2.0/settings/storage/progress` until it returns 100. Do not repeat this call during a move. Each call
    /// starts a new move. The move copies the documents and does not delete them. The call deletes the saved keys of
    /// the previous storage. To switch back, send them again with `PUT api/2.0/settings/storage`. Before
    /// `POST api/2.0/settings/encryption/start`, reset the storage with this call.
    /// </remarks>
    /// <summary>
    /// Reset the storage settings
    /// </summary>
    /// <path>api/2.0/settings/storage</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The storage configuration has been cleared and migration back to the local storage has started")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpDelete("storage")]
    public async Task ResetStorageToDefault()
    {
        try
        {
            await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

            await tenantExtra.DemandAccessSpacePermissionAsync();

            var settings = await settingsManager.LoadAsync<StorageSettings>();

            settings.Module = null;
            settings.Props = null;


            await StartMigrateAsync(settings);
        }
        catch (Exception e)
        {
            _log.ErrorResetStorageToDefault(e);
            throw;
        }
    }

    /// <remarks>
    /// Returns the storages that can serve the static content of the portal through a CDN. The list has only the
    /// providers of `GET api/2.0/settings/storage` that offer a CDN. Each entry shows its keys, whether the server has
    /// them set (`isSet`), and whether the portal uses it (`current`). Keys of the current entry come from the saved
    /// CDN settings. If no entry is `current`, the portal serves its static content itself. An empty list means that
    /// the build has no CDN provider. Requires the EditPortalSettings permission, which the portal owner and DocSpace
    /// admins have. Other callers get 403. The call also returns 403 if the installation is not self-hosted or limits
    /// the access space. The call is read-only. To select a CDN, use `PUT api/2.0/settings/storage/cdn`. To stop using
    /// one, use `DELETE api/2.0/settings/storage/cdn`.
    /// </remarks>
    /// <summary>
    /// Get the CDN storages
    /// </summary>
    /// <path>api/2.0/settings/storage/cdn</path>
    /// <collection>list</collection>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The storages that can serve as the portal CDN, with the one in use marked as current", typeof(List<StorageDto>))]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpGet("storage/cdn")]
    public async Task<List<StorageDto>> GetAllCdnStorages()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantExtra.DemandAccessSpacePermissionAsync();

        var current = await settingsManager.LoadAsync<CdnStorageSettings>();
        var consumers = consumerFactory.GetAll<DataStoreConsumer>().Where(r => r.Cdn != null);
        List<StorageDto> result = [];
        foreach (var consumer in consumers)
        {
            result.Add(await StorageDto.StorageWrapperInit(consumer, current));
        }
        return result;
    }

    /// <remarks>
    /// Selects the CDN that serves the static content of the portal and starts copying the content to it. Set `module`
    /// to an entry of `GET api/2.0/settings/storage/cdn` and `props` to the keys of that provider. Requires the
    /// EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers get 403. The call
    /// also returns 403 if the installation is not self-hosted or limits the access space. The call queues work. No
    /// call shows its progress. The content appears on the CDN after the server copies it. You can repeat it. If
    /// `module` is already the current CDN, the call changes nothing and returns the current settings. Otherwise, it
    /// returns the requested CDN settings. If the provider of `module` is not set on the server (`isSet` is false), the
    /// call returns 400. The call does not move documents. To move them, use `PUT api/2.0/settings/storage`.
    /// </remarks>
    /// <summary>
    /// Update the CDN storage
    /// </summary>
    /// <path>api/2.0/settings/storage/cdn</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The saved CDN configuration; the upload of the static content has been handed to the storage service", typeof(StorageSettingsDto))]
    [SwaggerResponse(400, "The request body cannot be read or has no `module`, or the requested CDN module is unknown or not configured on this installation")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpPut("storage/cdn")]
    public async Task<StorageSettingsDto> UpdateCdnStorage(StorageRequestsDto inDto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantExtra.DemandAccessSpacePermissionAsync();

        var consumer = consumerFactory.GetByKey(inDto.Module);
        if (!await consumer.GetIsSetAsync())
        {
            throw new ArgumentException("module");
        }

        var settings = await settingsManager.LoadAsync<CdnStorageSettings>();
        if (settings.Module == inDto.Module)
        {
            return settings.Map();
        }

        settings.Module = inDto.Module;
        settings.Props = inDto.Props.ToDictionary(r => r.Key, b => b.Value);

        try
        {
            var tenant = tenantManager.GetCurrentTenant();
            await serviceClient.UploadCdnAsync(tenant.Id, "/", webHostEnvironment.ContentRootPath, settings);
        }
        catch (Exception e)
        {
            _log.ErrorUpdateCdn(e);
            throw;
        }

        return settings.Map();
    }

    /// <remarks>
    /// Clears the CDN settings of the portal, so the portal serves its static content itself again. The call does not
    /// upload, move, or delete files. Files on the CDN stay there until you delete them in the provider console. Links
    /// that the portal builds after the call no longer use the CDN. An open page may use the CDN until you reload it.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. The call also returns 403 if the installation is not self-hosted or limits the access space. The call
    /// changes data and returns no body. You can repeat it. To see the current CDN, use
    /// `GET api/2.0/settings/storage/cdn`. To select a CDN again, use `PUT api/2.0/settings/storage/cdn`. To reset the
    /// document storage, use `DELETE api/2.0/settings/storage`.
    /// </remarks>
    /// <summary>
    /// Reset the CDN storage settings
    /// </summary>
    /// <path>api/2.0/settings/storage/cdn</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The CDN configuration has been cleared and static content is served by the portal again")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpDelete("storage/cdn")]
    public async Task ResetCdnToDefault()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantExtra.DemandAccessSpacePermissionAsync();

        await storageSettingsHelper.ClearAsync(await settingsManager.LoadAsync<CdnStorageSettings>());
    }

    /// <remarks>
    /// Returns the storages that can keep portal backups. The storage of the saved backup schedule is `current`, with
    /// its keys from the schedule. If no schedule exists or it uses no third-party storage, no entry is `current`.
    /// Requires the EditPortalSettings permission, which the portal owner and DocSpace admins have. Other callers
    /// get 403. If the installation is not self-hosted and offers no backup, the call also returns 403. The call is
    /// read-only. It does not show where the portal keeps its data. To change that storage, use
    /// `PUT api/2.0/settings/storage`.
    /// </remarks>
    /// <summary>
    /// Get the backup storages
    /// </summary>
    /// <path>api/2.0/settings/storage/backup</path>
    /// <collection>list</collection>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The storages that can hold portal backups, with the scheduled one marked as current", typeof(List<StorageDto>))]
    [SwaggerResponse(403, "The caller may not edit portal settings, or backup is not available on this installation")]
    [HttpGet("storage/backup")]
    public async Task<List<StorageDto>> GetAllBackupStorages(AllBackupStoragesDto dto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var schedule = await backupService.GetScheduleAsync(dto.Dump);
        var current = new StorageSettings();

        if (schedule is { StorageType: BackupStorageType.ThirdPartyConsumer })
        {
            current = new StorageSettings
            {
                Module = schedule.StorageParams["module"],
                Props = schedule.StorageParams.Where(r => r.Key != "module").ToDictionary(r => r.Key, r => r.Value)
            };
        }

        var consumers = consumerFactory.GetAll<DataStoreConsumer>();
        List<StorageDto> result = [];
        foreach (var consumer in consumers)
        {
            result.Add(await StorageDto.StorageWrapperInit(consumer, current));
        }
        return result;
    }

    private async Task StartMigrateAsync(StorageSettings settings)
    {
        var tenant = tenantManager.GetCurrentTenant();
        await serviceClient.MigrateAsync(tenant.Id, settings);

        tenant.SetStatus(TenantStatus.Migrating);
        await tenantManager.SaveTenantAsync(tenant);
    }

    /// <remarks>
    /// Returns the Amazon regions that the server knows, with their display names and partition details. The list is
    /// built into the server, so it is the same for every portal. It changes only with a server update, so you can
    /// cache it. Use the system name of an entry as the region in `props` of an Amazon S3 storage. This applies to
    /// `PUT api/2.0/settings/storage`, `PUT api/2.0/settings/storage/cdn`, and a backup schedule. Pick the region from
    /// this list. Any signed-in user can call it. The call is read-only. The list has no paging or filters.
    /// </remarks>
    /// <summary>
    /// Get the Amazon S3 regions
    /// </summary>
    /// <path>api/2.0/settings/storage/s3/regions</path>
    /// <collection>list</collection>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The Amazon regions known to this installation", typeof(IEnumerable<AmazonS3RegionDto>))]
    [HttpGet("storage/s3/regions")]
    public IEnumerable<AmazonS3RegionDto> GetAmazonS3Regions()
    {
        return RegionEndpoint.EnumerableAllRegions.Select(r => new AmazonS3RegionDto
        {
            SystemName = r.SystemName,
            DisplayName = r.DisplayName,
            PartitionName = r.PartitionName,
            PartitionDnsSuffix = r.PartitionDnsSuffix,
            PartitionRegionRegex = r.PartitionRegionRegex,
            HostnameTemplate = r.HostnameTemplate
        });
    }
}