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

namespace ASC.Files.Tests.Tests._06_Operations.Copy;

/// <summary>
/// Copying into a Virtual Data Room by role: a Room manager and a Content creator copy a file of their My documents
/// into the room and copy the owner's file into a folder of the room; an Editor, a Form Filler and a Viewer get 403
/// for both and no copy appears at the destination.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrCopyRolesTests(
    AspireAppFixture fixture)
    : CopyTestBase(fixture)
{
    public static TheoryData<FileShare, bool> CopyInByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, true },
        { FileShare.Editing, false },
        { FileShare.FillForms, false },
        { FileShare.Read, false }
    };

    [Theory]
    [MemberData(nameof(CopyInByRole))]
    public async Task CopyFromMyDocuments_VdrRole_OnlyContentCreatorAndAboveCopyIn(FileShare access, bool allowed)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access, $"Autotest VDR Copy In {access}");

        await _filesClient.Authenticate(member);
        var source = await CreateFileInMy($"Autotest VDR Copy In {access}.docx", member);
        var request = BuildCopyRequest(room.Id, fileIds: [source.Id]);

        // Act
        if (allowed)
        {
            await CopyAndWait(request);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesOperationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(403);
        }

        // Assert
        await _filesClient.Authenticate(Owner);
        FileTitles(await GetFolderContent(room.Id)).Contains(source.Title).Should().Be(allowed);
    }

    [Theory]
    [MemberData(nameof(CopyInByRole))]
    public async Task CopyWithinRoom_VdrRole_OnlyContentCreatorAndAboveCopyOwnersFile(FileShare access, bool allowed)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access, $"Autotest VDR Copy Within {access}");
        var source = await CreateFile($"Autotest VDR Copy Within {access}.docx", room.Id);
        var folder = await CreateFolder("Autotest VDR Copy Target", room.Id);
        var request = BuildCopyRequest(folder.Id, fileIds: [source.Id]);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            await CopyAndWait(request);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesOperationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken));

            exception.ErrorCode.Should().Be(403);
        }

        // Assert
        await _filesClient.Authenticate(Owner);
        FileTitles(await GetFolderContent(folder.Id)).Contains(source.Title).Should().Be(allowed);
    }

    private async Task<(FolderDto Room, User Member)> CreateVdrWithMember(FileShare access, string title)
    {
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom(title);
        await InviteToRoom(room.Id, member, access);

        return (room, member);
    }
}
