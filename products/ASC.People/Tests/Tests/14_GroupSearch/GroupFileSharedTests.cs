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
/// <c>GET /api/2.0/group/file/{id}</c> - functional behaviour and validation/edge cases.
///
/// Not ported: "Throws RequiredError for null/undefined file id" - the TypeScript SDK validates
/// its optional-looking `id: number` client-side before sending anything. The .NET SDK's
/// <c>GetGroupsWithFilesSharedAsync(int id, ...)</c> takes a non-nullable <c>int</c>, so there is
/// no value that reproduces "the client refused to send the request" - the code simply does not
/// compile with a null argument.
/// </summary>
public class GroupFileSharedTests(AspireAppFixture fixture) : GroupSearchTestBase(fixture)
{
    [Fact]
    public async Task GetGroupsWithFilesShared_ReturnsGroupSharedWithFile()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Should().Contain(g => g.Id == group.Id && g.Shared == true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_ReturnsMultipleGroupsSharedWithFile()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group1 = await CreateGroupAsync();
        var group2 = await CreateGroupAsync();

        await _filesClient.Authenticate(Owner);
        await _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto(
                [new FileShareParams(group1.Id, FileShare.Read), new FileShareParams(group2.Id, FileShare.Read)],
                notify: false),
            TestContext.Current.CancellationToken);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var sharedIds = result.Where(g => g.Shared == true).Select(g => g.Id).ToList();

        sharedIds.Should().Contain(group1.Id);
        sharedIds.Should().Contain(group2.Id);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GroupMarkedShared_AfterReadAccess()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GroupStaysShared_AfterAccessChangeFromReadToEditing()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Editing);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GroupWithoutAccess_IsNotMarkedShared()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var sharedGroup = await CreateGroupAsync();
        var unsharedGroup = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, sharedGroup.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == sharedGroup.Id).Shared.Should().Be(true);
        result.First(g => g.Id == unsharedGroup.Id).Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_NoGroupsMarkedShared_WhenFileNotSharedWithAnyGroup()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        await CreateGroupAsync();

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Where(g => g.Shared == true).Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GroupStopsBeingShared_AfterSharingIsRemoved()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.None);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.FirstOrDefault(g => g.Id == group.Id)?.Shared.Should().NotBe(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_RepeatedCalls_ReturnTheSameSharedGroup()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        var first = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var second = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        first.First(g => g.Id == group.Id).Shared.Should().Be(true);
        second.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_ExcludeSharedTrue_ExcludesAlreadySharedGroups()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var sharedGroup = await CreateGroupAsync();
        var unsharedGroup = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, sharedGroup.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, excludeShared: true, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var ids = result.Select(g => g.Id).ToList();

        ids.Should().NotContain(sharedGroup.Id);
        ids.Should().Contain(unsharedGroup.Id);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_Returns404_ForNonExistingFileId()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _groupSearchApi.GetGroupsWithFilesSharedAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    // Raw HTTP: the SDK's `id` parameter is a non-nullable int, so neither "not-a-number" nor an
    // empty path segment can be produced through the typed call.
    [Fact]
    public async Task GetGroupsWithFilesShared_Returns404_ForInvalidFileIdFormat()
    {
        using var response = await _peopleClient.GetAsync("api/2.0/group/file/not-a-number", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_Returns404_ForEmptyFileId()
    {
        using var response = await _peopleClient.GetAsync("api/2.0/group/file/", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GroupWithManager_IsReturnedWithManagerInfo()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Manager.Id.Should().Be(Owner.Id);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GroupWithDisabledMember_IsStillReturnedAsShared()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var member = await InviteContact(EmployeeType.User);
        var group = await CreateGroupAsync(members: [member.Id]);
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);
        await TerminateUser(member);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.First(g => g.Id == group.Id).Shared.Should().Be(true);
    }

    [Fact]
    public async Task GetGroupsWithFilesShared_GroupAppearsOnlyOnce_AfterRepeatedSharingUpdates()
    {
        var fileId = await CreateFileInMyDocumentsAsync();
        var group = await CreateGroupAsync();
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Read);
        await ShareFileWithGroupAsync(fileId, group.Id, FileShare.Editing);

        var result = (await _groupSearchApi.GetGroupsWithFilesSharedAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        result.Count(g => g.Id == group.Id).Should().Be(1);
    }
}
