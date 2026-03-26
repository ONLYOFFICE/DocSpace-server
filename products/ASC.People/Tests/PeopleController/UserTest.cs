// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.People.Tests.PeopleController;

[Collection("Test Collection")]
public class UserTest(PeopleFactory peopleFactory, WepApiFactory apiFactory) : BaseTest(peopleFactory, apiFactory)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79334")]
    public async Task CreateUser_AsRoomAdmin_ShouldCreateUserSuccessfully()
    {
        await _apiClient.Authenticate(Initializer.Owner);
        var roomAdmin = await Initializer.InviteContact(EmployeeType.RoomAdmin);
        await _peopleClient.Authenticate(roomAdmin);

        var fakeMember = Initializer.FakerMember.Generate();

        var user = await _profilesApi.AddMemberAsync(new MemberRequestDto
        {
            CultureName = "en-US",
            Spam = false,
            Type = EmployeeType.User,
            Email = fakeMember.Email,
            Password = fakeMember.Password,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName
        }, TestContext.Current.CancellationToken);

        user.Should().NotBeNull();
        user.Response.FirstName.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "79724")]
    public async Task UpdateDocspaceAdmin_ByDocspaceAdmin_ShouldThrowAccessDenied()
    {
        await _peopleClient.Authenticate(Initializer.Owner);

        var owner = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);
        owner.Should().NotBeNull();
        owner.Response.Should().NotBeNull();

        var docspaceAdmin1 = await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
        var docspaceAdmin2 = await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);

        await _peopleClient.Authenticate(docspaceAdmin1);

        // Try to update Owner - should throw access denied
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                owner.Response.Id.ToString(),
                new UpdateMemberRequestDto(),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        // Try to update another Docspace admin - should throw access denied
        exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _profilesApi.UpdateMemberAsync(
                docspaceAdmin2.Id.ToString(),
                new UpdateMemberRequestDto(),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);

        // Update self - can't disable, cant change type, but can update other fields
        var user = await _profilesApi.UpdateMemberAsync(
                docspaceAdmin1.Id.ToString(),
                new UpdateMemberRequestDto()
                {
                    FirstName = "UpdatedFirstName",
                    LastName = "UpdatedLastName",
                    Disable = true,
                    IsUser = true
                },
                TestContext.Current.CancellationToken);

        user.Should().NotBeNull();
        user.Response.Should().NotBeNull();
        user.Response.Id.Should().Be(docspaceAdmin1.Id);
        user.Response.FirstName.Should().Be("UpdatedFirstName");
        user.Response.LastName.Should().Be("UpdatedLastName");
        user.Response.Status.Should().Be(EmployeeStatus.Active);
        user.Response.IsAdmin.Should().Be(false);
        user.Response.IsVisitor.Should().Be(false);

        var groups = await _groupApi.GetGroupByUserIdAsync(
                docspaceAdmin1.Id,
                TestContext.Current.CancellationToken);

        groups.Should().NotBeNull();
        groups.Response.Should().NotBeNull();
        groups.Response.Select(x=>x.Id).Should().NotContain(Core.Users.Constants.GroupGuest.ID);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80474")]
    public async Task PromoteUserToDocspaceAdmin_ByDocspaceAdmin_ShouldThrowAccessDenied()
    {
        await _peopleClient.Authenticate(Initializer.Owner);

        var docspaceAdmin = await Initializer.InviteContact(EmployeeType.DocSpaceAdmin);
        var user = await Initializer.InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(docspaceAdmin);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.DocSpaceAdmin,
                new UpdateMembersRequestDto([user.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80478")]
    public async Task PromoteGuestToUser_ByRoomAdmin_ShouldThrowAccessDenied()
    {
        await _peopleClient.Authenticate(Initializer.Owner);

        var roomAdmin1 = await Initializer.InviteContact(EmployeeType.RoomAdmin);
        var roomAdmin2 = await Initializer.InviteContact(EmployeeType.RoomAdmin);

        await _peopleClient.Authenticate(roomAdmin1);
        var guest = await Initializer.InviteContact(EmployeeType.Guest);

        await _peopleClient.Authenticate(roomAdmin2);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _userTypeApi.UpdateUserTypeAsync(
                EmployeeType.User,
                new UpdateMembersRequestDto([guest.Id]),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
