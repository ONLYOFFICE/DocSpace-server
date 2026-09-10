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
    /// Returns the third-party storages the installation can keep portal data in, the providers the build ships with,
    /// such as Amazon S3, Google Cloud Storage or Rackspace. The built-in local storage is not among them: when none
    /// of the entries is `current`, the portal data sits in the local storage. Each entry carries the storage
    /// identifier and title, the authentication keys the provider expects, `isSet` telling whether those keys are
    /// already filled in on the server, and `current` marking the one the portal uses right now. Keys of the current
    /// storage are read from the saved settings, keys of the others from the provider configuration, so a value that
    /// was never configured comes back empty. The caller needs the permission to edit portal settings, which in
    /// practice means the portal owner or a DocSpace admin, and the installation has to be a server one whose access
    /// space is not restricted; otherwise the call is refused with 403. Nothing is written and the call is safe to
    /// repeat. Use `PUT api/2.0/settings/storage` to switch the storage, `DELETE api/2.0/settings/storage` to go back
    /// to the local one, and `GET api/2.0/settings/storage/cdn` or `GET api/2.0/settings/storage/backup` for the CDN
    /// and backup targets.
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
    /// Returns how far the current portal has got in moving its data to another storage, as a percentage from 0 to
    /// 100. The migration itself is started by `PUT api/2.0/settings/storage` or `DELETE api/2.0/settings/storage`,
    /// which put the portal into the migrating state; poll this operation until the value reaches 100, then the
    /// portal is served from the new storage. A value of -1 means storage migration is not offered on this
    /// installation, which is the case for every portal that is not a server one. Ask for the progress only once a
    /// migration has actually been started: for a portal whose migration the server does not remember, the call fails
    /// instead of answering with a zero. The response carries the percentage only, without the error flag the
    /// migration service reports internally, so a value that stops advancing is a reason to check the portal state
    /// with `GET api/2.0/portal` rather than proof of progress. The caller needs the permission to edit portal
    /// settings, which in practice means the portal owner or a DocSpace admin, and the call is accepted even when the
    /// portal payment has lapsed. Nothing is written and the call is safe to repeat.
    /// </remarks>
    /// <summary>
    /// Get the storage migration progress
    /// </summary>
    /// <path>api/2.0/settings/storage/progress</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "Migration progress as a percentage, or -1 where storage migration is not offered", typeof(double))]
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
    /// Queues encryption of everything the installation keeps in its local storage, or decryption of it when the data
    /// is already encrypted: the saved encryption state decides the direction, so the same call encrypts a decrypted
    /// installation and decrypts an encrypted one. It covers the whole server, not one portal, and only a server
    /// installation with the feature switched on can run it, with neither the portal storage nor the CDN pointing at
    /// a third-party provider: reset those first with `DELETE api/2.0/settings/storage` and
    /// `DELETE api/2.0/settings/storage/cdn`. No backup may be running, and the backup schedules of all portals are
    /// dropped as part of starting. The caller needs the permission to edit portal settings, that is the portal owner
    /// or a DocSpace admin, and an unrestricted access space. This is a long, disruptive operation: every portal is
    /// put into the encryption state and stays unavailable until it ends, so do not repeat the call while it runs,
    /// and follow it with `GET api/2.0/settings/encryption/progress` instead. The password is generated on the server
    /// and never returned by the API. Pass `notifyUsers=true` to mail every user before the portals go down. The
    /// response is true once the job is queued, and false where encryption is switched off, nothing being started
    /// then.
    /// </remarks>
    /// <summary>
    /// Start the storage encryption
    /// </summary>
    /// <path>api/2.0/settings/encryption/start</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "True when the encryption job has been queued; false in a build where storage encryption is switched off", typeof(bool))]
    [SwaggerResponse(402, "The portal pricing plan does not include storage encryption")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow storage encryption")]
    [SwaggerResponse(405, "Storage encryption is not available on this installation")]
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

        if (settings.Status == EncryprtionStatus.Decrypted)
        {
            settings.Status = EncryprtionStatus.EncryptionStarted;
            settings.Password = encryptionSettingsHelper.GeneratePassword(32, 16);
        }
        else if (settings.Status == EncryprtionStatus.Encrypted)
        {
            settings.Status = EncryprtionStatus.DecryptionStarted;
        }

        messageService.Send(settings.Status == EncryprtionStatus.EncryptionStarted ? MessageAction.StartStorageEncryption : MessageAction.StartStorageDecryption);

        var serverRootPath = commonLinkUtility.GetFullAbsolutePath("~").TrimEnd('/');

        foreach (var tenant in tenants)
        {
            tenantManager.SetCurrentTenant(tenant);

            if (notifyUsers)
            {
                if (settings.Status == EncryprtionStatus.EncryptionStarted)
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
    /// Returns the encryption state of the installation storage: the status, which is one of decrypted, encryption
    /// started, encrypted or decryption started, and the flag saying whether users are mailed when an encryption run
    /// begins. The password is deliberately blanked out, so the field always comes back empty even on an encrypted
    /// installation. The caller is expected to have the permission to edit portal settings, which in practice means
    /// the portal owner or a DocSpace admin, on a server installation with an unrestricted access space; on any other
    /// installation, and whenever the check fails, the operation answers with an empty body instead of an error. An
    /// empty answer is therefore not proof that encryption is off, only that the settings cannot be read in this
    /// context. Nothing is written and the call is safe to repeat. Use `GET api/2.0/settings/encryption/progress` to
    /// follow a run that is in flight, and `POST api/2.0/settings/encryption/start` to encrypt or decrypt the
    /// storage.
    /// </remarks>
    /// <summary>
    /// Get the storage encryption settings
    /// </summary>
    /// <path>api/2.0/settings/encryption/settings</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "The encryption status and the notify-users flag, with the password blanked out; empty where encryption settings cannot be read", typeof(EncryptionSettings))]
    [SwaggerResponse(403, "The caller may not edit portal settings")]
    [SwaggerResponse(405, "Storage encryption is not available on this installation")]
    [HttpGet("encryption/settings")]
    public async Task<EncryptionSettings> GetStorageEncryptionSettings()
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

            settings.Password = string.Empty; // Don't show password

            return settings;
        }
        catch (Exception e)
        {
            _log.ErrorGetStorageEncryptionSettings(e);
            return null;
        }
    }

    /// <remarks>
    /// Returns how far the running encryption or decryption of the installation storage has got, as a percentage from
    /// 0 to 100. It reports the run started by `POST api/2.0/settings/encryption/start`, whose direction, encryption
    /// or decryption, is told by `GET api/2.0/settings/encryption/settings`. An empty response means no run is in
    /// flight and no recent result is remembered: the value of a finished run is kept for one minute after it
    /// completes and then dropped, so poll often enough not to miss the end of the operation. A value of -1 means the
    /// build does not offer storage encryption at all, and on an installation that is not a server one the call is
    /// refused rather than answered. Unlike the other encryption operations, this one asks for no portal-settings
    /// permission: any authenticated member of the portal may read the progress, which is intentional, because the
    /// portals are unavailable while the run is on and their users need to see when it ends. Nothing is written and
    /// the call is safe to repeat.
    /// </remarks>
    /// <summary>
    /// Get the storage encryption progress
    /// </summary>
    /// <path>api/2.0/settings/encryption/progress</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "Encryption or decryption progress as a percentage, or empty when no run is in flight", typeof(double?))]
    [SwaggerResponse(405, "Storage encryption is not available on this installation")]
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
    /// Points the current portal at another storage and saves the credentials it needs: `module` is the identifier of
    /// one of the storages listed by `GET api/2.0/settings/storage`, and `props` carries that provider's
    /// authentication keys as name and value pairs, for example the bucket, region and access key of an Amazon S3
    /// storage. The provider has to be available on the server, which the `isSet` flag of the listing tells,
    /// otherwise the request is rejected as invalid. Sending the module the portal already uses changes nothing and
    /// returns the saved settings as they are. Any other module starts an asynchronous migration of the portal data:
    /// the portal moves into the migrating state and stays unavailable until the transfer ends, so follow it with
    /// `GET api/2.0/settings/storage/progress` and do not send a second switch while it runs. The caller needs the
    /// permission to edit portal settings, which in practice means the portal owner or a DocSpace admin, on a server
    /// installation with an unrestricted access space. The response is the stored configuration, module and
    /// properties, not the state of the migration. To return to the built-in local storage call
    /// `DELETE api/2.0/settings/storage`, and for the CDN use `PUT api/2.0/settings/storage/cdn`.
    /// </remarks>
    /// <summary>
    /// Switch the portal storage
    /// </summary>
    /// <path>api/2.0/settings/storage</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The saved storage configuration; migration of the portal data to it has been started", typeof(StorageSettings))]
    [SwaggerResponse(400, "The requested storage module is not configured on this installation")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpPut("storage")]
    public async Task<StorageSettings> UpdateStorage(StorageRequestsDto inDto)
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
                return settings;
            }

            settings.Module = inDto.Module;
            settings.Props = inDto.Props.ToDictionary(r => r.Key, b => b.Value);

            await StartMigrateAsync(settings);
            return settings;
        }
        catch (Exception e)
        {
            _log.ErrorUpdateStorage(e);
            throw;
        }
    }

    /// <remarks>
    /// Drops the third-party storage configuration of the current portal, module and saved credentials alike, and
    /// starts an asynchronous migration of the portal data back into the built-in local storage. The portal moves
    /// into the migrating state and stays unavailable until the transfer ends, so follow it with
    /// `GET api/2.0/settings/storage/progress`; the call itself returns as soon as the migration has been handed to
    /// the storage service and gives back no body. The caller needs the permission to edit portal settings, which in
    /// practice means the portal owner or a DocSpace admin, on a server installation with an unrestricted access
    /// space. This is a mutating and slow operation rather than a destructive one: documents are copied back rather
    /// than deleted, but the credentials of the previous storage are gone from the settings and have to be sent again
    /// with `PUT api/2.0/settings/storage` to switch back. Repeating the call while a migration is running starts
    /// another one, so poll instead. Resetting the storage is also the step that makes
    /// `POST api/2.0/settings/encryption/start` possible, since encryption only covers the local storage.
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
    /// Returns the storages that can serve the static content of the portal through a content delivery network, which
    /// is the subset of the providers of `GET api/2.0/settings/storage` that offer a CDN of their own. The entries
    /// have the same shape as in the storage listing: identifier and title, the authentication keys the provider
    /// expects, `isSet` telling whether those keys are filled in on the server, and `current` marking the CDN the
    /// portal uses now. Keys of the current entry come from the saved CDN settings and keys of the others from the
    /// provider configuration. An empty list means the build ships no CDN-capable provider, and a list where nothing
    /// is current means the portal serves its static content itself. The caller needs the permission to edit portal
    /// settings, which in practice means the portal owner or a DocSpace admin, on a server installation with an
    /// unrestricted access space. Nothing is written and the call is safe to repeat. Use
    /// `PUT api/2.0/settings/storage/cdn` to select a CDN and `DELETE api/2.0/settings/storage/cdn` to stop using
    /// one.
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
    /// Selects the content delivery network that serves the static content of the portal and saves the credentials it
    /// needs: `module` is the identifier of one of the entries of `GET api/2.0/settings/storage/cdn`, and `props`
    /// carries that provider's authentication keys as name and value pairs. The provider has to be available on the
    /// server, which the `isSet` flag of the listing tells, otherwise the request is rejected as invalid. Sending the
    /// module the portal already uses changes nothing and returns the saved settings as they are. Any other module is
    /// saved and the upload of the static content is handed to the storage service; the settings come back only when
    /// that hand-over succeeds, a failure being reported as a server error. Unlike the portal storage this has no
    /// progress operation, so there is nothing to poll: the content appears on the CDN once the service has copied
    /// it. Only static content is affected here, never documents; for those use `PUT api/2.0/settings/storage`. The
    /// caller needs the permission to edit portal settings, which in practice means the portal owner or a DocSpace
    /// admin, on a server installation with an unrestricted access space. The response is the stored CDN
    /// configuration.
    /// </remarks>
    /// <summary>
    /// Update the CDN storage
    /// </summary>
    /// <path>api/2.0/settings/storage/cdn</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "The saved CDN configuration; the upload of the static content has been handed to the storage service", typeof(CdnStorageSettings))]
    [SwaggerResponse(400, "The requested CDN module is not configured on this installation")]
    [SwaggerResponse(403, "The caller may not edit portal settings, or this installation does not allow changing the storage")]
    [HttpPut("storage/cdn")]
    public async Task<CdnStorageSettings> UpdateCdnStorage(StorageRequestsDto inDto)
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
            return settings;
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

        return settings;
    }

    /// <remarks>
    /// Drops the CDN configuration of the current portal, module and saved credentials alike, so that the static
    /// content is served by the portal itself again. Nothing is uploaded or migrated, no state change is queued and
    /// the call gives back no body: only the settings are cleared, and files already copied to the content delivery
    /// network are left where they are, to be removed in the provider's own console if that is wanted. The change
    /// takes effect for links built after it, so a page that is already open may keep pointing at the CDN until it is
    /// reloaded. Repeating the call is harmless, because clearing an empty configuration does nothing. The caller
    /// needs the permission to edit portal settings, which in practice means the portal owner or a DocSpace admin, on
    /// a server installation with an unrestricted access space. Use `GET api/2.0/settings/storage/cdn` to see what is
    /// configured now and `PUT api/2.0/settings/storage/cdn` to select a CDN again; the portal storage of the
    /// documents is untouched by this operation and is reset with `DELETE api/2.0/settings/storage` instead.
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
    /// Returns the storages that can hold portal backups, with the one the saved backup schedule writes to marked as
    /// `current` and its parameters filled in from that schedule; when no schedule is saved, or when the schedule
    /// stores backups somewhere else than a third-party provider, none of the entries is current. Each entry has the
    /// same shape as in `GET api/2.0/settings/storage`: identifier, title, the authentication keys the provider
    /// expects, and `isSet` telling whether those keys are filled in on the server. Pass `dump=true` to read the
    /// schedule of the whole server instead of the one of the current portal, which only makes sense on a server
    /// installation. The caller needs the permission to edit portal settings, which in practice means the portal
    /// owner or a DocSpace admin, and on an installation that is not a server one the call is also refused unless
    /// backup is available there. Nothing is written and the call is safe to repeat. This operation says nothing
    /// about where the portal data itself lives; the backup schedule is configured through the backup API, and the
    /// storage of the documents through `PUT api/2.0/settings/storage`.
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
    /// Returns the Amazon regions the server knows about, each with its system name such as `eu-central-1`, the
    /// display name to show a user, and the partition details the region belongs to: partition name, DNS suffix, the
    /// pattern its region names match and the template its host names are built from. This is static reference data
    /// compiled into the server rather than portal configuration: nothing is read from the settings, nothing is
    /// written, the answer is the same for every portal and changes only when the server is updated, so it can be
    /// cached by the caller. Use the system name of an entry as the region value in `props` when configuring an
    /// Amazon S3 storage with `PUT api/2.0/settings/storage`, `PUT api/2.0/settings/storage/cdn` or a backup
    /// schedule, and prefer picking a value from here over typing one, because a region the server does not know
    /// cannot be reached. Any authenticated caller may read the list, no portal-settings permission is asked for, and
    /// the result is neither paginated nor filtered.
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