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
/// The HEAD probe that create, update and switch-on send to the target, when the target gives no answer at all: the
/// connection is refused, or the TLS handshake fails. The probe used to let the transport exception escape, so the
/// call returned 500; it is now refused with 400 and the same text as a target that answers with an error code, and
/// nothing is stored. A target that stays silent past the client's 10-second timeout takes the same path; it is not
/// covered here, because the receiver answers every probe at once.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookTargetUnreachableTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    private const string NotAvailableText = "Webhook URL is not available.";

    [Fact]
    public async Task CreateWebhook_TargetRefusesTheConnection_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateWebhookDto();
        dto.Uri = ClosedPortUri();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(NotAvailableText);
        await ShouldHaveNoSubscriptionNamedAsync(dto.Name);
    }

    /// <summary>
    /// The receiver speaks plain http, so an https address on its port fails the TLS handshake - the transport error a
    /// target with an invalid certificate gives too.
    /// </summary>
    [Fact]
    public async Task CreateWebhook_TargetFailsTheTlsHandshake_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateWebhookDto(ssl: true);
        dto.Uri = HttpsOnPlainReceiver(CreateReceiver());

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(NotAvailableText);
        await ShouldHaveNoSubscriptionNamedAsync(dto.Name);
    }

    [Fact]
    public async Task UpdateWebhook_NewTargetRefusesTheConnection_ThrowsBadRequestAndKeepsTheOldTarget()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver());

        var update = new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = ClosedPortUri(),
            SecretKey = subscription.SecretKey,
            Enabled = true
        };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(update, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(NotAvailableText);
        (await GetSubscriptionAsync(subscription.Id))!.Uri.Should().Be(subscription.Uri);
    }

    /// <summary>
    /// Switching on probes the stored address. A stored target that has stopped answering is a state no API call
    /// produces on demand - the update probes it too - so the address is changed in the database (tests.md).
    /// </summary>
    [Fact]
    public async Task EnableWebhook_StoredTargetRefusesTheConnection_ThrowsBadRequestAndStaysOff()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver());
        var toggle = new UpdateWebhooksConfigRequestDto(subscription.Id)
        {
            Name = subscription.Name,
            Uri = subscription.Uri,
            SecretKey = subscription.SecretKey,
            Enabled = false
        };
        await _webhooksApi.EnableWebhookAsync(toggle, TestContext.Current.CancellationToken);

        var changed = await ExecuteSqlAsync(
            "UPDATE webhooks_config SET uri = @uri WHERE id = @id", ("@uri", ClosedPortUri()), ("@id", subscription.Id));
        changed.Should().Be(1);

        toggle.Enabled = true;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.EnableWebhookAsync(toggle, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain(NotAvailableText);
        (await GetSubscriptionAsync(subscription.Id))!.Enabled.Should().BeFalse();
    }

    /// <summary>
    /// An address on the receiver's loopback alias, which the integration-test blacklist leaves open, at a port nothing
    /// listens on: the port is taken from the system and released at once, so the connection is refused.
    /// </summary>
    private static string ClosedPortUri()
    {
        var listener = new TcpListener(IPAddress.Parse(ASC.AppHost.Configuration.Constants.WebhookReceiverHost), 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return $"http://{ASC.AppHost.Configuration.Constants.WebhookReceiverHost}:{port}/hooks/closed";
    }

    /// <summary>The address of <paramref name="receiver"/> with the scheme switched to https.</summary>
    private static string HttpsOnPlainReceiver(WebhookReceiver receiver)
    {
        return Uri.UriSchemeHttps + receiver.TargetUri[Uri.UriSchemeHttp.Length..];
    }

    private async Task ShouldHaveNoSubscriptionNamedAsync(string name)
    {
        var list = await _webhooksApi.GetTenantWebhooksAsync(TestContext.Current.CancellationToken);

        list.Response.Should().NotContain(w => w.Configs.Name == name);
    }
}
