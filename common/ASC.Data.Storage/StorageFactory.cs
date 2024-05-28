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

namespace ASC.Data.Storage;

[Singleton]
public class StorageFactoryConfig(IConfiguration configuration)
{
    public IEnumerable<string> GetModuleList(string region = "current", bool exceptDisabledMigration = false)
    {
        return GetStorage(region).Module
                .Where(x => x.Visible && (!exceptDisabledMigration || !x.DisableMigrate))
            .Select(x => x.Name);
    }

    public IEnumerable<string> GetDomainList(string modulename, string region = "current")
    {
        var section = GetStorage(region);
        if (section == null)
        {
            throw new ArgumentException("config section not found");
        }

        return section.Module
            .Single(x => x.Name.Equals(modulename, StringComparison.OrdinalIgnoreCase))
            .Domain
            .Where(x => x.Visible)
            .Select(x => x.Name);
    }

    public Configuration.Storage GetStorage(string region)
    {
        return region == "current" ? 
            StorageConfigExtension.GetStorage(configuration) : 
            configuration.GetSection($"regions:{region}:storage").Get<Configuration.Storage>();
    }
}

public static class StorageFactoryExtenstion
{
    public static void InitializeHttpHandlers(this IEndpointRouteBuilder builder, string module = null)
    {
        //TODO:
        //if (!HostingEnvironment.IsHosted)
        //{
        //    throw new InvalidOperationException("Application not hosted.");
        //}

        var configuration = builder.ServiceProvider.GetService<IConfiguration>();
        var section = StorageConfigExtension.GetStorage(configuration);
        
        if (section is { Module: not null })
        {
            foreach (var m in section.Module.Where(r => string.IsNullOrEmpty(module) || r.Name == module))
            {
                //todo: add path criterion
                if (m.Type == "disc" || !m.Public || m.Path.Contains(Constants.StorageRootParam))
                {
                    builder.RegisterStorageHandler(
                        m.Name,
                        string.Empty,
                        m.Public);
                }

                //todo: add path criterion
                if (m.Domain != null)
                {
                    foreach (var d in m.Domain.Where(d => d.Path.Contains(Constants.StorageRootParam)))
                    {
                        builder.RegisterStorageHandler(
                            m.Name,
                            d.Name,
                            d.Public);
                    }
                }
            }
        }
    }
}

[Scope]
public class StorageFactory(IServiceProvider serviceProvider,
    StorageFactoryConfig storageFactoryConfig,
    SettingsManager settingsManager,
    StorageSettingsHelper storageSettingsHelper,
    CoreBaseSettings coreBaseSettings)
{
    private const string DefaultTenantName = "default";

    public async Task<IDataStore> GetStorageAsync(int tenant, string module, string region = "current")
    {
        var tenantQuotaController = serviceProvider.GetService<TenantQuotaController>();
        tenantQuotaController.Init(tenant);

        return await GetStorageAsync(tenant, module, tenantQuotaController, region);
    }

    public async Task<IDataStore> GetStorageAsync(int? tenant, string module, IQuotaController controller, string region = "current")
    {
        var tenantPath = tenant != null ? TenantPath.CreatePath(tenant.Value) : TenantPath.CreatePath(DefaultTenantName);

        tenant ??= -2;

        var section = storageFactoryConfig.GetStorage(region);
        if (section == null)
        {
            throw new InvalidOperationException("config section not found");
        }

        var settings = await settingsManager.LoadAsync<StorageSettings>(tenant.Value);
        //TODO:GetStoreAndCache
        return await GetDataStoreAsync(tenantPath, module, await storageSettingsHelper.DataStoreConsumerAsync(settings), controller, region);
    }

    public async Task<IDataStore> GetStorageFromConsumerAsync(int? tenant, string module, DataStoreConsumer consumer, string region = "current")
    {
        var tenantPath = tenant != null ? TenantPath.CreatePath(tenant.Value) : TenantPath.CreatePath(DefaultTenantName);

        var section = storageFactoryConfig.GetStorage(region);
        if (section == null)
        {
            throw new InvalidOperationException("config section not found");
        }

        var tenantQuotaController = serviceProvider.GetService<TenantQuotaController>();
        tenantQuotaController.Init(tenant.GetValueOrDefault());

        return await GetDataStoreAsync(tenantPath, module, consumer, tenantQuotaController);
    }

    public async Task QuotaUsedAddAsync(int? tenant, string module, string domain, string dataTag, long size, Guid ownerId)
    {
        var tenantQuotaController = serviceProvider.GetService<TenantQuotaController>();
        tenantQuotaController.Init(tenant.GetValueOrDefault());

        await tenantQuotaController.QuotaUserUsedAddAsync(module, domain, dataTag, size, ownerId);
    }

    public async Task QuotaUsedDeleteAsync(int? tenant, string module, string domain, string dataTag, long size, Guid ownerId)
    {
        var tenantQuotaController = serviceProvider.GetService<TenantQuotaController>();
        tenantQuotaController.Init(tenant.GetValueOrDefault());

        await tenantQuotaController.QuotaUserUsedDeleteAsync(module, domain, dataTag, size, ownerId);
    }

    private async Task<IDataStore> GetDataStoreAsync(string tenantPath, string module, DataStoreConsumer consumer, IQuotaController controller, string region = "current")
    {
        var storage = storageFactoryConfig.GetStorage(region);
        var moduleElement = storage.GetModuleElement(module);
        if (moduleElement == null)
        {
            throw new ArgumentException("no such module", module);
        }

        var handler = storage.GetHandler(moduleElement.Type);
        Type instanceType;
        IDictionary<string, string> props;

        if (coreBaseSettings.Standalone &&
            !moduleElement.DisableMigrate &&
            await consumer.GetIsSetAsync())
        {
            instanceType = consumer.HandlerType;
            props = consumer;
        }
        else
        {
            instanceType = Type.GetType(handler.Type, true);
            props = handler.Property.ToDictionary(r => r.Name, r => r.Value);
        }

        IDataStoreValidator validator = null;
        if (!string.IsNullOrEmpty(moduleElement.ValidatorType))
        {
            var validatorType = Type.GetType(moduleElement.ValidatorType, false);
            if (validatorType != null)
            {
                validator = (IDataStoreValidator)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, validatorType);
            }
        }

        return ((IDataStore)ActivatorUtilities.CreateInstance(serviceProvider, instanceType))
            .Configure(tenantPath, handler, moduleElement, props, validator)
            .SetQuotaController(moduleElement.Count ? controller : null
            /*don't count quota if specified on module*/);
    }
}
