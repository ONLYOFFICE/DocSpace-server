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

using Amazon;

using ASC.Data.Storage.Encryption.IntegrationEvents.Events;

namespace ASC.Web.Api.Controllers.Settings;

public class StorageController(ILoggerProvider option,
        ServiceClient serviceClient,
        MessageService messageService,
        SecurityContext securityContext,
        StudioNotifyService studioNotifyService,
        ApiContext apiContext,
        TenantManager tenantManager,
        PermissionContext permissionContext,
        SettingsManager settingsManager,
        WebItemManager webItemManager,
        CoreBaseSettings coreBaseSettings,
        CommonLinkUtility commonLinkUtility,
        StorageSettingsHelper storageSettingsHelper,
        IWebHostEnvironment webHostEnvironment,
        ConsumerFactory consumerFactory,
        IMemoryCache memoryCache,
        IEventBus eventBus,
        EncryptionSettingsHelper encryptionSettingsHelper,
        BackupAjaxHandler backupAjaxHandler,
        ICacheNotify<DeleteSchedule> cacheDeleteSchedule,
        EncryptionWorker encryptionWorker,
        IHttpContextAccessor httpContextAccessor, 
        IDistributedLockProvider distributedLockProvider,
        TenantExtra tenantExtra)
    : BaseSettingsController(apiContext, memoryCache, webItemManager, httpContextAccessor)
{
    private readonly ILogger _log = option.CreateLogger("ASC.Api");

    /// <summary>
    /// Returns a list of all the portal storages.
    /// </summary>
    /// <short>Get storages</short>
    /// <path>api/2.0/settings/storage</path>
    /// <collection>list</collection>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "List of storages with the following parameters", typeof(List<StorageDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("storage")]
    public async Task<List<StorageDto>> GetAllStoragesAsync()
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

    /// <summary>
    /// Returns the storage progress.
    /// </summary>
    /// <short>Get the storage progress</short>
    /// <path>api/2.0/settings/storage/progress</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "Storage progress", typeof(double))]
    [AllowNotPayment]
    [HttpGet("storage/progress")]
    public async Task<double> GetStorageProgressAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        if (!coreBaseSettings.Standalone)
        {
            return -1;
        }

        var tenant = tenantManager.GetCurrentTenant();
        return serviceClient.GetProgress(tenant.Id);
    }

    /// <summary>
    /// Starts the storage encryption process.
    /// </summary>
    /// <short>Start the storage encryption process</short>
    /// <path>api/2.0/settings/encryption/start</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "Boolean value: true if the operation is successful", typeof(bool))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(405, "Method not allowed")]
    [HttpPost("encryption/start")]
    public async Task<bool> StartStorageEncryptionAsync(StorageEncryptionRequestsDto inDto)
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

        var storages = await GetAllStoragesAsync();

        if (storages.Exists(s => s.Current))
        {
            throw new NotSupportedException();
        }

        var cdnStorages = await GetAllCdnStoragesAsync();

        if (cdnStorages.Exists(s => s.Current))
        {
            throw new NotSupportedException();
        }

        var tenants = await tenantManager.GetTenantsAsync();

        foreach (var tenant in tenants)
        {
            var progress = await backupAjaxHandler.GetBackupProgressAsync(tenant.Id);
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

        await eventBus.PublishAsync(new EncryptionDataStorageRequestedIntegrationEvent
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

    /// <summary>
    /// Returns the storage encryption settings.
    /// </summary>
    /// <short>Get the storage encryption settings</short>
    /// <path>api/2.0/settings/encryption/settings</path>
    [ApiExplorerSettings(IgnoreApi = true)]
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "Storage encryption settings", typeof(EncryptionSettings))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [SwaggerResponse(405, "Method not allowed")]
    [HttpGet("encryption/settings")]
    public async Task<EncryptionSettings> GetStorageEncryptionSettingsAsync()
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

    /// <summary>
    /// Returns the storage encryption progress.
    /// </summary>
    /// <short>Get the storage encryption progress</short>
    /// <path>api/2.0/settings/encryption/progress</path>
    [Tags("Settings / Encryption")]
    [SwaggerResponse(200, "Storage encryption progress", typeof(double?))]
    [SwaggerResponse(405, "Method not allowed")]
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

    /// <summary>
    /// Updates a storage with the parameters specified in the request.
    /// </summary>
    /// <short>Update a storage</short>
    /// <path>api/2.0/settings/storage</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "Updated storage settings", typeof(StorageSettings))]
    [SwaggerResponse(400, "Module")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("storage")]
    public async Task<StorageSettings> UpdateStorageAsync(StorageRequestsDto inDto)
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

    /// <summary>
    /// Resets the storage settings to the default parameters.
    /// </summary>
    /// <short>Reset the storage settings</short>
    /// <path>api/2.0/settings/storage</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("storage")]
    public async Task ResetStorageToDefaultAsync()
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

    /// <summary>
    /// Returns a list of all the CDN storages.
    /// </summary>
    /// <short>Get the CDN storages</short>
    /// <path>api/2.0/settings/storage/cdn</path>
    /// <collection>list</collection>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "List of the CDN storages with the following parameters", typeof(List<StorageDto>))]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpGet("storage/cdn")]
    public async Task<List<StorageDto>> GetAllCdnStoragesAsync()
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

    /// <summary>
    /// Updates the CDN storage with the parameters specified in the request.
    /// </summary>
    /// <short>Update the CDN storage</short>
    /// <path>api/2.0/settings/storage/cdn</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "Updated CDN storage", typeof(CdnStorageSettings))]
    [SwaggerResponse(400, "Module")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpPut("storage/cdn")]
    public async Task<CdnStorageSettings> UpdateCdnAsync(StorageRequestsDto inDto)
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

    /// <summary>
    /// Resets the CDN storage settings to the default parameters.
    /// </summary>
    /// <short>Reset the CDN storage settings</short>
    /// <path>api/2.0/settings/storage/cdn</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "Ok")]
    [SwaggerResponse(403, "No permissions to perform this action")]
    [HttpDelete("storage/cdn")]
    public async Task ResetCdnToDefaultAsync()
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        await tenantExtra.DemandAccessSpacePermissionAsync();

        await storageSettingsHelper.ClearAsync(await settingsManager.LoadAsync<CdnStorageSettings>());
    }

    /// <summary>
    /// Returns a list of all the backup storages.
    /// </summary>
    /// <short>Get the backup storages</short>
    /// <path>api/2.0/settings/storage/backup</path>
    /// <collection>list</collection>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "List of the backup storages with the following parameters", typeof(List<StorageDto>))]
    [SwaggerResponse(402, "Your pricing plan does not support this option")]
    [HttpGet("storage/backup")]
    public async Task<List<StorageDto>> GetAllBackupStorages(AllBackupStoragesDto dto)
    {
        await permissionContext.DemandPermissionsAsync(SecurityConstants.EditPortalSettings);

        var schedule = await backupAjaxHandler.GetScheduleAsync(dto.Dump);
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

    /// <summary>
    /// Returns a list of all Amazon regions.
    /// </summary>
    /// <short>Get Amazon regions</short>
    /// <path>api/2.0/settings/storage/s3/regions</path>
    [Tags("Settings / Storage")]
    [SwaggerResponse(200, "List of the Amazon regions", typeof(object))]
    [HttpGet("storage/s3/regions")]
    public object GetAmazonS3Regions()
    {
        return RegionEndpoint.EnumerableAllRegions;
    }
    }
