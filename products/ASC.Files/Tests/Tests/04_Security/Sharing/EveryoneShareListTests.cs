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
/// The "Everyone" system group in the share lists of a file and a folder
/// (<c>GET /api/2.0/files/file/{id}/share</c>, <c>GET /api/2.0/files/folder/{id}/share</c>).
/// Everyone has no row in <c>core_group</c>, yet the user-or-group share query joins the share
/// records to the groups table; these cases pin that Everyone still comes back as a group entry,
/// with its access level, in the total and on the right page.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class EveryoneShareListTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    // Constants.GroupEveryone.ID of ASC.Core.Common
    private static readonly Guid _everyoneId = new("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e");

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithEveryone_ListsEveryoneAsSystemGroup()
    {
        var file = await CreateFileInMy("Autotest Everyone Listed.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.Read);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var everyone = FindEveryone(entries);
        everyone.Should().NotBeNull();
        everyone!.SubjectType.Should().Be(SubjectType.Group);
        everyone.SharedToGroup.IsSystem.Should().BeTrue();
        everyone.SharedToGroup.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithEveryone_ReturnsGrantedAccess()
    {
        var file = await CreateFileInMy("Autotest Everyone Access.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.ReadWrite);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        FindEveryone(entries)!.Access.Should().Be(FileShare.ReadWrite);
    }

    [Fact]
    public async Task GetFileSecurityInfo_EveryoneGroupAndUser_AllListed()
    {
        var user = await InviteContact(EmployeeType.User);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto([user.Id], Owner.Id, $"Autotest Everyone Mix {Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest Everyone Mixed.docx", Owner);
        await _sharingApi.SetFileSecurityInfoAsync(
            file.Id,
            new SecurityInfoSimpleRequestDto
            {
                Share =
                [
                    new() { ShareTo = _everyoneId, Access = FileShare.Read },
                    new() { ShareTo = group.Id, Access = FileShare.Read },
                    new() { ShareTo = user.Id, Access = FileShare.ReadWrite }
                ],
                Notify = false
            },
            TestContext.Current.CancellationToken);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        FindEveryone(entries).Should().NotBeNull();
        entries.Should().Contain(e => e.SharedToGroup != null && e.SharedToGroup.Id == group.Id);
        entries.Should().Contain(e => e.SharedToUser != null && e.SharedToUser.Id == user.Id);
        entries.Should().Contain(e => e.IsOwner);
        entries.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithEveryone_TotalCountsEveryone()
    {
        var file = await CreateFileInMy("Autotest Everyone Total.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.Read);

        var total = await ReadShareTotal($"api/2.0/files/file/{file.Id}/share");

        total.Should().Be(2, "the owner and Everyone are the two entries of the list");
    }

    [Fact]
    public async Task GetFileSecurityInfo_SharedWithEveryone_SecondPageHoldsEveryone()
    {
        var file = await CreateFileInMy("Autotest Everyone Paging.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.Read);

        var firstPage = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, count: 1, startIndex: 0, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var secondPage = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, count: 1, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken)).Response;

        firstPage.Should().ContainSingle(e => e.IsOwner);
        secondPage.Should().ContainSingle();
        FindEveryone(secondPage).Should().NotBeNull();
    }

    [Fact]
    public async Task GetFileSecurityInfo_EveryoneShareRemoved_NoLongerListed()
    {
        var file = await CreateFileInMy("Autotest Everyone Removed.docx", Owner);
        await ShareFile(file.Id, _everyoneId, FileShare.Read);
        await ShareFile(file.Id, _everyoneId, FileShare.None);

        var entries = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        FindEveryone(entries).Should().BeNull();
        entries.Should().ContainSingle(e => e.IsOwner);
    }

    [Fact]
    public async Task GetFolderSecurityInfo_SharedWithEveryone_ListsEveryone()
    {
        var folder = await CreateFolderInMy("Autotest Everyone Folder", Owner);
        await ShareFolder(folder.Id, _everyoneId, FileShare.Read);

        var entries = (await _sharingApi.GetFolderSecurityInfoAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        var everyone = FindEveryone(entries);
        everyone.Should().NotBeNull();
        everyone!.Access.Should().Be(FileShare.Read);
        everyone.SharedToGroup.IsSystem.Should().BeTrue();
    }

    private static FileShareDto? FindEveryone(IEnumerable<FileShareDto> entries)
    {
        return entries.FirstOrDefault(e => e.SharedToGroup != null && e.SharedToGroup.Id == _everyoneId);
    }

    /// <summary>
    /// Reads the <c>total</c> of a share list from the raw JSON: <c>FileShareArrayWrapper</c> in the
    /// SDK has no <c>Total</c> property, so the typed call cannot see it.
    /// </summary>
    private async Task<int> ReadShareTotal(string path)
    {
        using var response = await _filesClient.GetAsync(path, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.IsSuccessStatusCode.Should().BeTrue(body);

        using var json = JsonDocument.Parse(body);

        return json.RootElement.GetProperty("total").GetInt32();
    }
}
