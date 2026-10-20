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
/// A user event is cut for each subscription owner by the rules the REST API applies to a reader
/// (UserWebhookDtoHelper.RestrictForSubscriberAsync): quota only to a <c>DocSpaceAdmin</c> or the user
/// themselves, <c>createdBy</c> only to a <c>DocSpaceAdmin</c>. A <c>User</c> gets no event about anybody
/// else at all (WebhookUserAccessChecker). The payload is read from the delivery record, which holds the
/// body exactly as the subscription gets it.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookUserPayloadTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task UserUpdated_AdminSubscription_CarriesQuotaGroupsAndCreatedBy()
    {
        // Arrange
        var subject = await CreateMemberInGroupAsync();
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserUpdated);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull("the owner may see every member");
        payload!.Value.TryGetProperty("usedSpace", out _).Should().BeTrue();
        payload.Value.TryGetProperty("groups", out _).Should().BeTrue();

        payload.Value.TryGetProperty("createdBy", out var createdBy).Should().BeTrue();
        createdBy.GetProperty("id").GetGuid().Should().Be(Owner.Id);
        createdBy.TryGetProperty("email", out _).Should().BeFalse("a summary of another user never carries their email");
    }

    [Fact]
    public async Task UserUpdated_RoomAdminSubscription_AboutAnotherMember_OmitsQuotaAndCreatedBy()
    {
        // Arrange
        var subject = await CreateMemberInGroupAsync();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserUpdated, roomAdmin);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull("a room admin may see the members of the portal");
        payload!.Value.TryGetProperty("usedSpace", out _).Should().BeFalse();
        payload.Value.TryGetProperty("quotaLimit", out _).Should().BeFalse();
        payload.Value.TryGetProperty("isCustomQuota", out _).Should().BeFalse();
        payload.Value.TryGetProperty("createdBy", out _).Should().BeFalse();
        payload.Value.TryGetProperty("groups", out _).Should().BeTrue("a room admin reads the groups of a member through the REST API too");
    }

    [Fact]
    public async Task UserUpdated_UserSubscription_AboutThemselves_KeepsQuotaOmitsCreatedBy()
    {
        // Arrange
        var subject = await CreateMemberInGroupAsync();
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserUpdated, subject);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull("a member gets the events about themselves");
        payload!.Value.TryGetProperty("usedSpace", out _).Should().BeTrue();
        payload.Value.TryGetProperty("groups", out _).Should().BeTrue();
        payload.Value.TryGetProperty("createdBy", out _).Should().BeFalse();
    }

    [Fact]
    public async Task UserUpdated_UserSubscription_AboutAnotherMember_IsNotPublished()
    {
        // Arrange
        var subject = await InviteContact(EmployeeType.User);
        var member = await InviteContact(EmployeeType.User);
        var memberSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserUpdated, member);
        var ownerSubscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.UserUpdated);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert: both subscriptions are served by the same publication, so once the owner's has the event
        // the member's would have it too
        (await WaitForUserPayloadAsync(ownerSubscription.Id, subject, firstName)).Should().NotBeNull();

        (await GetDeliveryRecordsAsync(memberSubscription.Id)).Should().BeEmpty();
    }

    private async Task<User> CreateMemberInGroupAsync()
    {
        var subject = await InviteContact(EmployeeType.User);

        await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: $"group-{Guid.NewGuid():N}", groupManager: Owner.Id, members: [subject.Id]),
            TestContext.Current.CancellationToken);

        return subject;
    }

    /// <summary>
    /// <paramref name="subject"/> renames themselves, which publishes <c>user.updated</c>. Only the user
    /// themselves can change the name: the same call made by the owner leaves it as it is and publishes
    /// nothing.
    /// </summary>
    private async Task<string> RenameAsync(User subject)
    {
        var firstName = $"Renamed{Initializer.Faker.Random.String2(8, "abcdefghijklmnopqrstuvwxyz")}";

        await _peopleClient.Authenticate(subject);
        await _profilesApi.UpdateMemberAsync(
            subject.Id.ToString(), new UpdateMemberRequestDto { FirstName = firstName }, TestContext.Current.CancellationToken);

        return firstName;
    }

    private Task<JsonElement?> WaitForUserPayloadAsync(int configId, User subject, string firstName)
    {
        return WaitForPayloadAsync(configId, p =>
            p.TryGetProperty("id", out var id) && id.GetGuid() == subject.Id
            && p.TryGetProperty("firstName", out var name) && name.GetString() == firstName);
    }
}
