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

namespace ASC.Files.Tests.Tests._04_Security;

/// <summary>
/// The VDR room setting "deny download" restricts only members without editing rights: the owner, a
/// Room manager, a Content creator and an Editor keep the download right on a file of the room, a
/// Viewer loses it.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "DenyDownload")]
public class VdrDenyDownloadRolesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<EmployeeType, FileShare, bool> MemberDownload => new()
    {
        { EmployeeType.RoomAdmin, FileShare.RoomManager, true },
        { EmployeeType.RoomAdmin, FileShare.ContentCreator, true },
        { EmployeeType.User, FileShare.Editing, true },
        { EmployeeType.User, FileShare.Read, false }
    };

    [Fact]
    public async Task VdrDenyDownloadOn_Owner_KeepsDownloadRight()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (_, file) = await CreateVdrWithDenyDownload();

        // Act
        var info = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Security.Download.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(MemberDownload))]
    public async Task VdrDenyDownloadOn_Member_DownloadRightMatchesEditingRight(EmployeeType employeeType, FileShare access, bool canDownload)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateVdrWithDenyDownload();
        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, access);

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Security.Download.Should().Be(canDownload);
    }

    /// <summary>
    /// The download restriction also closes copying out: a Viewer cannot copy the room's file into My documents (403,
    /// nothing copied), an Editor still can.
    /// </summary>
    [Theory]
    [InlineData(FileShare.Read, false)]
    [InlineData(FileShare.Editing, true)]
    public async Task VdrDenyDownloadOn_MemberCopiesToMyDocuments_OnlyWithEditingRight(FileShare access, bool copied)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateVdrWithDenyDownload();
        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);
        var myDocsId = await GetUserFolderIdAsync(member);
        var request = new BatchRequestDto
        {
            DestFolderId = new BatchRequestDtoAllOfDestFolderId(myDocsId),
            ConflictResolveType = FileConflictResolveType.Skip,
            FileIds = [new BatchRequestDtoAllOfFileIds(file.Id)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        // Act + Assert
        if (copied)
        {
            var results = (await _filesOperationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken)).Response;
            await WaitLongOperation(results.FirstOrDefault()?.Id);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesOperationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken));
            exception.ErrorCode.Should().Be(403);
        }

        var content = (await _foldersApi.GetFolderByFolderIdAsync(myDocsId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        (content.Files ?? []).Any(f => f.Title == file.Title).Should().Be(copied);
    }

    private async Task<(FolderDto Room, FileDto File)> CreateVdrWithDenyDownload()
    {
        var room = await CreateVDRRoom("Autotest VDR Deny Download Roles");
        var file = await CreateFile("Autotest VDR Deny Download.docx", room.Id);

        var updated = (await _roomsApi.UpdateRoomAsync(room.Id, new UpdateRoomRequest(denyDownload: true), TestContext.Current.CancellationToken)).Response;
        updated.DenyDownload.Should().BeTrue();

        return (room, file);
    }
}
