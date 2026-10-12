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

namespace ASC.People.Tests.Tests._13_Groups;

/// <summary>
/// POST /api/2.0/group - creation validation, edge cases and permissions.
/// </summary>
public class GroupPermissionCreateTests(
    AspireAppFixture fixture)
    : GroupPermissionTestBase(fixture)
{
    #region Validation and edge cases

    // Malformed bodies below (wrong JSON types, an empty GUID field, a missing required field) go
    // through raw HTTP: GroupRequestDto's GroupManager/Members are typed Guid/List<Guid> and cannot
    // carry a non-Guid string, a bare "not-an-array" value, or an omitted-but-present empty field.

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81417")]
    public async Task CreateGroup_WithoutGroupName_BadRequest()
    {
        using var response = await SendRawGroupRequest(HttpMethod.Post, "api/2.0/group",
            $$"""{"groupManager":"{{Owner.Id}}"}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81485")]
    public async Task CreateGroup_WithoutGroupManager_Created()
    {
        var group = await CreateGroupAsync(RandomGroupName());

        group.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81418")]
    public async Task CreateGroup_WithEmptyGroupName_BadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.AddGroupAsync(
                new GroupRequestDto(groupManager: Owner.Id, groupName: ""),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateGroup_WithEmptyGroupManager_BadRequest()
    {
        using var response = await SendRawGroupRequest(HttpMethod.Post, "api/2.0/group",
            $$"""{"groupName":"{{RandomGroupName()}}","groupManager":""}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81419")]
    public async Task CreateGroup_WithNonExistentGroupManager_BadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.AddGroupAsync(
                new GroupRequestDto(groupManager: Guid.NewGuid(), groupName: RandomGroupName()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81420")]
    public async Task CreateGroup_WithNonExistentMember_BadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.AddGroupAsync(
                new GroupRequestDto(members: [Guid.NewGuid()], groupManager: Owner.Id, groupName: RandomGroupName()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateGroup_MembersNotAnArray_BadRequest()
    {
        using var response = await SendRawGroupRequest(HttpMethod.Post, "api/2.0/group",
            $$"""{"groupName":"{{RandomGroupName()}}","groupManager":"{{Owner.Id}}","members":"not-an-array"}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGroup_GroupNameNotAString_BadRequest()
    {
        using var response = await SendRawGroupRequest(HttpMethod.Post, "api/2.0/group",
            $$"""{"groupName":123,"groupManager":"{{Owner.Id}}"}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGroup_GroupManagerNotAString_BadRequest()
    {
        using var response = await SendRawGroupRequest(HttpMethod.Post, "api/2.0/group",
            $$"""{"groupName":"{{RandomGroupName()}}","groupManager":123}""");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateGroup_VeryLongGroupName_BadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.AddGroupAsync(
                new GroupRequestDto(groupManager: Owner.Id, groupName: RandomGroupName(255)),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "81421")]
    public async Task CreateGroup_SpacesOnlyGroupName_BadRequest()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.AddGroupAsync(
                new GroupRequestDto(groupManager: Owner.Id, groupName: "   "),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateGroup_SpecialCharactersInGroupName_Created()
    {
        const string groupName = "Test Group !@#$%^&*()";

        var group = await CreateGroupAsync(groupName, Owner.Id);

        group.Name.Should().Be(groupName);
    }

    [Fact]
    public async Task CreateGroup_UnicodeGroupName_Created()
    {
        const string groupName = "Тестовая группа 테스트 グループ";

        var group = await CreateGroupAsync(groupName, Owner.Id);

        group.Name.Should().Be(groupName);
    }

    [Fact]
    public async Task CreateGroup_DuplicateMembers_Deduplicated()
    {
        var member = await InviteContact(EmployeeType.User);

        var created = await CreateGroupAsync(RandomGroupName(), Owner.Id, [member.Id, member.Id]);
        var group = await GetGroupWithMembersAsync(created.Id);

        group.Members.Count(m => m.Id == member.Id).Should().Be(1);
    }

    #endregion

    #region Permissions

    [Fact]
    public async Task CreateGroup_DocSpaceAdmin_Created()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _peopleClient.Authenticate(admin);

        var group = await CreateGroupAsync(RandomGroupName(), Owner.Id);

        group.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateGroup_Anonymous_Unauthorized()
    {
        await _peopleClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.AddGroupAsync(
                new GroupRequestDto(groupManager: Owner.Id, groupName: RandomGroupName()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.Guest)]
    public async Task CreateGroup_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _peopleClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _groupApi.AddGroupAsync(
                new GroupRequestDto(groupManager: Owner.Id, groupName: RandomGroupName()),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    #endregion
}
