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
/// GET /api/2.0/settings/webhooks/log — the webhook delivery log. A DocSpace admin reads every record of the
/// portal; any other member except a Guest reads only the records of the subscriptions they created, whatever
/// <c>configId</c> or <c>userId</c> they ask for, because a record holds the whole payload that was sent. An
/// anonymous caller is rejected outright. The filters themselves are covered by <see cref="WebhookLogFilterTests"/>.
/// </summary>
[Trait("Category", "Settings")]
public class GetWebhooksLogsTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task GetWebhooksLogs_Owner_ReturnsList()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var logs = await _webhooksApi.GetWebhooksLogsAsync(
            count: 10, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        logs.StatusCode.Should().Be(200);
        logs.Response.Should().NotBeNull();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetWebhooksLogs_RoomAdminOrUser_ReadsOnlyOwnRecords(EmployeeType employeeType)
    {
        // Arrange
        var (member, ownerSubscription, memberSubscription) = await CreateRecordsOfOwnerAndMemberAsync(employeeType);

        // Act
        await _webApiClient.Authenticate(member);
        var logs = (await _webhooksApi.GetWebhooksLogsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        logs.Should().NotBeEmpty();
        logs.Should().OnlyContain(l => l.ConfigName == memberSubscription.Name,
            "the records of {0}, the owner's subscription, hold payloads the member did not subscribe to", ownerSubscription.Name);
    }

    [Fact]
    public async Task GetWebhooksLogs_User_AsksForAnotherMembersSubscription_GetsNothing()
    {
        // Arrange
        var (member, ownerSubscription, _) = await CreateRecordsOfOwnerAndMemberAsync(EmployeeType.User);

        // Act
        await _webApiClient.Authenticate(member);
        var logs = (await _webhooksApi.GetWebhooksLogsAsync(
            configId: ownerSubscription.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        logs.Should().BeEmpty("configId narrows the caller's own records, it does not open another member's");
    }

    [Fact]
    public async Task GetWebhooksLogs_User_AsksForAnotherMembersRecords_GetsOnlyOwn()
    {
        // Arrange
        var (member, _, memberSubscription) = await CreateRecordsOfOwnerAndMemberAsync(EmployeeType.User);

        // Act
        await _webApiClient.Authenticate(member);
        var logs = (await _webhooksApi.GetWebhooksLogsAsync(
            userId: Owner.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        logs.Should().NotBeEmpty();
        logs.Should().OnlyContain(l => l.ConfigName == memberSubscription.Name, "userId is forced to the caller for anyone but an admin");
    }

    [Fact]
    public async Task GetWebhooksLogs_Admin_ReadsTheRecordsOfEveryMember()
    {
        // Arrange
        var (_, ownerSubscription, memberSubscription) = await CreateRecordsOfOwnerAndMemberAsync(EmployeeType.User);
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        // Act
        await _webApiClient.Authenticate(admin);
        var logs = (await _webhooksApi.GetWebhooksLogsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        logs.Select(l => l.ConfigName).Should().Contain([ownerSubscription.Name, memberSubscription.Name]);
    }

    [Fact]
    public async Task GetWebhooksLogs_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.GetWebhooksLogsAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task GetWebhooksLogs_Guest_ThrowsAccessDenied()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _webhooksApi.GetWebhooksLogsAsync(cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    /// <summary>
    /// A member of <paramref name="employeeType"/> and the owner each subscribe to <c>file.created</c>, and the owner
    /// creates a file in a room the member can read, so both subscriptions get a delivery record of the same event.
    /// Returns once the owner can see both records.
    /// </summary>
    private async Task<(User Member, WebhookSubscription OwnerSubscription, WebhookSubscription MemberSubscription)> CreateRecordsOfOwnerAndMemberAsync(
        EmployeeType employeeType)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var member = await InviteContact(employeeType);

        await _filesClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: $"room-{Guid.NewGuid():N}", roomType: RoomType.CustomRoom), cancellationToken)).Response;
        await _roomsApi.SetRoomSecurityAsync(
            room.Id,
            new RoomInvitationRequest(invitations: [new RoomInvitation(member.Id, DocSpace.API.SDK.Model.FileShare.Read)], notify: false),
            cancellationToken);

        var memberSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated, member);
        var ownerSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.FileCreated);

        await _filesClient.Authenticate(Owner);
        await _filesApi.CreateFileAsync(room.Id, new CreateFileRequest($"file-{Guid.NewGuid():N}.docx"), cancellationToken);

        (await WaitForRecordAsync(ownerSubscription.Id, _ => true)).Should().NotBeNull("the owner's subscription gets the event");
        (await WaitForRecordAsync(memberSubscription.Id, _ => true)).Should().NotBeNull("the member may read the room the file is in");

        return (member, ownerSubscription, memberSubscription);
    }
}
