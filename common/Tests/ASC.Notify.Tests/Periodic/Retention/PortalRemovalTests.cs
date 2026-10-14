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
/// The removal of a portal, up to the purge it starts: once the portal is taken down, nothing comes back to
/// it, so whatever fails on the way must not keep the purge from starting.
/// </summary>
public class PortalRemovalTests
{
    private static async ValueTask<LetterStackFixture> GetStackAsync()
    {
        return await TestContext.Current.GetFixture<LetterStackFixture>()
            ?? throw new InvalidOperationException(
                $"No stack in the test context. {nameof(LetterStackFixture)} is registered with "
                + "[assembly: AssemblyFixture] and starts before any letter test runs.");
    }

    [Fact]
    public async Task Removal_WhoseLettersFail_StillStartsThePurge()
    {
        var stack = await GetStackAsync();

        using var portal = await stack.CreatePortalAsync(TestContext.Current.CancellationToken);
        using var scope = stack.Host.CreateScope();

        var services = scope.ServiceProvider;
        var tenant = await services.GetRequiredService<TenantManager>().SetCurrentTenantAsync(portal.TenantId);
        var securityContext = services.GetRequiredService<SecurityContext>();

        var logger = new RecordingLogger<PortalRemovalService>();
        var eventBus = new RecordingEventBus();
        var removal = ActivatorUtilities.CreateInstance<PortalRemovalService>(services, logger, eventBus);

        try
        {
            // The request to the identity service carries a token issued for the current account, as in the daily job.
            await securityContext.AuthenticateMeWithoutCookieAsync(tenant.OwnerId);

            await removal.RemoveAsync(tenant, Guid.Empty, auto: true, () => throw new InvalidOperationException("The letters could not be queued"));
        }
        finally
        {
            securityContext.Logout();
        }

        eventBus.Published.Should().ContainSingle("the purge starts whatever failed before it")
            .Which.Should().BeOfType<RemovePortalIntegrationEvent>()
            .Which.TenantId.Should().Be(portal.TenantId);
        logger.Messages.Should().ContainSingle(m => m.Contains("a step before the purge failed"));
    }

    /// <summary>Keeps the events it is asked to publish and publishes none, so the portal is not purged for real.</summary>
    private sealed class RecordingEventBus : IEventBus
    {
        public List<IntegrationEvent> Published { get; } = [];

        public Task PublishAsync(IntegrationEvent @event)
        {
            Published.Add(@event);

            return Task.CompletedTask;
        }

        public Task SubscribeAsync<T, TH>() where T : IntegrationEvent where TH : IIntegrationEventHandler<T> => throw new NotSupportedException();

        public Task SubscribeDynamicAsync<TH>(string eventName) where TH : IDynamicIntegrationEventHandler => throw new NotSupportedException();

        public void UnsubscribeDynamic<TH>(string eventName) where TH : IDynamicIntegrationEventHandler => throw new NotSupportedException();

        public void Unsubscribe<T, TH>() where TH : IIntegrationEventHandler<T> where T : IntegrationEvent => throw new NotSupportedException();
    }
}
