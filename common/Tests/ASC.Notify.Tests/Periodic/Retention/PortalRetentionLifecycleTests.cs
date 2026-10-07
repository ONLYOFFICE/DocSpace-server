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
/// The retention policy for real, on a portal of its own: the job blocks it, Web.Api stops serving it, the
/// owner's link brings it back, and a portal nobody brings back is removed. Each case registers a fresh
/// portal, because the one the letter tests share must never be blocked.
/// </summary>
public class PortalRetentionLifecycleTests
{
    private static readonly string _senderName = ASC.Core.Configuration.Constants.NotifyEMailSenderSysName;

    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    /// <summary>A service scope working for <paramref name="tenantId"/>, the way the daily job's is.</summary>
    private static async Task<IServiceScope> OpenScopeAsync(LetterStackFixture stack, int tenantId)
    {
        var scope = stack.Host.CreateScope();

        scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().ServerUri = LetterEnvironment.PublishedUrl;
        await scope.ServiceProvider.GetRequiredService<TenantManager>().SetCurrentTenantAsync(tenantId);

        return scope;
    }

    /// <summary>A free portal last used on <paramref name="lastActivity"/>, looked at on <paramref name="today"/>.</summary>
    private static PeriodicLetterContext Free(Tenant tenant, DateTime today, DateTime lastActivity)
    {
        return PeriodicLetterContexts.Fresh(tenant, today) with
        {
            Quota = PeriodicLetterContexts.Quota(free: true),
            Tariff = new Tariff { Quotas = [], State = TariffState.Paid, DueDate = DateTime.MaxValue, DelayDueDate = DateTime.MaxValue },
            LastActivity = PeriodicLetterContexts.Activity(lastActivity)
        };
    }

    /// <summary>Blocks the portal the way the daily job does when it has been idle past its threshold.</summary>
    private static async Task<Tenant> BlockAsync(IServiceScope scope, int tenantId, RecordingNotifyClient client)
    {
        var services = scope.ServiceProvider;
        var today = DateTime.UtcNow.Date;
        var tenant = await services.GetRequiredService<TenantManager>().GetTenantAsync(tenantId);

        var leaveAlone = await services.GetRequiredService<PortalRetentionJob>()
            .ApplyAsync(Free(tenant, today, today.AddDays(-60)), today.AddYears(-1), client, _senderName);

        leaveAlone.Should().BeTrue("a portal that has just been blocked gets none of the ordinary letters");

        return tenant;
    }

    /// <summary>The portal's status as the database has it, past every cache.</summary>
    private static async Task<bool> HasStatusAsync(IServiceScope scope, int tenantId, TenantStatus status)
    {
        var tenants = await scope.ServiceProvider.GetRequiredService<TenantManager>().GetTenantsByStatusAsync(status);

        return tenants.Any(t => t.Id == tenantId);
    }

