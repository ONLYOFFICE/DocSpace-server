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

namespace ASC.Files.Core.Data;

[Scope(typeof(ILinkDao<int>), GenericArguments = [typeof(int)])]
[Scope(typeof(ILinkDao<string>), GenericArguments = [typeof(string)])]
internal class LinkDao<T>(
    IDaoFactory daoFactory,
    UserManager userManager,
    IDbContextFactory<FilesDbContext> dbContextManager,
    TenantManager tenantManager,
    TenantUtil tenantUtil,
    SetupInfo setupInfo,
    MaxTotalSizeStatistic maxTotalSizeStatistic,
    SettingsManager settingsManager,
    AuthContext authContext,
    IServiceProvider serviceProvider,
    IDistributedLockProvider distributedLockProvider)
    : AbstractDao(dbContextManager,
        userManager,
        tenantManager,
        tenantUtil,
        setupInfo,
        maxTotalSizeStatistic,
        settingsManager,
        authContext,
        serviceProvider,
        distributedLockProvider), ILinkDao<T>
{
    public async Task AddLinkAsync(T sourceId, T linkedId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mapping = daoFactory.GetMapping<T>();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await filesDbContext.AddOrUpdateAsync(r => r.FilesLink, new DbFilesLink
        {
            TenantId = tenantId,
            SourceId = (await mapping.MappingIdAsync(sourceId)),
            LinkedId = (await mapping.MappingIdAsync(linkedId)),
            LinkedFor = _authContext.CurrentAccount.ID
        });

        await filesDbContext.SaveChangesAsync();
    }

    public async Task<T> GetSourceAsync(T linkedId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mapping = daoFactory.GetMapping<T>();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedLinkedId = (await mapping.MappingIdAsync(linkedId));

        var fromDb = await filesDbContext.SourceIdAsync(tenantId, mappedLinkedId, _authContext.CurrentAccount.ID);

        if (Equals(fromDb, default))
        {
            return default;
    }

        return (T)Convert.ChangeType(fromDb, typeof(T));
    }

    public async Task<T> GetLinkedAsync(T sourceId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mapping = daoFactory.GetMapping<T>();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedSourceId = await mapping.MappingIdAsync(sourceId);

        var fromDb = await filesDbContext.LinkedIdAsync(tenantId, mappedSourceId, _authContext.CurrentAccount.ID);

        if (Equals(fromDb, default))
        {
            return default;
    }

        return (T)Convert.ChangeType(fromDb, typeof(T));
    }

    public async Task DeleteLinkAsync(T sourceId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mapping = daoFactory.GetMapping<T>();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedSourceId = (await mapping.MappingIdAsync(sourceId));

        var link = await filesDbContext.FileLinkAsync(tenantId, mappedSourceId, _authContext.CurrentAccount.ID);

        filesDbContext.FilesLink.Remove(link);

        await filesDbContext.SaveChangesAsync();
    }

    public async Task DeleteAllLinkAsync(T fileId)
    {
        var tenantId = await _tenantManager.GetCurrentTenantIdAsync();
        var mapping = daoFactory.GetMapping<T>();
        
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedFileId = (await mapping.MappingIdAsync(fileId));

        await filesDbContext.DeleteFileLinks(tenantId, mappedFileId);
    }
}
