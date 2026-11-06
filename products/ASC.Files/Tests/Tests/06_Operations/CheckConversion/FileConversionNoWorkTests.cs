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

namespace ASC.Files.Tests.Tests._06_Operations.CheckConversion;

/// <summary>
/// <c>PUT</c> and <c>GET /api/2.0/files/file/{fileId}/checkconversion</c> where there is nothing to
/// convert, nothing to find, or nobody to ask: a 200 with an empty list and no work, or a refusal. All
/// of it is decided synchronously, so none of it needs a document server.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class FileConversionNoWorkTests(
    AspireAppFixture fixture)
    : FileConversionTestBase(fixture)
{
    [Theory]
    [InlineData("text.txt", "plain text\nsecond line\n")]
    [InlineData("table.csv", "name,value\nalpha,1\nbeta,2\n")]
    [InlineData("notes.md", "# Heading\n\ntext\n")]
    public async Task StartFileConversion_FormatThatNeedsNoConversion_AnswersEmptyWithNoJobNoNewFileAndNoNewVersion(string name, string text)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder($"Autotest Convert None {name}", myDocsFolderId);
        var bytes = Encoding.UTF8.GetBytes(text);
        var id = await Put(dir.Id, $"Autotest Convert None {name}", bytes);
        var before = await FileTitles(dir.Id);

        // Act
        var started = await Start(id, new CheckConversionRequestDto(startConvert: true));

        // Assert
        started.Should().BeEmpty();
        (await ReadEntries(id)).Should().BeEmpty();
        (await FileTitles(dir.Id)).Should().Equal(before);
        await AssertSourceIntact(id, bytes);
    }

    [Theory]
    [InlineData("docx")]
    [InlineData("xlsx")]
    [InlineData("pptx")]
    public async Task StartFileConversion_AlreadyEditableFormat_QueuesNothingAndAddsNoFileAndNoVersion(string extension)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder($"Autotest Convert Editable {extension}", myDocsFolderId);
        var file = await CreateFile($"Autotest Convert Editable.{extension}", dir.Id);
        var before = await FileTitles(dir.Id);

        // Act
        var started = await Start(file.Id, new CheckConversionRequestDto(startConvert: true));

        // Assert
        started.Should().BeEmpty();
        (await FileTitles(dir.Id)).Should().Equal(before);
        (await _filesApi.GetFileVersionInfoAsync(file.Id, TestContext.Current.CancellationToken)).Response.Should().ContainSingle();
    }

    [Theory]
    [InlineData(99999999)]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Conversion_FileThatDoesNotExist_Answers200WithAnEmptyListForBothMethods(int fileId)
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var before = await FileTitles(myDocsFolderId);

        // Act
        var started = await Start(fileId);
        var read = await ReadEntries(fileId);

        // Assert
        started.Should().BeEmpty();
        read.Should().BeEmpty();
        (await FileTitles(myDocsFolderId)).Should().Equal(before);
    }

    [Fact]
    public async Task Conversion_IdThatIsNotANumber_IsAClientErrorForBothMethods()
    {
        // Act
        var start = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.StartFileConversionAsync("abc", cancellationToken: TestContext.Current.CancellationToken));
        var read = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesOperationsApi.CheckConversionStatusAsync("abc", cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        start.ErrorCode.Should().BeInRange(400, 499);
        read.ErrorCode.Should().BeInRange(400, 499);
    }

    [Fact]
    public async Task Conversion_UserWithNoAccessToTheFile_IsRefusedWith403ByBothMethodsAndNothingIsMade()
    {
        // Arrange
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var dir = await CreateFolder("Autotest Convert Private", myDocsFolderId);
        var bytes = Rtf("Private text");
        var id = await Put(dir.Id, "Autotest Convert Private.rtf", bytes);
        var before = await FileTitles(dir.Id);
        var outsider = await InviteMember(EmployeeType.User);

        // Act
        await _filesClient.Authenticate(outsider);
        var start = await Assert.ThrowsAsync<ApiException>(async () => await Start(id));
        var read = await Assert.ThrowsAsync<ApiException>(async () => await ReadEntries(id, start: true));

        // Assert
        start.ErrorCode.Should().Be(403);
        read.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await ReadEntries(id)).Should().BeEmpty();
        (await FileTitles(dir.Id)).Should().Equal(before);
        await AssertSourceIntact(id, bytes);
    }

    [Fact]
    public async Task CheckConversionStatus_UserWithReadAccess_MayAskAndGetsAnEmptyListWhileNoneIsKnown()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Convert Read Status Room");
        var id = await Put(room.Id, "Autotest Convert ReadStatus.rtf", Rtf("Read status"));
        var reader = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, reader, FileShare.Read);

        // Act
        await _filesClient.Authenticate(reader);
        var entries = await ReadEntries(id);

        // Assert
        entries.Should().BeEmpty();
    }
}
