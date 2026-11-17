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

namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// "Links of an item of a private room belong to those who manage the room's links": a request made of external links
/// only is checked against the room, while sharing such an item with a member stays refused, also when a link travels
/// in the same request. Revocations go through the SDK, which carries everything a revocation needs.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateItemSharingGuardTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private static SecurityInfoSimpleRequestDto ShareFor(params (Guid Id, FileShare Access)[] shares) => new()
    {
        Share = [.. shares.Select(s => new FileShareParams { ShareTo = s.Id, Access = s.Access })],
        Notify = false
    };

    [Fact]
    public async Task ShareFile_WithAMember_IsRefused()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, member, FileShare.Read);
        var fileId = await UploadEncryptedFile(room.Id, "member.docx");

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SetFileSecurityInfoAsync(fileId, ShareFor((member.Id, FileShare.Read)), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task ShareFolder_WithAMember_IsRefused()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, member, FileShare.Read);
        var folderId = await CreateFolderIn(room.Id);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SetFolderSecurityInfoAsync(folderId, ShareFor((member.Id, FileShare.Read)), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// A member share and a revocation of an existing link in one request: the member makes the request a share of the
    /// file, which is refused as a whole, so the link stays.
    /// </summary>
    [Fact]
    public async Task ShareFile_WithAMemberAndALinkTogether_IsRefusedAndKeepsTheLink()
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, member, FileShare.Read);
        var fileId = await UploadEncryptedFile(room.Id, "mixed.docx");
        var linkId = await StoreFileLink(fileId);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _sharingApi.SetFileSecurityInfoAsync(fileId, ShareFor((member.Id, FileShare.Read), (linkId, FileShare.None)), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        (await FileLinkCount(fileId)).Should().Be(1);
    }

    [Fact]
    public async Task RevokeFileLink_ByTheRoomOwner_RemovesIt()
    {
        var room = await CreateRoomAsOwner();
        var fileId = await UploadEncryptedFile(room.Id, "revoked.docx");
        var linkId = await StoreFileLink(fileId);

        (await FileLinkCount(fileId)).Should().Be(1);

        await RevokeFileLink(fileId, linkId);

        (await FileLinkCount(fileId)).Should().Be(0);
    }

    [Fact]
    public async Task RevokeFolderLink_ByTheRoomOwner_RemovesIt()
    {
        var room = await CreateRoomAsOwner();
        var folderId = await CreateFolderIn(room.Id);
        var linkId = await StoreFolderLink(folderId);

        (await FolderLinkCount(folderId)).Should().Be(1);

        await RevokeFolderLink(folderId, linkId);

        (await FolderLinkCount(folderId)).Should().Be(0);
    }

    [Fact]
    public async Task RevokeFileLink_ByAContentCreator_IsRefused()
    {
        var creator = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);
        var fileId = await UploadEncryptedFile(room.Id, "kept.docx");
        var linkId = await StoreFileLink(fileId);

        await _filesClient.Authenticate(creator);
        var exception = await Assert.ThrowsAsync<ApiException>(() => RevokeFileLink(fileId, linkId));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await FileLinkCount(fileId)).Should().Be(1);
    }

    [Fact]
    public async Task RevokeFolderLink_ByAContentCreator_IsRefused()
    {
        var creator = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, creator, FileShare.ContentCreator);
        var folderId = await CreateFolderIn(room.Id);
        var linkId = await StoreFolderLink(folderId);

        await _filesClient.Authenticate(creator);
        var exception = await Assert.ThrowsAsync<ApiException>(() => RevokeFolderLink(folderId, linkId));

        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await FolderLinkCount(folderId)).Should().Be(1);
    }

    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.ContentCreator)]
    public async Task FileLinks_ForAMemberWhoDoesNotManageTheRoom_AreHidden(FileShare memberAccess)
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, member, memberAccess);
        var fileId = await UploadEncryptedFile(room.Id, "listed.docx");
        await StoreFileLink(fileId);

        (await FileLinkCount(fileId)).Should().Be(1);
        (await FileLinkTotal(fileId)).Should().Be(1);

        await _filesClient.Authenticate(member);
        (await FileLinkCount(fileId)).Should().Be(0);
        (await FileLinkTotal(fileId)).Should().Be(0);
    }

    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.ContentCreator)]
    public async Task FolderLinks_ForAMemberWhoDoesNotManageTheRoom_AreHidden(FileShare memberAccess)
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, member, memberAccess);
        var folderId = await CreateFolderIn(room.Id);
        await StoreFolderLink(folderId);

        (await FolderLinkCount(folderId)).Should().Be(1);

        await _filesClient.Authenticate(member);
        (await FolderLinkCount(folderId)).Should().Be(0);
    }
}
