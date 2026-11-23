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
/// Which access levels <c>PUT /api/2.0/files/file/{fileId}/share</c> accepts for an account or a group on an
/// entry in My documents, and what the Shared with me section reports about an entry read through a share.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class MyDocumentsAccessLevelsTests(
    AspireAppFixture fixture)
    : SharingTestBase(fixture)
{
    [Theory]
    [InlineData(FileShare.ReadWrite)]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.Review)]
    [InlineData(FileShare.Comment)]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Restrict)]
    public async Task SetFileSecurityInfo_UserShareOnDocxInMyDocuments_LevelIsStored(FileShare access)
    {
        var file = await CreateFileInMy($"Autotest Level {access}.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(file.Id, user.Id, access);

        var rights = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        rights.Should().ContainSingle(r => r.SharedToUser != null && r.SharedToUser.Id == user.Id)
            .Which.Access.Should().Be(access);
    }

    [Theory]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.Read)]
    public async Task SetFileSecurityInfo_GroupShareOnDocxInMyDocuments_LevelIsStored(FileShare access)
    {
        var file = await CreateFileInMy($"Autotest Group Level {access}.docx", Owner);
        await _peopleClient.Authenticate(Owner);
        var group = (await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: $"Autotest Level Group {access}", groupManager: Owner.Id),
            TestContext.Current.CancellationToken)).Response;

        await ShareFile(file.Id, group.Id, access);

        var rights = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        rights.Should().ContainSingle(r => r.SubjectType == SubjectType.Group)
            .Which.Access.Should().Be(access);
    }

    [Theory]
    [InlineData(FileShare.RoomManager)]
    [InlineData(FileShare.ContentCreator)]
    [InlineData(FileShare.Varies)]
    [InlineData(FileShare.FillForms)]
    public async Task SetFileSecurityInfo_RoomOrFormLevelOnDocxInMyDocuments_Returns403AndAddsNothing(FileShare access)
    {
        var file = await CreateFileInMy($"Autotest Refused {access}.docx", Owner);
        var user = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ShareFile(file.Id, user.Id, access));

        exception.ErrorCode.Should().Be(403);

        var rights = (await _sharingApi.GetFileSecurityInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        rights.Should().NotContain(r => r.SharedToUser != null && r.SharedToUser.Id == user.Id);
    }

    [Fact]
    public async Task SetFileSecurityInfo_FillFormsOnPdfFormInMyDocuments_LevelIsStored()
    {
        var form = await CreateFileInMy("Autotest Level Form.pdf", Owner);
        var user = await InviteContact(EmployeeType.User);

        await ShareFile(form.Id, user.Id, FileShare.FillForms);

        var rights = (await _sharingApi.GetFileSecurityInfoAsync(form.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        rights.Should().ContainSingle(r => r.SharedToUser != null && r.SharedToUser.Id == user.Id)
            .Which.Access.Should().Be(FileShare.FillForms);
    }

    [Theory]
    [InlineData(FileShare.RoomManager)]
    [InlineData(FileShare.ContentCreator)]
    [InlineData(FileShare.Varies)]
    public async Task SetFolderSecurityInfo_RoomLevelOnFolderInMyDocuments_Returns403(FileShare access)
    {
        var folder = await CreateFolderInMy($"Autotest Refused Folder {access}", Owner);
        var user = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await ShareFolder(folder.Id, user.Id, access));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SharedWithMe_EntriesFromTwoSharers_CarrySharedByOwnedByAndFilterBySharer()
    {
        var recipient = await InviteContact(EmployeeType.User);
        var otherSharer = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var ownerFile = await CreateFileInMy("Autotest SharedBy Owner.docx", Owner);
        await ShareFile(ownerFile.Id, recipient.Id, FileShare.Read);

        await _filesClient.Authenticate(otherSharer);
        var otherFile = await CreateFileInMy("Autotest SharedBy Other.docx", otherSharer);
        await ShareFile(otherFile.Id, recipient.Id, FileShare.Read);

        var sharedId = await GetShareFolderIdAsync(recipient);

        var all = (await _foldersApi.GetFolderByFolderIdAsync(sharedId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        var ownerEntry = all.Files.Should().ContainSingle(f => f.Title == ownerFile.Title).Which;
        ownerEntry.SharedBy.Id.Should().Be(Owner.Id);
        ownerEntry.OwnedBy.Id.Should().Be(Owner.Id);
        var otherEntry = all.Files.Should().ContainSingle(f => f.Title == otherFile.Title).Which;
        otherEntry.SharedBy.Id.Should().Be(otherSharer.Id);
        otherEntry.OwnedBy.Id.Should().Be(otherSharer.Id);

        var byOwner = (await _foldersApi.GetFolderByFolderIdAsync(sharedId, sharedBy: Owner.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        byOwner.Files.Select(f => f.Title).Should().BeEquivalentTo([ownerFile.Title]);
    }

    [Fact]
    public async Task GetFileInfo_SharedFileInUnsharedSubfolder_ReportsSharedWithMeAsFolder()
    {
        var recipient = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var folder = await CreateFolderInMy("Autotest Hidden Parent", Owner);
        var file = await CreateFile("Autotest In Hidden Parent.docx", folder.Id);
        await ShareFile(file.Id, recipient.Id, FileShare.Read);

        var sharedId = await GetShareFolderIdAsync(recipient);
        var info = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.FolderId.Should().Be(sharedId);
        await Assert.ThrowsAsync<ApiException>(async () =>
            await _foldersApi.GetFolderByFolderIdAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));
    }
}
