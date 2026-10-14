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

    /// <summary>
    /// Blocks the portal the way the daily job does when it has been idle past its threshold: warned on the
    /// thirtieth day of a block on the sixtieth, and blocked on that day.
    /// </summary>
    private static async Task<Tenant> BlockAsync(IServiceScope scope, int tenantId, RecordingNotifyClient client)
    {
        var services = scope.ServiceProvider;
        var today = DateTime.UtcNow.Date;
        var tenant = await services.GetRequiredService<TenantManager>().GetTenantAsync(tenantId);
        var job = services.GetRequiredService<PortalRetentionJob>();

        await job.ApplyAsync(Free(tenant, today.AddDays(-30), today.AddDays(-60)), today.AddYears(-1), new RecordingNotifyClient(), _senderName);

        var leaveAlone = await job.ApplyAsync(Free(tenant, today, today.AddDays(-60)), today.AddYears(-1), client, _senderName);

        leaveAlone.Should().BeTrue("a portal that has just been blocked gets none of the ordinary letters");

        // The job blocks the portal as it reads it again, not the instance it was handed.
        return await services.GetRequiredService<TenantManager>().GetTenantAsync(tenantId);
    }

    /// <summary>The portal's status as the database has it, past every cache.</summary>
    private static async Task<bool> HasStatusAsync(IServiceScope scope, int tenantId, TenantStatus status)
    {
        var tenants = await scope.ServiceProvider.GetRequiredService<TenantManager>().GetTenantsByStatusAsync(status);

        return tenants.Any(t => t.Id == tenantId);
    }

    /// <summary>
    /// Asks Web.Api a question, again and again until the answer is the one expected or time is up, and
    /// returns the status of the last answer - the SDK's own for a success, its <see cref="ApiException"/>'s
    /// for a refusal.
    /// </summary>
    private static async Task<HttpStatusCode> PollAsync(Func<CancellationToken, Task<IApiResponse>> request, HttpStatusCode expected)
    {
        // The status reaches Web.Api through its tenant cache, which another process has just invalidated.
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            HttpStatusCode status;

            try
            {
                status = (await request(TestContext.Current.CancellationToken)).StatusCode;
            }
            catch (ApiException exception)
            {
                status = (HttpStatusCode)exception.ErrorCode;
            }

            if (status == expected || DateTime.UtcNow >= deadline)
            {
                return status;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Calls the unblocking with <paramref name="confirm"/> as its link. Raw for now: the pinned DocSpace.API.SDK
    /// predates PUT api/2.0/portal/unblock; move this to the SDK once it is regenerated.
    /// </summary>
    private static async Task<HttpStatusCode> UnblockAsync(LetterPortalClients portal, string? confirm)
    {
        using var unblock = new HttpRequestMessage(HttpMethod.Put, "api/2.0/portal/unblock");

        if (confirm is not null)
        {
            unblock.Headers.Add("confirm", confirm);
        }

        using var response = await portal.WebApiHttpClient.SendAsync(unblock, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }

    /// <summary>The query of the unblocking link the letters carry for the portal as it is now.</summary>
    private static async Task<string> UnblockLinkAsync(IServiceScope scope, int tenantId, string? keyPostfix = null)
    {
        var tenant = await scope.ServiceProvider.GetRequiredService<TenantManager>().GetTenantAsync(tenantId);
        var owner = await scope.ServiceProvider.GetRequiredService<UserManager>().GetUsersAsync(tenant.OwnerId);
        var link = scope.ServiceProvider.GetRequiredService<CommonLinkUtility>()
            .GetConfirmationEmailUrl(owner.Email, ConfirmType.PortalUnblock, keyPostfix ?? CommonLinkUtility.GetPortalUnblockKeyPostfix(tenant));

        return new Uri(link).Query.TrimStart('?');
    }

    [Fact]
    public async Task BlockLetter_KeptBackByAFailure_IsSentByTheNextRun()
    {
        var stack = await GetStackAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var portal = await stack.CreatePortalAsync(cancellationToken);
        using var scope = await OpenScopeAsync(stack, portal.TenantId);

        var services = scope.ServiceProvider;
        var tenantManager = services.GetRequiredService<TenantManager>();
        var settingsManager = services.GetRequiredService<SettingsManager>();
        var job = services.GetRequiredService<PortalRetentionJob>();
        var today = DateTime.UtcNow.Date;
        var policyStart = today.AddYears(-1);

        var tenant = await tenantManager.GetTenantAsync(portal.TenantId);

        await job.ApplyAsync(Free(tenant, today.AddDays(-30), today.AddDays(-60)), policyStart, new RecordingNotifyClient(), _senderName);

        // The portal is blocked, and the letter about it fails.
        var failing = new RecordingNotifyClient { Fails = action => action is SaasOwnerRetentionBlockedNotifyAction };

        await Assert.ThrowsAsync<InvalidOperationException>(() => job.ApplyAsync(Free(tenant, today, today.AddDays(-60)), policyStart, failing, _senderName));

        (await HasStatusAsync(scope, portal.TenantId, TenantStatus.Blocked)).Should().BeTrue();
        (await settingsManager.LoadAsync<PortalRetentionSettings>(portal.TenantId)).BlockNoticeSentOn.Should().BeNull();

        // The next run sends it, and keeps the day it did.
        tenant = await tenantManager.GetTenantAsync(portal.TenantId);

        var client = new RecordingNotifyClient();

        (await job.ApplyAsync(Free(tenant, today.AddDays(1), today.AddDays(-60)), policyStart, client, _senderName)).Should().BeTrue();

        client.Sent.Should().ContainSingle()
            .Which.Action.Should().BeOfType<SaasOwnerRetentionBlockedNotifyAction>("the owner hears of the block before any reminder of the deletion");
        (await settingsManager.LoadAsync<PortalRetentionSettings>(portal.TenantId)).BlockNoticeSentOn.Should().Be(today.AddDays(1));

        // And only once.
        var after = new RecordingNotifyClient();

        await job.ApplyAsync(Free(tenant, today.AddDays(2), today.AddDays(-60)), policyStart, after, _senderName);

        after.Sent.Should().BeEmpty();
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
            confirm = await UnblockLinkAsync(scope, tenant.Id);
        }

        var settingsApi = portal.CommonSettingsApi;

        (await PollAsync(async token => await settingsApi.GetSupportedCulturesWithHttpInfoAsync(token), HttpStatusCode.NotFound))
            .Should().Be(HttpStatusCode.NotFound, "a blocked portal answers nothing but what its blocked page needs");

        (await PollAsync(async token => await settingsApi.GetPortalColorThemeWithHttpInfoAsync(token), HttpStatusCode.OK))
            .Should().Be(HttpStatusCode.OK, "the blocked page is drawn in the portal's own colors");

        (await UnblockAsync(portal, confirm)).Should().Be(HttpStatusCode.OK);

        using (var scope = await OpenScopeAsync(stack, portal.TenantId))
        {
            (await HasStatusAsync(scope, portal.TenantId, TenantStatus.Active)).Should().BeTrue("the owner's link brings the portal back");

            (await scope.ServiceProvider.GetRequiredService<SettingsManager>().LoadAsync<PortalRetentionSettings>(portal.TenantId)).Category
                .Should().BeNull("the category of the block goes with the block");
        }

        (await PollAsync(async token => await settingsApi.GetSupportedCulturesWithHttpInfoAsync(token), HttpStatusCode.OK))
            .Should().Be(HttpStatusCode.OK, "an unblocked portal answers as before");

        // The unblocking moved the status date to today, the day of the block, so the key still matches: only the
        // status refuses the link now.
        (await UnblockAsync(portal, confirm)).Should().Be(HttpStatusCode.Unauthorized, "a repeated call finds the portal no longer blocked");
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
            // The key the link would carry for the portal as it is, so nothing but its status can refuse it.
            confirm = await UnblockLinkAsync(scope, portal.TenantId);
        }

        (await UnblockAsync(portal, confirm)).Should().Be(HttpStatusCode.Unauthorized, "the link only opens a blocked portal");

        using var after = await OpenScopeAsync(stack, portal.TenantId);

        (await HasStatusAsync(after, portal.TenantId, TenantStatus.Active)).Should().BeTrue();
    }

    [Fact]
    public async Task UnblockLink_OfAnotherBlock_IsRefused()
    {
        var stack = await GetStackAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var portal = await stack.CreatePortalAsync(cancellationToken);

        string confirm;

        string current;

        using (var scope = await OpenScopeAsync(stack, portal.TenantId))
        {
            var tenant = await BlockAsync(scope, portal.TenantId, new RecordingNotifyClient());

            // The link a letter about an earlier block carried: the same owner, the day of that block.
            confirm = await UnblockLinkAsync(scope, tenant.Id, tenant.StatusChangeDate.AddDays(-120).ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            current = await UnblockLinkAsync(scope, tenant.Id);
        }

        // Web.Api must see the block first, or the link would be refused for a portal it still takes for active.
        (await PollAsync(async token => await portal.CommonSettingsApi.GetSupportedCulturesWithHttpInfoAsync(token), HttpStatusCode.NotFound))
            .Should().Be(HttpStatusCode.NotFound);

        (await UnblockAsync(portal, confirm)).Should().Be(HttpStatusCode.Unauthorized, "a link opens only the block it was mailed for");

        using (var after = await OpenScopeAsync(stack, portal.TenantId))
        {
            (await HasStatusAsync(after, portal.TenantId, TenantStatus.Blocked)).Should().BeTrue();
        }

        (await UnblockAsync(portal, current)).Should().Be(HttpStatusCode.OK, "the link of this block opens it, so the other was refused for its block alone");
    }

    [Fact]
    public async Task UnblockWithoutTheLink_IsRefused()
    {
        var stack = await GetStackAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var portal = await stack.CreatePortalAsync(cancellationToken);

        (await UnblockAsync(portal, null)).Should().Be(HttpStatusCode.Unauthorized, "no link and no sign-in");

        await portal.WebApiHttpClient.Authenticate(portal.Owner);

        (await UnblockAsync(portal, null)).Should().Be(HttpStatusCode.Forbidden, "an ordinary sign-in, even the owner's, is not the link");
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
            .Which.Action.Should().BeOfType<SaasOwnerRetentionFinalReminderNotifyAction>();

        (await services.GetRequiredService<SettingsManager>().LoadAsync<PortalRetentionSettings>(portal.TenantId)).FinalNoticeSentOn
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

    /// <summary>
    /// The portal as a run holds it: a copy of its own, since the tenant cache hands every reader the same
    /// instance, and a change saved later would otherwise show through it.
    /// </summary>
    private static Tenant Snapshot(Tenant tenant)
    {
        return new Tenant(tenant.Id, tenant.Alias)
        {
            OwnerId = tenant.OwnerId,
            Name = tenant.Name,
            Language = tenant.Language,
            TimeZone = tenant.TimeZone,
            HostedRegion = tenant.HostedRegion,
            MappedDomain = tenant.MappedDomain,
            CreationDateTime = tenant.CreationDateTime,
            Status = tenant.Status,
            StatusChangeDate = tenant.StatusChangeDate
        };
    }

    [Fact]
    public async Task BlockedPortal_UnblockedAfterTheRunReadIt_IsNotRemoved()
    {
        var stack = await GetStackAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);
        using var scope = await OpenScopeAsync(stack, portal.TenantId);

        var services = scope.ServiceProvider;
        var tenantManager = services.GetRequiredService<TenantManager>();
        var blocked = Snapshot(await BlockAsync(scope, portal.TenantId, new RecordingNotifyClient()));

        // Reminded a week before its deletion, so nothing but its status stands between it and the deletion.
        var reminderDay = DateTime.UtcNow.Date.AddDays(23);

        await ActivatorUtilities.CreateInstance<PortalRetentionJob>(services, new RecordingLogger<PortalRetentionJob>())
            .ApplyAsync(Free(blocked, reminderDay, reminderDay.AddYears(-1)), reminderDay.AddYears(-2), new RecordingNotifyClient(), _senderName);

        // Brought back while the run still holds the portal as it found it: blocked, and with the reminder on
        // record, as the run had read it before the unblocking.
        var unblocked = await tenantManager.GetTenantAsync(portal.TenantId);

        unblocked.SetStatus(TenantStatus.Active);
        await tenantManager.SaveTenantAsync(unblocked);

        var client = new RecordingNotifyClient();
        var later = DateTime.UtcNow.Date.AddDays(30);

        var logger = new RecordingLogger<PortalRetentionJob>();
        var job = ActivatorUtilities.CreateInstance<PortalRetentionJob>(services, logger);

        await job.ApplyAsync(Free(blocked, later, later.AddYears(-1)), later.AddYears(-2), client, _senderName);

        logger.Messages.Should().Contain(m => m.Contains("Free: Delete"), "the portal it was handed is due for deletion");
        logger.Messages.Should().Contain(m => m.Contains("is not removed"), string.Join(" | ", logger.Messages));

        (await HasStatusAsync(scope, portal.TenantId, TenantStatus.Active)).Should().BeTrue("a portal unblocked in the meantime stays");

        client.Sent.Should().BeEmpty("the owner of a portal that stays is not told it is gone");
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

        (await settingsManager.LoadAsync<PortalRetentionSettings>(portal.TenantId)).Category
            .Should().Be(PortalRetentionCategory.Free, "the block records the category its letter was written for");

        // As if it had been blocked with money on its wallet: its letter promised ninety days. The stack has
        // no accounting service, so from here on its wallet reads as empty.
        await settingsManager.SaveAsync(new PortalRetentionSettings { Category = PortalRetentionCategory.FreeWithBalance, BlockNoticeSentOn = DateTime.UtcNow.Date }, portal.TenantId);

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
