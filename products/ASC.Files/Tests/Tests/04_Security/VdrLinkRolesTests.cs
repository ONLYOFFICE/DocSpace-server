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
/// Creating the primary link of a file and of a folder in a Virtual Data Room by role: reading the primary link of an
/// entry that has none creates it, which only a Room manager may do (the link is internal); a Content creator, an
/// Editor, a Form Filler and a Viewer get 403 for the owner's file and folder.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrLinkRolesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<FileShare, bool> CreateLinkByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, false },
        { FileShare.Editing, false },
        { FileShare.FillForms, false },
        { FileShare.Read, false }
    };

    [Theory]
    [MemberData(nameof(CreateLinkByRole))]
    public async Task GetPrimaryExternalLink_VdrRole_OnlyRoomManagerCreatesFileAndFolderLink(FileShare access, bool allowed)
    {
        // Arrange
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom($"Autotest VDR Links {access}");
        var file = await CreateFile("Autotest VDR Link.docx", room.Id);
        var folder = await CreateFolder("Autotest VDR Link Folder", room.Id);
        await InviteToRoom(room.Id, member, access);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            var fileLink = (await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var folderLink = (await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

            // Assert
            fileLink.SharedLink.Internal.Should().BeTrue();
            folderLink.SharedLink.Internal.Should().BeTrue();
        }
        else
        {
            var fileException = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesApi.GetFilePrimaryExternalLinkAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken));
            var folderException = await Assert.ThrowsAsync<ApiException>(
                async () => await _foldersApi.GetFolderPrimaryExternalLinkAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken));

            // Assert
            fileException.ErrorCode.Should().Be(403);
            folderException.ErrorCode.Should().Be(403);
        }
    }
}
