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

namespace ASC.Files.Tests.Tests._04_Security.Sharing;

/// <summary>
/// <c>GET /api/2.0/files/file/{id}/share</c> (<c>GetFileSecurityInfo</c>) - paging (<c>count</c>,
/// <c>startIndex</c>), 404 cases and lifecycle interaction (trash, permanent delete). Entry shape
/// and access levels live in <see cref="FileSecurityInfoReadTests"/>.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class FileSecurityInfoLifecycleTests(
    AspireAppFixture fixture)
    : FileSecurityInfoTestBase(fixture)
{
    [Fact]
    public async Task GetFileSecurityInfo_CountParameter_LimitsReturnedEntries()
    {
        var file = await CreateFileInMy("Autotest Security Info Count Param.docx", Owner);
        var user1 = await InviteContact(EmployeeType.User);
        var user2 = await InviteContact(EmployeeType.User);

        await _sharingApi.SetFileSecurityInfoAsync(file.Id, new SecurityInfoSimpleRequestDto
        {
            Share = [new() { ShareTo = user1.Id, Access = FileShare.Read }, new() { ShareTo = user2.Id, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, count: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        entries.Count.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetFileSecurityInfo_StartIndex_SkipsEntries()
    {
        var file = await CreateFileInMy("Autotest Security Info StartIndex.docx", Owner);
        var user1 = await InviteContact(EmployeeType.User);
        var user2 = await InviteContact(EmployeeType.User);

        await _sharingApi.SetFileSecurityInfoAsync(file.Id, new SecurityInfoSimpleRequestDto
        {
            Share = [new() { ShareTo = user1.Id, Access = FileShare.Read }, new() { ShareTo = user2.Id, Access = FileShare.Read }],
            Notify = false
        }, TestContext.Current.CancellationToken);

        var fullList = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var skippedList = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        skippedList.Should().HaveCount(fullList.Count - 1);
    }

    [Fact]
    public async Task GetFileSecurityInfo_NonExistentFileId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(999999999, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFileSecurityInfo_IdZero_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(0, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFileSecurityInfo_NegativeId_Returns404()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(-1, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFileSecurityInfo_FileMovedToTrash_UserShareEntryIsRemoved()
    {
        var file = await CreateFileInMy("Autotest Security Info Trash File.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await DeleteFileAndWait(file.Id, immediately: false);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        FindUserEntry(entries, user.Id).Should().BeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_PermanentlyDeletedFile_Returns404()
    {
        var file = await CreateFileInMy("Autotest Security Info Perm Delete File.docx", Owner);

        await DeleteFileAndWait(file.Id, immediately: true);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedUser_CannotAccessAfterFileMovedToTrash()
    {
        var file = await CreateFileInMy("Autotest Security Info Trash Shared.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, FileShare.Read);
        await DeleteFileAndWait(file.Id, immediately: false);

        await _filesClient.Authenticate(user);
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }
}
