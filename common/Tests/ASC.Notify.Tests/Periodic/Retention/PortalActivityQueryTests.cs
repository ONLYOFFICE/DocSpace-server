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

namespace ASC.Notify.Tests.Periodic.Retention;

/// <summary>
/// The two queries the retention job reads a portal's last activity with, against the stack's real database:
/// they translate, they pick the newest row by date rather than by id, and the sign-in one leaves failed
/// sign-ins out. Each case writes events dated in the future into a portal of its own, so the events the
/// portal's registration wrote never decide the answer.
/// </summary>
public class PortalActivityQueryTests
{
    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    private static readonly DateTime _future = DateTime.UtcNow.Date.AddDays(100);

    [Fact]
    public async Task LastEventDate_IsTheNewestByDate_NotByOrderOfWriting()
    {
        var stack = await GetStackAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);
        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        await using (var context = await scope.Services.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            // Written first, so with the lower id, but the later of the two.
            context.AuditEvents.Add(new DbAuditEvent { TenantId = portal.TenantId, Date = _future.AddDays(20), Action = (int)MessageAction.RoomCreated });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.AuditEvents.Add(new DbAuditEvent { TenantId = portal.TenantId, Date = _future.AddDays(10), Action = (int)MessageAction.RoomCreated });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var lastEventOn = await scope.Services.GetRequiredService<AuditEventsRepository>().GetLastEventDateAsync(portal.TenantId);

        lastEventOn?.Date.Should().Be(_future.AddDays(20), "the newest event is the latest date, whatever order the events were written in");
    }

    [Fact]
    public async Task LastSuccessLoginDate_IsTheNewestSuccessfulSignInByDate()
    {
        var stack = await GetStackAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);
        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        await using (var context = await scope.Services.GetRequiredService<IDbContextFactory<MessagesContext>>().CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            // In the order they are written: the latest sign-in, a failed one later still, an earlier sign-in.
            foreach (var (date, action) in new[]
                     {
                         (_future.AddDays(20), MessageAction.LoginSuccess),
                         (_future.AddDays(30), MessageAction.LoginFailInvalidCombination),
                         (_future.AddDays(10), MessageAction.LoginSuccessViaApi)
                     })
            {
                context.LoginEvents.Add(new DbLoginEvent { TenantId = portal.TenantId, UserId = portal.Owner.Id, Date = date, Action = (int)action, Active = true });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
        }

        var lastLoginOn = await scope.Services.GetRequiredService<LoginEventsRepository>().GetLastSuccessEventDateAsync(portal.TenantId);

        lastLoginOn?.Date.Should().Be(_future.AddDays(20), "a failed sign-in is no activity, and the newest is the latest date");
    }

    [Fact]
    public async Task LastDates_OfAPortalWithoutEvents_AreNull()
    {
        var stack = await GetStackAsync();

        using var scope = await LetterScope.OpenAsync(stack, CultureInfo.GetCultureInfo(LetterCultures.DefaultCultureName));

        // An id no portal has, so no event is ever written for it.
        const int noPortal = int.MaxValue - 6;

        (await scope.Services.GetRequiredService<AuditEventsRepository>().GetLastEventDateAsync(noPortal)).Should().BeNull();
        (await scope.Services.GetRequiredService<LoginEventsRepository>().GetLastSuccessEventDateAsync(noPortal)).Should().BeNull();
    }
}
