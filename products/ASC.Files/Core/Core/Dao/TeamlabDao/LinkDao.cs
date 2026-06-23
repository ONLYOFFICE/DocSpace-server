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
        var tenantId = _tenantManager.GetCurrentTenantId();
        var mapping = daoFactory.GetMapping<T>();
        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        await filesDbContext.AddOrUpdateAsync(r => r.FilesLink, new DbFilesLink
        {
            TenantId = tenantId,
            SourceId = (await mapping.MappingIdAsync(sourceId)).Item1,
            LinkedId = (await mapping.MappingIdAsync(linkedId)).Item1,
            LinkedFor = _authContext.CurrentAccount.ID
        });

        await filesDbContext.SaveChangesAsync();
    }

    public async Task<T> GetSourceAsync(T linkedId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var mapping = daoFactory.GetMapping<T>();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedLinkedId = await mapping.MappingIdAsync(linkedId);

        var fromDb = await filesDbContext.SourceIdAsync(tenantId, mappedLinkedId.Item1, _authContext.CurrentAccount.ID);

        if (Equals(fromDb, null))
        {
            return default;
        }

        return (T)Convert.ChangeType(fromDb, typeof(T));
    }

    public async Task<T> GetLinkedAsync(T sourceId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var mapping = daoFactory.GetMapping<T>();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedSourceId = await mapping.MappingIdAsync(sourceId);

        var fromDb = await filesDbContext.LinkedIdAsync(tenantId, mappedSourceId.Item1, _authContext.CurrentAccount.ID);

        if (Equals(fromDb, null))
        {
            return default;
        }

        return (T)Convert.ChangeType(fromDb, typeof(T));
    }

    public async Task<Dictionary<T, T>> GetLinkedIdsAsync(IEnumerable<T> sourceIds)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var mapping = daoFactory.GetMapping<T>();

        var mappedIds = await sourceIds
            .ToAsyncEnumerable()
            .Select(async (T x, CancellationToken _) => await mapping.MappingIdAsync(x))
            .ToListAsync();
        var source = mappedIds.Select(x => x.Item1);

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        return await filesDbContext.FilesLinksAsync(tenantId, source, _authContext.CurrentAccount.ID)
            .ToDictionaryAsync(
                x => (T)Convert.ChangeType(x.SourceId, typeof(T)),
                x => (T)Convert.ChangeType(x.LinkedId, typeof(T)));
    }

    public async Task DeleteLinkAsync(T sourceId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var mapping = daoFactory.GetMapping<T>();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedSourceId = await mapping.MappingIdAsync(sourceId);

        var link = await filesDbContext.FileLinkAsync(tenantId, mappedSourceId.Item1, _authContext.CurrentAccount.ID);

        filesDbContext.FilesLink.Remove(link);

        await filesDbContext.SaveChangesAsync();
    }

    public async Task DeleteAllLinkAsync(T fileId)
    {
        var tenantId = _tenantManager.GetCurrentTenantId();
        var mapping = daoFactory.GetMapping<T>();

        await using var filesDbContext = await _dbContextFactory.CreateDbContextAsync();

        var mappedFileId = await mapping.MappingIdAsync(fileId);

        await filesDbContext.DeleteFileLinks(tenantId, mappedFileId.Item1);
    }
}