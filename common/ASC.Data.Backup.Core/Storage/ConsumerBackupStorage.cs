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

namespace ASC.Data.Backup.Storage;

[Scope]
public class ConsumerBackupStorage(
        StorageSettingsHelper storageSettingsHelper,
        SetupInfo setupInfo,
        StorageFactory storageFactory,
        IFusionCache cache)
    : IBackupStorage, IGetterWriteOperator
{
    private IDataStore _store;

    private bool _isTemporary;
    private string Domain => _isTemporary ? "" : "backup";
    private CommonChunkedUploadSessionHolder _sessionHolder;

    public async Task InitAsync(IReadOnlyDictionary<string, string> storageParams)
    {
        var settings = new StorageSettings { Module = storageParams["module"], Props = storageParams.Where(r => r.Key != "module").ToDictionary(r => r.Key, r => r.Value) };
        _store = await storageSettingsHelper.DataStoreAsync(settings);
        _sessionHolder = new CommonChunkedUploadSessionHolder(_store, Domain, cache, setupInfo.ChunkUploadSize);
    }

    public async Task InitAsync(int tenant)
    {
        _isTemporary = true;
        _store = await storageFactory.GetStorageAsync(tenant, "backup");
        _sessionHolder = new CommonChunkedUploadSessionHolder(_store, Domain, cache, setupInfo.ChunkUploadSize);
    }

    public async Task<string> UploadAsync(string storageBasePath, string localPath, Guid userId, CancellationToken token)
    {
        await using var stream = File.OpenRead(localPath);
        var storagePath = Path.GetFileName(localPath);
        await _store.SaveAsync(Domain, storagePath, stream, ACL.Private, token);
        return storagePath;
    }

    public async Task<string> DownloadAsync(string storagePath, string targetLocalPath)
    {
        var tempPath = Path.Combine(targetLocalPath, Path.GetFileName(storagePath));
        await using var source = await _store.GetReadStreamAsync(Domain, storagePath);
        await using var destination = File.OpenWrite(tempPath);
        await source.CopyToAsync(destination);
        return tempPath;
    }

    public async Task DeleteAsync(string storagePath)
    {
        if (await _store.IsFileAsync(Domain, storagePath))
        {
            await _store.DeleteAsync(Domain, storagePath);
        }
    }

    public async Task<bool> IsExistsAsync(string storagePath)
    {
        if (_store != null)
        {
            return await _store.IsFileAsync(Domain, storagePath);
        }

        return false;
    }

    public async Task<string> GetPublicLinkAsync(string storagePath)
    {
        if (_isTemporary)
        {
            return (await _store.GetPreSignedUriAsync(Domain, storagePath, TimeSpan.FromDays(1), null)).ToString();
        }

        return (await _store.GetInternalUriAsync(Domain, storagePath, TimeSpan.FromDays(1), null)).AbsoluteUri;
    }

    public async Task<IDataWriteOperator> GetWriteOperatorAsync(string storageBasePath, string title, Guid userId)
    {
        var session = new CommonChunkedUploadSession(-1)
        {
            TempPath = title,
            UploadId = await _store.InitiateChunkedUploadAsync(Domain, title)
        };
        return _store.CreateDataWriteOperator(session, _sessionHolder, true);
    }

    public Task<string> GetBackupExtensionAsync(string storageBasePath)
    {
        return Task.FromResult(_store.GetBackupExtension(true));
    }
}