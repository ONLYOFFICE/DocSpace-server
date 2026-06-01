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

namespace ASC.Files.Tests.Tests._01_Files;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "Files")]
public class FileCreateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{

    public static TheoryData<string> Data =>
    [
        "test.docx",
        "test.pptx",
        "test.xlsx",
        "test.pdf"
    ];

    public static TheoryData<string, EmployeeType> DataWithEmployeeType => new MatrixTheoryData<string, EmployeeType>(
        [
            "test.docx",
            "test.pptx",
            "test.xlsx",
            "test.pdf"
        ],
        [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User]
    );
    public static TheoryData<FolderType> FolderTypesData =>
    [
        FolderType.Archive,
        FolderType.TRASH,
        FolderType.VirtualRooms
    ];

    [Theory]
    [MemberData(nameof(Data))]
    public async Task CreateFile_FolderMy_Owner_ReturnsOk(string fileName)
    {
        var createdFile = await CreateFileInMy(fileName, Initializer.Owner);

        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);

        // Verify the file was created
        var file = await GetFile(createdFile.Id);
        file.Should().NotBeNull();
        file.Title.Should().Be(fileName);
    }

    [Theory]
    [MemberData(nameof(DataWithEmployeeType))]
    public async Task CreateFile_FolderMy_Admin_ReturnsOk(string fileName, EmployeeType employeeType)
    {
        var roomAdmin = await Initializer.InviteContact(employeeType);

        var createdFile = await CreateFileInMy(fileName, roomAdmin);

        createdFile.Should().NotBeNull();
        createdFile.Title.Should().Be(fileName);

        // Verify the file was created
        var file = await GetFile(createdFile.Id);
        file.Should().NotBeNull();
        file.Title.Should().Be(fileName);
    }

    [Fact]
    public async Task CreateFile_FolderDoesNotExist_ReturnsFileInMy()
    {
        await _filesClient.Authenticate(Initializer.Owner);

        //Arrange
        var file = new CreateFileJsonElement("test.docx");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateFileAsync(Random.Shared.Next(10000, 20000), file, cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Theory]
    [MemberData(nameof(FolderTypesData))]
    public async Task CreateFile_SystemFolder_Owner_ReturnsOk(FolderType folderType)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () =>  await CreateFile("test.docx", folderType, Initializer.Owner));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task CreateFile_NameLongerThan165Chars_Returns400()
    {
        await _filesClient.Authenticate(Initializer.Owner);

        // Arrange
        var longFileName = new string('a', 166) + ".docx"; // 166 characters + 5 for extension = 171 characters
        var file = new CreateFileJsonElement(longFileName);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.CreateFileAsync(
                await GetUserFolderIdAsync( Initializer.Owner),
                file,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateTextFile_ValidContent_ReturnsNewTextFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var userFolderId = await GetUserFolderIdAsync(Initializer.Owner);
        var fileName = "new_text_file";
        var content = "This is the content of my text file.";

        // Act
        var createParams = new CreateTextOrHtmlFile(
            title: fileName,
            content: content,
            createNewIfExist: true
        );

        var result = (await _filesApi.CreateTextFileAsync(userFolderId, createParams, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(fileName + ".txt");
        result.FolderId.Should().Be(userFolderId);
        result.FileExst.Should().Be(".txt");

        // We cannot directly verify content in this test without downloading the file
    }

    [Fact]
    public async Task CreateHtmlFile_ValidContent_ReturnsNewHtmlFile()
    {
        // Arrange
        await _filesClient.Authenticate(Initializer.Owner);

        var userFolderId = await GetUserFolderIdAsync(Initializer.Owner);
        var fileName = "new_html_file";
        var content = "<html><body><h1>Test HTML</h1><p>This is a test HTML file.</p></body></html>";

        // Act
        var createParams = new CreateTextOrHtmlFile(
            title: fileName,
            content: content,
            createNewIfExist: true
        );

        var result = (await _filesApi.CreateHtmlFileAsync(userFolderId, createParams, TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be(fileName + ".html");
        result.FolderId.Should().Be(userFolderId);
        result.FileExst.Should().Be(".html");
    }
}
