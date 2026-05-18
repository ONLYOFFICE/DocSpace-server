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

namespace ASC.Migration.Core;

[Scope]
public class MigrationLogger(
    ILogger<MigrationLogger> logger,
    StorageFactory storageFactory,
    TenantManager tenantManager,
    TempStream tempStream)
    : IAsyncDisposable
{
    private Stream _migrationStream;
    private StreamWriter _migrationLog;
    private string _logName;

    public void Init(string logName = null)
    {
        _logName = logName ?? Path.GetRandomFileName();
        if (logName == null)
        {
            _migrationStream = tempStream.Create();
            _migrationLog = new StreamWriter(_migrationStream);
        }
    }

    public async Task SaveLogAsync()
    {
        var store = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), "migration_log", (IQuotaController)null);
        _migrationStream.Position = 0;
        await store.SaveAsync("", _logName, _migrationStream);
    }

    public void Log(string msg, Exception exception = null)
    {
        try
        {
            if (exception != null)
            {
                logger.WarningWithException(msg, exception);
            }
            else
            {
                logger.Information(msg);
            }
            _migrationLog.WriteLine($"{DateTime.Now:s}: {msg}");
            if (exception != null)
            {
                _migrationLog.WriteLine($"{exception.Message}");
            }
            _migrationLog.Flush();
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_migrationLog != null)
        {
            await _migrationLog.DisposeAsync();
        }
    }

    public async Task<Stream> GetStreamAsync()
    {
        logger.Debug($"try get log {_logName} - {tenantManager.GetCurrentTenantId()}");
        var store = await storageFactory.GetStorageAsync(tenantManager.GetCurrentTenantId(), "migration_log", (IQuotaController)null);
        return await store.GetReadStreamAsync("", _logName);
    }

    public string GetLogName()
    {
        return _logName;
    }
}