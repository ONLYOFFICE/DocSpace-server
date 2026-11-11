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

namespace ASC.Files.Tests.Tests._01_Files.Create;

/// <summary>
/// Creating files and folders in a Virtual Data Room by role: a Room manager and a Content creator create a document,
/// a text file and a folder; an Editor, a Form Filler and a Viewer get 403 for each and nothing appears in the room.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrFileCreateRolesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<FileShare, bool> CreateByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, true },
        { FileShare.Editing, false },
        { FileShare.FillForms, false },
        { FileShare.Read, false }
    };

    [Theory]
    [MemberData(nameof(CreateByRole))]
    public async Task CreateFileTextFileAndFolder_VdrRole_OnlyContentCreatorAndAboveCreate(FileShare access, bool allowed)
    {
        // Arrange
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom($"Autotest VDR Create {access}");
        await InviteToRoom(room.Id, member, access);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            var document = (await _filesApi.CreateFileAsync(room.Id, new CreateFileRequest("Autotest VDR Created.docx"), TestContext.Current.CancellationToken)).Response;
            var text = (await _filesApi.CreateTextFileAsync(room.Id, new CreateTextOrHtmlFileRequest("Autotest VDR Created", "some text", true), TestContext.Current.CancellationToken)).Response;
            var folder = (await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Autotest VDR Created Folder"), TestContext.Current.CancellationToken)).Response;

            // Assert
            document.FolderId.Should().Be(room.Id);
            text.FolderId.Should().Be(room.Id);
            folder.ParentId.Should().Be(room.Id);
        }
        else
        {
            var document = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesApi.CreateFileAsync(room.Id, new CreateFileRequest("Autotest VDR Created.docx"), TestContext.Current.CancellationToken));
            var text = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesApi.CreateTextFileAsync(room.Id, new CreateTextOrHtmlFileRequest("Autotest VDR Created", "some text", true), TestContext.Current.CancellationToken));
            var folder = await Assert.ThrowsAsync<ApiException>(
                async () => await _foldersApi.CreateFolderAsync(room.Id, new CreateFolder("Autotest VDR Created Folder"), TestContext.Current.CancellationToken));

            // Assert
            document.ErrorCode.Should().Be(403);
            text.ErrorCode.Should().Be(403);
            folder.ErrorCode.Should().Be(403);

            await _filesClient.Authenticate(Owner);
            var content = (await _foldersApi.GetFolderByFolderIdAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
            content.Total.Should().Be(0);
        }
    }
}
