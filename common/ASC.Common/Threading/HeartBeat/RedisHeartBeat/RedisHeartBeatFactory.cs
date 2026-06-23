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

using ASC.Common.Threading.HeartBeat.Abstractions;

namespace ASC.Common.Threading.HeartBeat.RedisHeartBeat;

public class RedisHeartBeatFactory(IRedisDatabase database) : IHeartBeatFactory
{
    private static readonly string _idPrefix;
    
    static RedisHeartBeatFactory()
    {
        using var currentProcess = Process.GetCurrentProcess();
        _idPrefix = $"{Environment.MachineName}_{currentProcess.Id}";
    }
    
    public async ValueTask<IHeartBeat> CreateAsync(
        string key, 
        TimeSpan timeout, 
        TimeSpan pulseInterval,
        CancellationToken cancellationToken = default)
    {
        var id = $"{_idPrefix}_{Guid.NewGuid():n}";

        if (!await database.Database.LockTakeAsync(key, id, timeout))
        {
            throw new HeartBeatExistsException();
        }
        
        // CA2000: PeriodicTimer is owned and disposed by RedisHeartBeat (see RedisHeartBeat.DisposeAsync)
#pragma warning disable CA2000
        var timer = StartPulseLoop(database, key, id, timeout, pulseInterval, cancellationToken);

        return new RedisHeartBeat(key, id, database, timer);
#pragma warning restore CA2000
    }
    
    private static PeriodicTimer StartPulseLoop(
        IRedisDatabase database, 
        string key, 
        string id,
        TimeSpan timeout,
        TimeSpan pulseInterval,
        CancellationToken cancellationToken)
    {
        var timer = new PeriodicTimer(pulseInterval);

        _ = Task.Run(async () =>
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await database.Database.LockExtendAsync(key, id, timeout);
            }
        }, cancellationToken);

        return timer;
    }
}