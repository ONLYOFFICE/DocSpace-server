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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// Shared by the retry suites: a subscription that has really been delivered to once, so a retry has a
/// delivery record with an outcome to reset.
/// </summary>
public abstract class WebhookDeliveryTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Creates an enabled catch-all subscription as the owner and returns its id. The target is the same
    /// <c>example.com</c> the other webhook suites use, so a delivery ends with whatever status it answers.
    /// </summary>
    protected async Task<int> CreateSubscriptionAsync()
    {
        await _webApiClient.Authenticate(Owner);

        var config = await _webhooksApi.CreateWebhookAsync(
            WebhooksTestData.CreateWebhookDto(enabled: true), TestContext.Current.CancellationToken);

        return config.Response.Id;
    }

    /// <summary>
    /// Adds a member, which publishes <c>user.created</c> to every subscription of the portal.
    /// </summary>
    protected async Task FireEventAsync()
    {
        await InviteContact(EmployeeType.User);
    }

    /// <summary>
    /// Polls the delivery log of one subscription until it holds <paramref name="count"/> records that have
    /// each been attempted (a non-zero status), and returns the last observed records either way. The sender
    /// picks a due record up within seconds; the deadline is long because the target is on the internet.
    /// </summary>
    protected async Task<List<WebhooksLogDto>> WaitForAttemptedDeliveriesAsync(int configId, int count = 1)
    {
        await _webApiClient.Authenticate(Owner);

        var deadline = DateTime.UtcNow.AddSeconds(90);

        while (true)
        {
            var logs = (await _webhooksApi.GetWebhooksLogsAsync(
                configId: configId, cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (logs.Count(l => l.Status != 0) >= count || DateTime.UtcNow >= deadline)
            {
                return logs;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls one delivery record until it has been attempted (a non-zero status), and returns the last observed
    /// state either way.
    /// </summary>
    protected async Task<WebhooksLogDto?> WaitForAttemptAsync(int deliveryId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);

        while (true)
        {
            var record = (await _webhooksApi.GetWebhooksLogsAsync(
                eventId: deliveryId, cancellationToken: TestContext.Current.CancellationToken)).Response.SingleOrDefault();

            if (record is { Status: not 0 } || DateTime.UtcNow >= deadline)
            {
                return record;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// One attempted delivery of a fresh subscription, read as the owner.
    /// </summary>
    protected async Task<WebhooksLogDto> CreateAttemptedDeliveryAsync()
    {
        var configId = await CreateSubscriptionAsync();
        await FireEventAsync();

        var attempted = (await WaitForAttemptedDeliveriesAsync(configId)).Where(l => l.Status != 0).ToList();

        attempted.Should().ContainSingle("the sender should have attempted the delivery by now");

        return attempted[0];
    }
}
