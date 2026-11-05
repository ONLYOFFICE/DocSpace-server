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
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserUpdated);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull("the owner may see every member");
        payload!.Model.UsedSpace.Should().NotBeNull();
        payload.Model.Groups.Should().NotBeNull();

        payload.Model.CreatedBy.Id.Should().Be(Owner.Id);
        payload.Json.GetProperty("createdBy").TryGetProperty("email", out _).Should().BeFalse("a summary of another user never carries their email");
    }

    [Fact]
    public async Task UserUpdated_RoomAdminSubscription_AboutAnotherMember_OmitsQuotaAndCreatedBy()
    {
        // Arrange
        var subject = await CreateMemberInGroupAsync();
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserUpdated, roomAdmin);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull("a room admin may see the members of the portal");
        payload!.Model.UsedSpace.Should().BeNull();
        payload.Model.QuotaLimit.Should().BeNull();
        payload.Model.IsCustomQuota.Should().BeNull();
        payload.Model.CreatedBy.Should().BeNull();
        payload.Model.Groups.Should().NotBeNull("a room admin reads the groups of a member through the REST API too");
    }

    [Fact]
    public async Task UserUpdated_UserSubscription_AboutThemselves_KeepsQuotaOmitsCreatedBy()
    {
        // Arrange
        var subject = await CreateMemberInGroupAsync();
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserUpdated, subject);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull("a member gets the events about themselves");
        payload!.Model.UsedSpace.Should().NotBeNull();
        payload.Model.Groups.Should().NotBeNull();
        payload.Model.CreatedBy.Should().BeNull();
    }

    [Fact]
    public async Task UserUpdated_UserSubscription_AboutAnotherMember_IsNotPublished()
    {
        // Arrange
        var subject = await InviteContact(EmployeeType.User);
        var member = await InviteContact(EmployeeType.User);
        var memberSubscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserUpdated, member);
        var ownerSubscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserUpdated);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert: both subscriptions are served by the same publication, so once the owner's has the event
        // the member's would have it too
        (await WaitForUserPayloadAsync(ownerSubscription.Id, subject, firstName)).Should().NotBeNull();

        (await GetLogAsync(memberSubscription.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task UserUpdated_Payload_AvatarsAreAbsoluteUrls()
    {
        // Arrange
        var subject = await InviteContact(EmployeeType.User);
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserUpdated);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert: a receiver has no portal address to resolve a relative path against
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull();

        var p = payload!.Model;

        foreach (var url in new[] { p.Avatar, p.AvatarOriginal, p.AvatarMax, p.AvatarMedium, p.AvatarSmall })
        {
            ShouldBeAbsoluteUrl(url, "every avatar must be usable outside the portal");
        }

        ShouldBeAbsoluteUrl(p.CreatedBy.Avatar, "so must the avatar of a user summary");
        ShouldBeAbsoluteUrl(p.ProfileUrl, "so must the profile link the owner gets");
    }

    [Fact]
    public async Task UserUpdated_Payload_RegistrationDateIsUtcInWholeSeconds()
    {
        // Arrange: a member whose registration date is read back from the database for the update
        var subject = await InviteContact(EmployeeType.User);
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserUpdated);

        // Act
        var firstName = await RenameAsync(subject);

        // Assert: a stored date is UTC; read as the server's local time it would be shifted by the server's offset
        var payload = await WaitForUserPayloadAsync(subscription.Id, subject, firstName);

        payload.Should().NotBeNull();

        // the format on the wire, which a parsed DateTime no longer shows
        var registrationDate = payload!.Json.GetProperty("registrationDate").GetString();
        registrationDate.Should().MatchRegex(@"^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ$", "every timestamp goes out in UTC, in whole seconds");
        DateTimeOffset.Parse(registrationDate!, CultureInfo.InvariantCulture).Should()
            .BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(10), "the member registered moments ago");
    }

    [Fact]
    public async Task UserInvited_Payload_DescribesThePendingMember()
    {
        // Arrange
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserInvited);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        await _peopleClient.Authenticate(Owner);
        await _profilesApi.InviteUsersAsync(
            new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = email }]),
            TestContext.Current.CancellationToken);

        // Assert: the invitee exists only as a pending member with an email until they accept
        var payload = await WaitForEventPayloadAsync<UserPayload>(
            subscription.Id, "user.invited", p => p.Email == email);

        payload.Should().NotBeNull("an invitation by email publishes user.invited");
        payload!.Model.Id.Should().NotBeEmpty();
        payload.Model.CreatedBy.Id.Should().Be(Owner.Id, "the owner may see who invited them");
    }

    private async Task<User> CreateMemberInGroupAsync()
    {
        var subject = await InviteContact(EmployeeType.User);

        await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: $"group-{Guid.NewGuid():N}", groupManager: Owner.Id, members: [subject.Id]),
            TestContext.Current.CancellationToken);

        return subject;
    }

    private Task<DeliveredPayload<UserPayload>?> WaitForUserPayloadAsync(int configId, User subject, string firstName)
    {
        return WaitForPayloadAsync<UserPayload>(configId, p => p.Id == subject.Id && p.FirstName == firstName);
    }
}
