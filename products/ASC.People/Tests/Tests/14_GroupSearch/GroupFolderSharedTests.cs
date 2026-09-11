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

namespace ASC.People.Tests.Tests._14_GroupSearch;

/// <summary>
/// <c>GET /api/2.0/group/folder/{id}</c> - functional behaviour, pagination/filtering and
/// validation/edge cases.
///
/// Not ported: "Throws RequiredError for null/undefined folder id" - see the remark on
/// <see cref="GroupFileSharedTests"/>; the .NET SDK's <c>id</c> parameter is a non-nullable
/// <c>int</c>, so there is no argument that reproduces the TypeScript client-side check.
/// </summary>
public class GroupFolderSharedTests(AspireAppFixture fixture) : GroupSearchTestBase(fixture)
{
    [Fact]
    public async Task GetGroupsWithFoldersShared_ReturnsGroupSharedWithFolder()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().Contain(g => g.Id == group.Id && g.Shared == true);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_ReturnsMultipleGroupsSharedWithFolder()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group1 = await CreateGroupAsync();
        var group2 = await CreateGroupAsync();

        await _filesClient.Authenticate(Owner);
        await _sharingApi.SetFolderSecurityInfoAsync(
            folderId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(group1.Id, FileShare.Read), new FileShareParams(group2.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedIds = result.Where(g => g.Shared == true).Select(g => g.Id).ToList();

        sharedIds.Should().Contain(group1.Id);
        sharedIds.Should().Contain(group2.Id);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_GroupStaysShared_AfterAccessChangeFromReadToEditing()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Editing);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_GroupWithoutAccess_IsNotMarkedShared()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var sharedGroup = await CreateGroupAsync();
        var unsharedGroup = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, sharedGroup.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == sharedGroup.Id).Shared.Should().Be(true);
        result.First(g => g.Id == unsharedGroup.Id).Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_NoGroupsMarkedShared_WhenFolderNotSharedWithAnyGroup()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        await CreateGroupAsync();

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Where(g => g.Shared == true).Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_GroupStopsBeingShared_AfterSharingIsRemoved()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.None);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.FirstOrDefault(g => g.Id == group.Id)?.Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_RepeatedCalls_ReturnTheSameSharedGroup()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        var first = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        first.First(g => g.Id == group.Id).Shared.Should().Be(true);
        second.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_ExcludeSharedTrue_ExcludesAlreadySharedGroups()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var sharedGroup = await CreateGroupAsync();
        var unsharedGroup = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, sharedGroup.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, excludeShared: true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var ids = result.Select(g => g.Id).ToList();

        ids.Should().NotContain(sharedGroup.Id);
        ids.Should().Contain(unsharedGroup.Id);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_FilterValue_ReturnsOnlyMatchingGroups()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var uniqueToken = Guid.NewGuid().ToString()[..8];
        var matchingGroup = await CreateGroupAsync(name: $"match-{uniqueToken}");
        var nonMatchingGroup = await CreateGroupAsync(name: $"other-{Guid.NewGuid():N}");

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, filterValue: uniqueToken, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var ids = result.Select(g => g.Id).ToList();

        ids.Should().Contain(matchingGroup.Id);
        ids.Should().NotContain(nonMatchingGroup.Id);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_FilterValue_WithNoMatch_ReturnsEmptyList()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(
            folderId,
            filterValue: $"nomatch-{Guid.NewGuid():N}",
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_Count_LimitsNumberOfGroupsReturned()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateGroupAsync();
        }

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_StartIndex_OffsetsResultList()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateGroupAsync();
        }

        var full = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var offset = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        offset.Should().HaveCount(full.Count - 1);
        offset[0].Id.Should().Be(full[1].Id);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_CountAndStartIndex_WorkTogether()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateGroupAsync();
        }

        var full = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var page = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, count: 1, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        page.Should().HaveCount(1);
        page[0].Id.Should().Be(full[1].Id);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_Returns404_ForNonExistingFolderId()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFoldersSharedAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    // Raw HTTP: the SDK's `id` parameter is a non-nullable int - see GroupFileSharedTests.
    [Fact]
    public async Task GetGroupsWithFoldersShared_Returns404_ForInvalidFolderIdFormat()
    {
        using var response = await _peopleClient.GetAsync("api/2.0/group/folder/not-a-number", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_Returns404_ForEmptyFolderId()
    {
        using var response = await _peopleClient.GetAsync("api/2.0/group/folder/", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_Returns404_ForDeletedFolderId()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        await _foldersApi.DeleteFolderAsync(folderId, new DeleteFolder(deleteAfter: true, immediately: true), TestContext.Current.CancellationToken);

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken);
            }
            catch (ApiException)
            {
                break;
            }

            await Task.Delay(1000, TestContext.Current.CancellationToken);
        }

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_GroupWithManager_IsReturnedWithManagerInfo()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Manager.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_GroupWithDisabledMember_IsStillReturnedAsShared()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var member = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [member.Id]);
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);
        await TerminateUser(member);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_GroupAppearsOnlyOnce_AfterRepeatedSharingUpdates()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Editing);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Count(g => g.Id == group.Id).Should().Be(1);
    }

    [Fact]
    public async Task GetGroupsWithFoldersShared_ResponseItems_ContainExpectedGroupFields()
    {
        var folderId = await CreateFolderInMyDocumentsAsync();
        var groupName = "Autotest Group " + Guid.NewGuid().ToString()[..8];
        var group = await CreateGroupAsync(name: groupName);
        await ShareFolderWithGroupAsync(folderId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFoldersSharedAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // TS asserts `category` is defined; the .NET DTO types Category as a non-nullable Guid,
        // so "defined" has no separate meaning here - Id and Name are the fields worth asserting.
        var found = result.First(g => g.Id == group.Id);
        found.Id.Should().Be(group.Id);
        found.Name.Should().Be(groupName);
    }
}
