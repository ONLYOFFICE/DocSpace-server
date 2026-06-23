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

namespace ASC.Files.Thirdparty.ProviderDao;

internal class ProviderDaoBase(IServiceProvider serviceProvider,
        TenantManager tenantManager,
        CrossDao crossDao,
        SelectorFactory selectorFactory,
        ISecurityDao<string> securityDao)
    : ThirdPartyProviderDao
{
    protected readonly IServiceProvider _serviceProvider = serviceProvider;
    protected readonly TenantManager _tenantManager = tenantManager;
    protected readonly ISecurityDao<string> _securityDao = securityDao;
    protected readonly SelectorFactory _selectorFactory = selectorFactory;
    protected readonly CrossDao _crossDao = crossDao;

    protected bool IsCrossDao(string id1, string id2)
    {
        if (id2 == null || id1 == null)
        {
            return false;
        }

        return !Equals(_selectorFactory.GetSelector(id1).GetIdCode(id1), _selectorFactory.GetSelector(id2).GetIdCode(id2));
    }

    protected async Task SetSharedPropertyAsync(IEnumerable<FileEntry<string>> entries)
    {
        var pureShareRecords = await _securityDao.GetPureShareRecordsAsync(entries).ToListAsync();
        var ids = pureShareRecords
            //.Where(x => x.Owner == SecurityContext.CurrentAccount.ID)
            .Select(x => x.EntryId).Distinct();

        foreach (var id in ids)
        {
            var firstEntry = entries.FirstOrDefault(y => y.Id.Equals(id));

            firstEntry?.Shared = true;
        }
    }

    protected internal Task<File<string>> PerformCrossDaoFileCopyAsync(string fromFileId, string toFolderId, bool deleteSourceFile)
    {
        var fromSelector = _selectorFactory.GetSelector(fromFileId);
        var toSelector = _selectorFactory.GetSelector(toFolderId);

        return _crossDao.PerformCrossDaoFileCopyAsync(
            fromFileId, fromSelector.GetFileDao(fromFileId), fromSelector.ConvertId,
            toFolderId, toSelector.GetFileDao(toFolderId), toSelector.ConvertId,
            deleteSourceFile);
    }

    protected async Task<File<int>> PerformCrossDaoFileCopyAsync(string fromFileId, int toFolderId, bool deleteSourceFile, Guid chatId = default)
    {
        var fromSelector = _selectorFactory.GetSelector(fromFileId);

        return await _crossDao.PerformCrossDaoFileCopyAsync(
            fromFileId, 
            fromSelector.GetFileDao(fromFileId), 
            fromSelector.ConvertId,
            toFolderId, 
            _serviceProvider.GetService<IFileDao<int>>(), 
            r => r,
            deleteSourceFile, 
            chatId);
    }

    protected Task<Folder<string>> PerformCrossDaoFolderCopyAsync(string fromFolderId, string toRootFolderId, bool deleteSourceFolder, CancellationToken? cancellationToken)
    {
        var fromSelector = _selectorFactory.GetSelector(fromFolderId);
        var toSelector = _selectorFactory.GetSelector(toRootFolderId);

        return _crossDao.PerformCrossDaoFolderCopyAsync(
            fromFolderId, fromSelector.GetFolderDao(fromFolderId), fromSelector.GetFileDao(fromFolderId), fromSelector.ConvertId,
            toRootFolderId, toSelector.GetFolderDao(toRootFolderId), toSelector.GetFileDao(toRootFolderId), toSelector.ConvertId,
            deleteSourceFolder, cancellationToken);
    }

    protected Task<Folder<int>> PerformCrossDaoFolderCopyAsync(string fromFolderId, int toRootFolderId, bool deleteSourceFolder, CancellationToken? cancellationToken)
    {
        var fromSelector = _selectorFactory.GetSelector(fromFolderId);

        return _crossDao.PerformCrossDaoFolderCopyAsync(
            fromFolderId, fromSelector.GetFolderDao(fromFolderId), fromSelector.GetFileDao(fromFolderId), fromSelector.ConvertId,
            toRootFolderId, _serviceProvider.GetService<IFolderDao<int>>(), _serviceProvider.GetService<IFileDao<int>>(), r => r,
            deleteSourceFolder, cancellationToken);
    }
}