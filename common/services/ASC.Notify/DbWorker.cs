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

using ASC.Core.Common.Notify.Model;

namespace ASC.Notify;

[Singleton]
public class DbWorker(IServiceScopeFactory serviceScopeFactory, ConfigureNotifyServiceCfg notifyServiceCfg, IDistributedLockProvider distributedLockProvider)
{
    private readonly NotifyServiceCfg _notifyServiceCfg = notifyServiceCfg.Value;

    public async Task SaveMessageAsync(NotifyMessage m)
    {
        using var scope = serviceScopeFactory.CreateScope();
        await using var context = await scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContextAsync();

        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var dbContext = await scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContextAsync();
            await using var tx = await dbContext.Database.BeginTransactionAsync();
            var notifyQueue = m.Map();
            notifyQueue.Attachments = JsonSerializer.Serialize(m.Attachments);

            notifyQueue = (await dbContext.NotifyQueue.AddAsync(notifyQueue)).Entity;
            await dbContext.SaveChangesAsync();

            var id = notifyQueue.NotifyId;

            var info = new NotifyInfo
            {
                NotifyId = id,
                State = 0,
                Attempts = 0,
                ModifyDate = DateTime.UtcNow,
                Priority = m.Priority
            };

            await dbContext.NotifyInfo.AddAsync(info);
            await dbContext.SaveChangesAsync();

            await tx.CommitAsync();
        });
    }

    public async Task<IDictionary<int, NotifyMessage>> GetMessagesAsync(int count)
    {
        await using (await distributedLockProvider.TryAcquireLockAsync("get_notify_messages"))
        {
            using var scope = serviceScopeFactory.CreateScope();

            await using var dbContext = await scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContextAsync();

            var q = dbContext.NotifyQueue
                .Join(dbContext.NotifyInfo, r => r.NotifyId, r => r.NotifyId, (queue, info) => new { queue, info })
                .Where(r => r.info.State == (int)MailSendingState.NotSended || r.info.State == (int)MailSendingState.Error && r.info.ModifyDate < DateTime.UtcNow - TimeSpan.Parse(_notifyServiceCfg.Process.AttemptsInterval))
                .OrderBy(i => i.info.Priority)
                .ThenBy(i => i.info.NotifyId)
                .Take(count);


            var messages = await q
                .ToDictionaryAsync(
                    r => r.queue.NotifyId,
                    r =>
                    {
                        var res = r.queue.Map();

                        try
                        {
                            res.Attachments = JsonSerializer.Deserialize<NotifyMessageAttachment[]>(r.queue.Attachments);
                        }
                        catch (Exception)
                        {
                        }

                        return res;
                    });

            await dbContext.NotifyInfo.Where(r => messages.Keys.Any(a => a == r.NotifyId)).ExecuteUpdateAsync(entry => entry.SetProperty(p => p.State, (int)MailSendingState.Sending));

            return messages;
        }
    }

    public async Task ResetStatesAsync()
    {
        using var scope = serviceScopeFactory.CreateScope();
        await using var dbContext = await scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContextAsync();

        await Queries.ResetStatesAsync(dbContext);
    }

    public async Task SetStateAsync(int id, MailSendingState result)
    {
        using var scope = serviceScopeFactory.CreateScope();
        await using var dbContext = await scope.ServiceProvider.GetService<IDbContextFactory<NotifyDbContext>>().CreateDbContextAsync();

        if (result == MailSendingState.Sended)
        {
            var d = await Queries.NotifyInfoAsync(dbContext, id);

            dbContext.NotifyInfo.Remove(d);
            await dbContext.SaveChangesAsync();
        }
        else
        {
            if (result == MailSendingState.Error)
            {
                var attempts = await Queries.AttemptsAsync(dbContext, id);
                if (_notifyServiceCfg.Process.MaxAttempts <= attempts + 1)
                {
                    result = MailSendingState.FatalError;
                }
            }

            await Queries.UpdateNotifyInfoAsync(dbContext, id, (int)result);
        }
    }
}

static file class Queries
{
    public static readonly Func<NotifyDbContext, Task<int>> ResetStatesAsync =
        EF.CompileAsyncQuery(
            (NotifyDbContext ctx) =>
                ctx.NotifyInfo
                    .Where(r => r.State == 1)
                    .ExecuteUpdate(q => q.SetProperty(p => p.State, 0)));

    public static readonly Func<NotifyDbContext, int, Task<NotifyInfo>> NotifyInfoAsync =
        EF.CompileAsyncQuery(
            (NotifyDbContext ctx, int id) =>
                ctx.NotifyInfo
                    .FirstOrDefault(r => r.NotifyId == id));

    public static readonly Func<NotifyDbContext, int, Task<int>> AttemptsAsync =
        EF.CompileAsyncQuery(
            (NotifyDbContext ctx, int id) =>
                ctx.NotifyInfo
                    .Where(r => r.NotifyId == id)
                    .Select(r => r.Attempts)
                    .FirstOrDefault());

    public static readonly Func<NotifyDbContext, int, int, Task<int>> UpdateNotifyInfoAsync =
        EF.CompileAsyncQuery(
            (NotifyDbContext ctx, int id, int result) =>
                ctx.NotifyInfo
                    .Where(r => r.NotifyId == id)
                    .ExecuteUpdate(q =>
                        q.SetProperty(p => p.State, result)
                            .SetProperty(p => p.Attempts, p => p.Attempts + 1)
                            .SetProperty(p => p.ModifyDate, DateTime.UtcNow)));
}