    /// <summary>Asks Web.Api a question, again and again until the answer is the one expected or time is up.</summary>
    private static async Task<HttpStatusCode> PollAsync(Func<Task<HttpResponseMessage>> request, HttpStatusCode expected)
    {
        // The status reaches Web.Api through its tenant cache, which another process has just invalidated.
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            using var response = await request();

            if (response.StatusCode == expected || DateTime.UtcNow >= deadline)
            {
                return response.StatusCode;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task IdlePortal_IsBlocked_StopsAnswering_AndComesBackWithTheOwnersLink()
    {
        var stack = await GetStackAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var portal = await stack.CreatePortalAsync(cancellationToken);

        string confirm;

        using (var scope = await OpenScopeAsync(stack, portal.TenantId))
        {
            var client = new RecordingNotifyClient();
            var tenant = await BlockAsync(scope, portal.TenantId, client);

            (await HasStatusAsync(scope, portal.TenantId, TenantStatus.Blocked)).Should().BeTrue();

            client.Sent.Should().ContainSingle()
                .Which.Action.Should().BeOfType<SaasOwnerRetentionBlockedNotifyAction>("the owner is told at the moment of the block");

            // The link the letters carry for a portal that may be unblocked from them.
            var owner = await scope.ServiceProvider.GetRequiredService<UserManager>().GetUsersAsync(tenant.OwnerId);
            var link = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalUnblock);

            confirm = new Uri(link).Query.TrimStart('?');
        }

        var http = portal.WebApiHttpClient;

        (await PollAsync(() => http.GetAsync("api/2.0/settings/cultures", cancellationToken), HttpStatusCode.NotFound))
            .Should().Be(HttpStatusCode.NotFound, "a blocked portal answers nothing but what its blocked page needs");

        (await PollAsync(() => http.GetAsync("api/2.0/settings/colortheme", cancellationToken), HttpStatusCode.OK))
            .Should().Be(HttpStatusCode.OK, "the blocked page is drawn in the portal's own colors");

        using (var unblock = new HttpRequestMessage(HttpMethod.Put, "api/2.0/portal/unblock"))
        {
            unblock.Headers.Add("confirm", confirm);

            using var response = await http.SendAsync(unblock, cancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(cancellationToken));
        }

        using (var scope = await OpenScopeAsync(stack, portal.TenantId))
        {
            (await HasStatusAsync(scope, portal.TenantId, TenantStatus.Active)).Should().BeTrue("the owner's link brings the portal back");

            (await scope.ServiceProvider.GetRequiredService<SettingsManager>().LoadAsync<PortalRetentionBlockSettings>(portal.TenantId)).Category
                .Should().BeNull("the category of the block goes with the block");
        }

        (await PollAsync(() => http.GetAsync("api/2.0/settings/cultures", cancellationToken), HttpStatusCode.OK))
            .Should().Be(HttpStatusCode.OK, "an unblocked portal answers as before");
    }

    [Fact]
    public async Task UnblockLink_IsRefusedOnAPortalThatIsNotBlocked()
    {
        var stack = await GetStackAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var portal = await stack.CreatePortalAsync(cancellationToken);

        string confirm;

        using (var scope = await OpenScopeAsync(stack, portal.TenantId))
        {
            var owner = await scope.ServiceProvider.GetRequiredService<UserManager>().GetUsersAsync(portal.Owner.Id);
            var link = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>().GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalUnblock);

            confirm = new Uri(link).Query.TrimStart('?');
        }

        using var unblock = new HttpRequestMessage(HttpMethod.Put, "api/2.0/portal/unblock");
        unblock.Headers.Add("confirm", confirm);

        using var response = await portal.WebApiHttpClient.SendAsync(unblock, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the link only opens a blocked portal");

        using var after = await OpenScopeAsync(stack, portal.TenantId);

        (await HasStatusAsync(after, portal.TenantId, TenantStatus.Active)).Should().BeTrue();
    }

    [Fact]
    public async Task UnblockWithoutTheLink_IsRefused()
    {
        var stack = await GetStackAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var portal = await stack.CreatePortalAsync(cancellationToken);

        using var response = await portal.WebApiHttpClient.PutAsync("api/2.0/portal/unblock", null, cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BlockedPortal_NobodyUnblocks_IsRemovedAtTheEndOfItsRetention()
    {
        var stack = await GetStackAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);
        using var scope = await OpenScopeAsync(stack, portal.TenantId);

        var services = scope.ServiceProvider;
        var tenant = await BlockAsync(scope, portal.TenantId, new RecordingNotifyClient());

        // A week before the deletion the owner is reminded; the deletion waits for that reminder.
        var reminderClient = new RecordingNotifyClient();
        var reminderDay = DateTime.UtcNow.Date.AddDays(23);

        await ActivatorUtilities.CreateInstance<PortalRetentionJob>(services, new RecordingLogger<PortalRetentionJob>())
            .ApplyAsync(Free(tenant, reminderDay, reminderDay.AddYears(-1)), reminderDay.AddYears(-2), reminderClient, _senderName);

        reminderClient.Sent.Should().ContainSingle()
            .Which.Action.Should().BeOfType<SaasOwnerRetentionDeletionReminderNotifyAction>();

        (await services.GetRequiredService<SettingsManager>().LoadAsync<PortalRetentionBlockSettings>(portal.TenantId)).FinalNoticeSentOn
            .Should().Be(reminderDay, "the deletion waits for the reminder, so the day it went out is kept");

        // Thirty days on, as the free schedule keeps a blocked portal.
        var client = new RecordingNotifyClient();
        var later = DateTime.UtcNow.Date.AddDays(30);

        var logger = new RecordingLogger<PortalRetentionJob>();
        var job = ActivatorUtilities.CreateInstance<PortalRetentionJob>(services, logger);

        var leaveAlone = await job.ApplyAsync(Free(tenant, later, later.AddYears(-1)), later.AddYears(-2), client, _senderName);

        leaveAlone.Should().BeTrue();
        logger.Messages.Should().ContainSingle(m => m.Contains("Free: Delete"), string.Join(" | ", logger.Messages));

        (await HasStatusAsync(scope, portal.TenantId, TenantStatus.RemovePending)).Should().BeTrue("the portal is taken down and its purge started");

        client.Sent.Should().ContainSingle()
            .Which.Action.Should().BeOfType<SaasOwnerRetentionDeletedNotifyAction>("the owner hears of the deletion");
    }

    [Fact]
    public async Task BlockedPortal_KeepsTheRetentionOfItsBlock_WhenItsWalletEmpties()
    {
        var stack = await GetStackAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);
        using var scope = await OpenScopeAsync(stack, portal.TenantId);

        var services = scope.ServiceProvider;
        var settingsManager = services.GetRequiredService<SettingsManager>();
        var tenant = await BlockAsync(scope, portal.TenantId, new RecordingNotifyClient());

        (await settingsManager.LoadAsync<PortalRetentionBlockSettings>(portal.TenantId)).Category
            .Should().Be(PortalRetentionCategory.Free, "the block records the category its letter was written for");

        // As if it had been blocked with money on its wallet: its letter promised ninety days. The stack has
        // no accounting service, so from here on its wallet reads as empty.
        await settingsManager.SaveAsync(new PortalRetentionBlockSettings { Category = PortalRetentionCategory.FreeWithBalance }, portal.TenantId);

        var client = new RecordingNotifyClient();
        var later = DateTime.UtcNow.Date.AddDays(30);

        var logger = new RecordingLogger<PortalRetentionJob>();
        var job = ActivatorUtilities.CreateInstance<PortalRetentionJob>(services, logger);

        var leaveAlone = await job.ApplyAsync(Free(tenant, later, later.AddYears(-1)), later.AddYears(-2), client, _senderName);

        leaveAlone.Should().BeTrue("a blocked portal gets none of the ordinary letters");
        logger.Messages.Should().BeEmpty("thirty days into a ninety-day retention is not a day of its schedule");
        client.Sent.Should().BeEmpty();

        (await HasStatusAsync(scope, portal.TenantId, TenantStatus.Blocked)).Should().BeTrue("the free schedule's thirty days do not apply to it");
    }
}
