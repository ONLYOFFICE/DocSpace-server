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
/// A deletion event describes the user or group as it was right before it went: the payload is built
/// before the delete (UserWebhookManager.PublishAfterAsync) and sent after it, so what the delete clears — group
/// memberships, the member list — is still in it.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookDeletedPayloadTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task UserDeleted_Payload_DescribesTheUserAsTheyWere()
    {
        // Arrange
        var subject = await InviteContact(EmployeeType.User);
        var groupName = $"group-{Guid.NewGuid():N}";
        await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: groupName, groupManager: Owner.Id, members: [subject.Id]),
            TestContext.Current.CancellationToken);
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.UserDeleted);

        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated, new UpdateMembersRequestDto([subject.Id], false), TestContext.Current.CancellationToken);

        // Act
        await _profilesApi.DeleteMemberAsync(subject.Id.ToString(), TestContext.Current.CancellationToken);

        // Assert
        var payload = await WaitForPayloadAsync<UserPayload>(subscription.Id, p => p.Id == subject.Id);

        payload.Should().NotBeNull();
        payload!.Model.Email.Should().Be(subject.Email);
        payload.Model.DisplayName.Should().NotBeNullOrEmpty();
        payload.Model.Groups
            .Select(g => g.Name)
            .Should().Contain(groupName, "the membership the delete removes is part of the user it removes");
    }

    [Fact]
    public async Task GroupDeleted_Payload_DescribesTheGroupAsItWas()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var groupName = $"group-{Guid.NewGuid():N}";
        var group = await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: groupName, groupManager: Owner.Id, members: [member.Id]),
            TestContext.Current.CancellationToken);
        var subscription = await CreateSubscriptionAsync(CreateReceiver(), WebhookTrigger.GroupDeleted);

        // Act
        await _peopleClient.Authenticate(Owner);
        await _groupApi.DeleteGroupAsync(group.Response.Id, TestContext.Current.CancellationToken);

        // Assert
        var payload = await WaitForPayloadAsync<GroupPayload>(subscription.Id, p => p.Id == group.Response.Id);

        payload.Should().NotBeNull();
        payload!.Model.Name.Should().Be(groupName);
        payload.Model.Manager.Id.Should().Be(Owner.Id);
        payload.Model.MembersCount.Should().BeGreaterThan(0, "the members are counted before the delete removes them");
    }
}
