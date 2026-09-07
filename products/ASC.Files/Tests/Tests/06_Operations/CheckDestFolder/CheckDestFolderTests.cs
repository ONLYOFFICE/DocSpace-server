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

namespace ASC.Files.Tests.Tests._06_Operations.CheckDestFolder;

[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CheckDestFolderTests(
    AspireAppFixture fixture)
    : CheckDestFolderTestBase(fixture)
{
    [Fact]
    public async Task CheckDestFolder_MoveFileToCustomRoom_ReturnsAllAllowedAndSourceFile()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var sourceTitle = "Autotest CheckDestFolder File CustomRoom.docx";
        var file = await CreateFile(sourceTitle, myDocsFolderId);

        var room = await CreateCustomRoom("Autotest CheckDestFolder CustomRoom");

        var result = await CheckDestFolder(fileIds: [file.Id], destFolderId: room.Id);

        result.Result.Should().Be(CheckDestFolderResult.AllAllowed);
        result.Files.Should().ContainSingle();
        result.Files![0].Title.Should().Be(sourceTitle);
    }

    [Fact]
    public async Task CheckDestFolder_MultipleFiles_FilesFieldContainsAllSourceFiles()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        const string file1Title = "Autotest CheckDestFolder Multi Source1.docx";
        const string file2Title = "Autotest CheckDestFolder Multi Source2.docx";

        var file1 = await CreateFile(file1Title, myDocsFolderId);
        var file2 = await CreateFile(file2Title, myDocsFolderId);

        var room = await CreateCustomRoom("Autotest CheckDestFolder Multi Files Field Room");

        var result = await CheckDestFolder(fileIds: [file1.Id, file2.Id], destFolderId: room.Id);

        result.Result.Should().Be(CheckDestFolderResult.AllAllowed);
        result.Files.Should().HaveCount(2);
        result.Files!.Select(f => f.Title).Should().Contain([file1Title, file2Title]);
    }

    public static TheoryData<RoomType> RoomTypesThatAllowRegularFiles =>
    [
        RoomType.EditingRoom, RoomType.PublicRoom, RoomType.VirtualDataRoom
    ];

    [Theory]
    [MemberData(nameof(RoomTypesThatAllowRegularFiles))]
    public async Task CheckDestFolder_MoveFileToRoom_ReturnsAllAllowed(RoomType roomType)
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile($"Autotest CheckDestFolder File {roomType}.docx", myDocsFolderId);

        var room = await CreateRoomOf(roomType, $"Autotest CheckDestFolder {roomType}");

        var result = await CheckDestFolder(fileIds: [file.Id], destFolderId: room.Id);

        result.Result.Should().Be(CheckDestFolderResult.AllAllowed);
    }

    [Fact]
    public async Task CheckDestFolder_MoveDocxToFillingFormsRoom_ReturnsNoneAllowed()
    {
        // FillingFormsRoom only accepts form files (a real ONLYOFFICE PDF form); moving a regular
        // .docx returns NoneAllowed.
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest CheckDestFolder Docx FillingForms.docx", myDocsFolderId);

        var room = await CreateFillingFormsRoom("Autotest CheckDestFolder FillingFormsRoom Docx");

        var result = await CheckDestFolder(fileIds: [file.Id], destFolderId: room.Id);

        result.Result.Should().Be(CheckDestFolderResult.NoneAllowed);
    }

    [Fact]
    public async Task CheckDestFolder_MoveFormFileToFillingFormsRoom_ReturnsAllAllowed()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var formFileId = await UploadOoFormAsync(myDocsFolderId);

        var room = await CreateFillingFormsRoom("Autotest CheckDestFolder FillingFormsRoom Form");

        var result = await CheckDestFolder(fileIds: [formFileId], destFolderId: room.Id);

        result.Result.Should().Be(CheckDestFolderResult.AllAllowed);
    }

    [Fact]
    public async Task CheckDestFolder_Copy_DeleteAfterFalse_ReturnsAllAllowed()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file = await CreateFile("Autotest CheckDestFolder Copy File.docx", myDocsFolderId);

        var room = await CreateCustomRoom("Autotest CheckDestFolder Copy Dest Room");

        var result = await CheckDestFolder(fileIds: [file.Id], destFolderId: room.Id, deleteAfter: false);

        result.Result.Should().Be(CheckDestFolderResult.AllAllowed);
    }

    [Fact]
    public async Task CheckDestFolder_MoveFolder_ReturnsAllAllowed()
    {
        var sourceFolder = await CreateFolderInMy("Autotest CheckDestFolder Source Folder", Owner);

        var room = await CreateCustomRoom("Autotest CheckDestFolder Folder Dest Room");

        var result = await CheckDestFolder(folderIds: [sourceFolder.Id], destFolderId: room.Id);

        result.Result.Should().Be(CheckDestFolderResult.AllAllowed);
    }

    [Fact]
    public async Task CheckDestFolder_MoveMultipleFiles_ReturnsAllAllowed()
    {
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var file1 = await CreateFile("Autotest CheckDestFolder Multi File1.docx", myDocsFolderId);
        var file2 = await CreateFile("Autotest CheckDestFolder Multi File2.docx", myDocsFolderId);

        var room = await CreateCustomRoom("Autotest CheckDestFolder Multi Files Dest");

        var result = await CheckDestFolder(fileIds: [file1.Id, file2.Id], destFolderId: room.Id);

        result.Result.Should().Be(CheckDestFolderResult.AllAllowed);
    }

    private Task<FolderDtoInteger> CreateRoomOf(RoomType roomType, string title) => roomType switch
    {
        RoomType.EditingRoom => CreateCollaborationRoom(title),
        RoomType.PublicRoom => CreatePublicRoom(title),
        RoomType.VirtualDataRoom => CreateVDRRoom(title),
        _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, null)
    };
}
