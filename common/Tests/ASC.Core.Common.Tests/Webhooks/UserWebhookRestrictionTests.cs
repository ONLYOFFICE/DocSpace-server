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

namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// The <c>user.*</c> payload as <see cref="UserWebhookDtoHelper.RestrictForSubscriberAsync"/> cuts it for one
/// subscription owner. A User or a Guest has no access to the contacts of other users, so about anyone but themselves
/// they get neither the email address, the phone, the additional contacts, the groups and department, nor the user
/// name and profile link, which give the email address away; a room admin keeps those, a DocSpace admin keeps
/// everything. The roles are what <see cref="UserManager"/> reads from the membership refs of the tenant, served here
/// by a stand-in user service; nothing else of the helper's dependencies is reached on this path.
/// </summary>
[Trait("Category", "Webhooks")]
public class UserWebhookRestrictionTests
{
    private const int TenantId = 1;

    private static readonly Guid _ownerId = new("0d8a7c3e-5b2f-4e19-a6d4-3c1b9e8f7a20");
    private static readonly Guid _subjectId = new("4f6e2d1c-9a8b-4c7d-b5e3-2a1f0e9d8c7b");

    [Fact]
    public async Task User_AboutSomeoneElse_LosesTheContacts()
    {
        var subscriberId = Guid.NewGuid();
        var payload = CreatePayload(_subjectId);

        var restricted = await CreateHelper(Member(subscriberId, ASC.Core.Users.Constants.GroupUser.ID))
            .RestrictForSubscriberAsync(payload, Subject(), subscriberId);

        ShouldHaveNoContacts(restricted);
        restricted.Id.Should().Be(_subjectId);
        restricted.DisplayName.Should().Be("Mike Zanyatski");
    }

    [Fact]
    public async Task Guest_AboutSomeoneElse_LosesTheContacts()
    {
        var subscriberId = Guid.NewGuid();

        var restricted = await CreateHelper(Member(subscriberId, ASC.Core.Users.Constants.GroupGuest.ID))
            .RestrictForSubscriberAsync(CreatePayload(_subjectId), Subject(), subscriberId);

        ShouldHaveNoContacts(restricted);
    }

    [Fact]
    public async Task User_AboutSomeoneElse_LeavesTheSharedPayloadIntact()
    {
        var subscriberId = Guid.NewGuid();
        var payload = CreatePayload(_subjectId);

        var restricted = await CreateHelper(Member(subscriberId, ASC.Core.Users.Constants.GroupUser.ID))
            .RestrictForSubscriberAsync(payload, Subject(), subscriberId);

        restricted.Should().NotBeSameAs(payload, "the payload is built once and cut for every subscriber of the event");
        ShouldHaveContacts(payload);
    }

    [Fact]
    public async Task User_AboutThemselves_KeepsTheContacts()
    {
        var restricted = await CreateHelper(Member(_subjectId, ASC.Core.Users.Constants.GroupUser.ID))
            .RestrictForSubscriberAsync(CreatePayload(_subjectId), Subject(), _subjectId);

        ShouldHaveContacts(restricted);
    }

    [Fact]
    public async Task RoomAdmin_AboutSomeoneElse_KeepsTheContacts()
    {
        // a room admin is a member in neither the User nor the Guest group
        var restricted = await CreateHelper([])
            .RestrictForSubscriberAsync(CreatePayload(_subjectId), Subject(), Guid.NewGuid());

        ShouldHaveContacts(restricted);
    }

    [Fact]
    public async Task DocSpaceAdmin_GetsThePayloadAsBuilt()
    {
        var payload = CreatePayload(_subjectId);

        var restricted = await CreateHelper([]).RestrictForSubscriberAsync(payload, Subject(), _ownerId);

        restricted.Should().BeSameAs(payload);
    }

    private static UserInfo Subject() => new() { Id = _subjectId, UserName = "mike.zanyatski" };

    private static UserWebhookDto CreatePayload(Guid id) => new()
    {
        Id = id,
        DisplayName = "Mike Zanyatski",
        UserName = "mike.zanyatski",
        Email = "mike.zanyatski@example.com",
        MobilePhone = "+15550100",
        Contacts = [new ContactWebhookDto { Type = "skype", Value = "mike.zanyatski" }],
        Department = "Sales",
        ProfileUrl = "https://example.onlyoffice.com/accounts/people/filter?search=mike.zanyatski%40example.com"
    };

    private static void ShouldHaveNoContacts(UserWebhookDto payload)
    {
        payload.Email.Should().BeNull();
        payload.MobilePhone.Should().BeNull();
        payload.Contacts.Should().BeNull();
        payload.Department.Should().BeNull();
        payload.Groups.Should().BeNull();
        payload.UserName.Should().BeNull("the user name is the local part of the email address");
        payload.ProfileUrl.Should().BeNull("the profile link is a people search by the email address");
    }

    private static void ShouldHaveContacts(UserWebhookDto payload)
    {
        payload.Email.Should().Be("mike.zanyatski@example.com");
        payload.MobilePhone.Should().Be("+15550100");
        payload.Contacts.Should().ContainSingle();
        payload.Department.Should().Be("Sales");
        payload.UserName.Should().Be("mike.zanyatski");
        payload.ProfileUrl.Should().NotBeNull();
    }

    /// <summary>The membership ref that gives <paramref name="userId"/> the role of <paramref name="groupId"/>.</summary>
    private static Dictionary<string, UserGroupRef> Member(Guid userId, Guid groupId) => new()
    {
        [UserGroupRef.CreateKey(TenantId, userId, groupId, UserGroupRefType.Contains)] =
            new UserGroupRef(userId, groupId, UserGroupRefType.Contains) { TenantId = TenantId }
    };

    /// <summary>
    /// The helper over a <see cref="UserManager"/> of a tenant owned by <see cref="_ownerId"/> whose membership refs
    /// are <paramref name="refs"/>. The dependencies the restriction does not reach are left out.
    /// </summary>
    private static UserWebhookDtoHelper CreateHelper(Dictionary<string, UserGroupRef> refs)
    {
        var tenantManager = new TenantManager(null!, null!, null!, null!, null!, null!, null!);
        tenantManager.SetCurrentTenant(new Tenant(TenantId, "test") { OwnerId = _ownerId });

        var userService = DispatchProxy.Create<IUserService, MembershipUserService>();
        ((MembershipUserService)(object)userService).Refs = refs;

        var userManager = new UserManager(
            userService, tenantManager, null!, null!, null!, null!, null!, null!, null!, null!, null!, null!,
            new ASC.Core.Users.Constants(new ConfigurationBuilder().Build()), null!, null!, null!, null!, null!, null!);

        return new UserWebhookDtoHelper(userManager, null!, null!, null!, null!, null!, tenantManager, null!, null!);
    }
}

/// <summary>
/// A user service that knows the membership refs of the tenant and nothing else; any other call fails the test.
/// </summary>
public class MembershipUserService : DispatchProxy
{
    public IDictionary<string, UserGroupRef> Refs { get; set; } = new Dictionary<string, UserGroupRef>();

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod?.Name == nameof(IUserService.GetUserGroupRefsAsync))
        {
            return Task.FromResult(Refs);
        }

        throw new NotSupportedException($"IUserService.{targetMethod?.Name} is not expected on this path");
    }
}
