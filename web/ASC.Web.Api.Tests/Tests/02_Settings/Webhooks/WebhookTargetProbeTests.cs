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
/// The checks of the target address on <c>POST</c> and <c>PUT api/2.0/settings/webhook</c>: http or https only, no
/// address in a blacklisted network, and a HEAD probe that has to answer with a success code itself — a redirect is
/// not followed, and a target that gives no answer is a 500. On an update the probe is sent only after the
/// subscription has been found and its owner checked (S5), so nobody can make the portal call an address by naming
/// another member's subscription. Every address here is the local receiver or is refused before any request.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookTargetProbeTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task CreateWebhook_FtpTarget_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateWebhookDto();
        dto.Uri = "ftp://127.0.0.2/hook";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Theory]
    [InlineData("http://10.1.2.3/hook")]
    [InlineData("http://172.16.0.1/hook")]
    [InlineData("http://192.168.0.10/hook")]
    public async Task CreateWebhook_TargetInAPrivateNetwork_ThrowsBadRequest(string uri)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateWebhookDto();
        dto.Uri = uri;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateWebhook_TargetThatDoesNotAnswer_ThrowsServerError()
    {
        // Arrange: nothing listens on port 1 of the receiver's address, so the connection is refused at once
        await _webApiClient.Authenticate(Owner);
        var dto = WebhooksTestData.CreateWebhookDto();
        dto.Uri = $"http://{ASC.AppHost.Configuration.Constants.WebhookReceiverHost}:1/hook";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);
    }

    [Fact]
    public async Task CreateWebhook_ProbeAnsweredWithARedirect_ThrowsBadRequestWithoutFollowingIt()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var receiver = WebhookReceiver.CreateRedirectingProbe();
        var dto = WebhooksTestData.CreateWebhookDto();
        dto.Uri = receiver.TargetUri;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.CreateWebhookAsync(dto, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        receiver.CountProbes().Should().Be(1);
        receiver.CountRedirectedRequests().Should().Be(0, "the probe must not follow the redirect");
    }

    [Fact]
    public async Task UpdateWebhook_OwnSubscription_ProbesTheNewTarget()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver());
        var target = CreateReceiver();

        // Act
        await _webhooksApi.UpdateWebhookAsync(UpdateTo(subscription.Id, target), TestContext.Current.CancellationToken);

        // Assert
        target.CountProbes().Should().Be(1);
    }

    /// <summary>
    /// S5: the probe used to go out before the ownership check, so a member could make the portal call any public
    /// address by naming another member's subscription; the lookup and the owner check now come first.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task UpdateWebhook_AnotherMembersSubscription_ThrowsAccessDeniedWithoutProbingTheTarget(EmployeeType employeeType)
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver());
        var member = await InviteContact(employeeType);
        var target = CreateReceiver();

        // Act
        await _webApiClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(UpdateTo(subscription.Id, target), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        target.CountProbes().Should().Be(0, "the target of another member's subscription must not be called");
    }

    [Fact]
    public async Task UpdateWebhook_UnknownId_ThrowsNotFoundWithoutProbingTheTarget()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var target = CreateReceiver();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.UpdateWebhookAsync(UpdateTo(int.MaxValue, target), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
        target.CountProbes().Should().Be(0);
    }

    private static UpdateWebhooksConfigRequestDto UpdateTo(int id, WebhookReceiver target)
    {
        return new UpdateWebhooksConfigRequestDto(id)
        {
            Name = $"webhook-{Initializer.Faker.Random.AlphaNumeric(10)}",
            Uri = target.TargetUri,
            SecretKey = Initializer.Faker.Random.AlphaNumeric(20),
            Enabled = true
        };
    }
}
