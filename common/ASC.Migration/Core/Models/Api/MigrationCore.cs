// (c) Copyright Ascensio System SIA 2010-2022
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

namespace ASC.Migration.Core.Models.Api;

[Scope]
public class MigrationCore(
    IServiceProvider serviceProvider,
    IEventBus eventBus,
    AuthContext authContext,
    TenantManager tenantManager,
    MigrationWorker migrationWorker)
{
    public string[] GetAvailableMigrations() => serviceProvider.GetService<IEnumerable<IMigration>>().Select(r => r.Meta.Name).Where(n => n != "Nextcloud" && n != "Owncloud").ToArray();

    public IMigration GetMigrator(string migrator)
    {
        return serviceProvider.GetService<IEnumerable<IMigration>>().FirstOrDefault(r => r.Meta.Name.Equals(migrator, StringComparison.OrdinalIgnoreCase));
    }

    public async Task StartParseAsync(string migrationName)
    {
        eventBus.Publish(new MigrationParseIntegrationEvent(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantIdAsync())
        {
            MigratorName = migrationName
        });
    }

    public async Task StartAsync(MigrationApiInfo info)
    {
        eventBus.Publish(new MigrationIntegrationEvent(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantIdAsync())
        {
            ApiInfo = info
        });
    }

    public async Task StopAsync()
    {
        eventBus.Publish(new MigrationCancelIntegrationEvent(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantIdAsync()));
    }

     public async Task ClearAsync()
     {
        eventBus.Publish(new MigrationClearIntegrationEvent(authContext.CurrentAccount.ID, await tenantManager.GetCurrentTenantIdAsync()));
     }

    public async Task<MigrationOperation> GetStatusAsync()
    {
        return migrationWorker.GetStatus(await tenantManager.GetCurrentTenantIdAsync());
    }

    public static void Register(DIHelper services)
    {
        services.TryAdd<MigrationCore>();

        services.TryAdd<IMigration, GoogleWorkspaceMigration>();
        services.TryAdd<GwsMigratingUser>();
        services.TryAdd<GwsMigratingFiles>();
        services.TryAdd<GWSMigratingGroups>();

        services.TryAdd<IMigration, NextcloudWorkspaceMigration>();
        services.TryAdd<NcMigratingUser>();
        services.TryAdd<NcMigratingFiles>();
        services.TryAdd<NcMigratingGroups>();

        services.TryAdd<IMigration, OwnCloudMigration>();
        services.TryAdd<OсMigratingUser>();
        services.TryAdd<OсMigratingFiles>();
        services.TryAdd<OсMigratingGroups>();

        services.TryAdd<IMigration, WorkspaceMigration>();
        services.TryAdd<WorkspaceMigratingUser>();
        services.TryAdd<WorkspaceMigratingFiles>();
        services.TryAdd<WorkspaceMigrationGroups>();
    }
}
