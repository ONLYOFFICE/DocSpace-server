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
/// The group payload (GroupWebhookDtoHelper) of group.created and group.updated: the group with its manager as a
/// summary and the number of members, never the member list, which can run to thousands. The payload is read from
/// the delivery record, which holds the body exactly as the subscription gets it.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookGroupPayloadTests(
    AspireAppFixture fixture)
    : WebhookDeliveryTestBase(fixture)
{
    [Fact]
    public async Task GroupCreated_Payload_CarriesTheManagerAndTheMemberCount()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync(), WebhookTrigger.GroupCreated);
        var groupName = $"group-{Guid.NewGuid():N}";

        // Act
        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: groupName, groupManager: Owner.Id, members: [member.Id]),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "group.created", p => p.GetProperty("id").GetGuid() == group.Id);

        payload.Should().NotBeNull();
        var p = payload!.Value;

        p.GetProperty("name").GetString().Should().Be(groupName);
        p.GetProperty("membersCount").GetInt32().Should().BeGreaterThan(0);
        p.TryGetProperty("members", out _).Should().BeFalse("the member list is read through the REST API, not carried");

        var manager = p.GetProperty("manager");
        manager.GetProperty("id").GetGuid().Should().Be(Owner.Id);
        manager.TryGetProperty("email", out _).Should().BeFalse("a summary of another user never carries their email");
        manager.TryGetProperty("profileUrl", out _).Should().BeTrue("the owner may see the contacts of every member");
        ShouldBeAbsoluteUrl(manager.GetProperty("avatar").GetString(), "the avatar of the manager must be usable outside the portal");
    }

    [Fact]
    public async Task GroupUpdated_Payload_CarriesTheNewNameAndCount()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        var newMember = await InviteContact(EmployeeType.User);
        // a catch-all subscription gets both events; the payloads are told apart by event.trigger
        var subscription = await CreateSubscriptionAsync(await CreateReceiverAsync());

        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: $"group-{Guid.NewGuid():N}", groupManager: Owner.Id, members: [member.Id]),
            TestContext.Current.CancellationToken)).Response;

        var created = await WaitForEventPayloadAsync(subscription.Id, "group.created", p => p.GetProperty("id").GetGuid() == group.Id);
        created.Should().NotBeNull();
        var countBefore = created!.Value.GetProperty("membersCount").GetInt32();

        var newName = $"renamed-{Guid.NewGuid():N}";

        // Act
        await _groupApi.UpdateGroupAsync(
            group.Id,
            new UpdateGroupRequest(membersToAdd: [newMember.Id], groupManager: Owner.Id, groupName: newName),
            TestContext.Current.CancellationToken);

        // Assert
        var payload = await WaitForEventPayloadAsync(subscription.Id, "group.updated", p => p.GetProperty("name").GetString() == newName);

        payload.Should().NotBeNull();
        var updated = payload!.Value;

        updated.GetProperty("id").GetGuid().Should().Be(group.Id);
        updated.GetProperty("membersCount").GetInt32().Should().Be(countBefore + 1, "the member added by the update is counted");
        updated.GetProperty("manager").GetProperty("id").GetGuid().Should().Be(Owner.Id);
    }
}
