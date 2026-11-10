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

namespace ASC.Files.Tests.Tests._03_Rooms.Instructions;

[Trait("Category", "Rooms")]
[Trait("Feature", "Instructions")]
public class SkillUploadTests(AspireAppFixture fixture) : InstructionsRoomTestBase(fixture)
{
    [Fact]
    public async Task Upload_ValidSkill_IsStoredInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload Valid");

        // Act
        var file = await UploadAsync(roomId, Skill("pdf-review"), "pdf-review.md");

        // Assert
        file.FolderId.Should().Be(roomId);
        (await FileTitlesAsync(roomId)).Should().Equal("pdf-review.md");
    }

    [Fact]
    public async Task Upload_WithoutFrontmatter_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload No Frontmatter");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadAsync(roomId, Text("# Just a heading\n\nNo frontmatter here.\n"), "plain.md"));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(FrontmatterMissingError);
        (await FileTitlesAsync(roomId)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Pdf-Review")]
    [InlineData("-pdf-review")]
    [InlineData("pdf--review")]
    [InlineData("pdf_review")]
    public async Task Upload_InvalidName_Returns400(string name)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload Bad Name");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadAsync(roomId, Skill(name), "skill.md"));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(NameInvalidError);
    }

    [Fact]
    public async Task Upload_NameOf65Characters_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload Long Name");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadAsync(roomId, Skill(new string('a', 65)), "skill.md"));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(NameInvalidError);
    }

    [Fact]
    public async Task Upload_DescriptionOf1025Characters_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload Long Description");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadAsync(roomId, Skill("pdf-review", new string('a', 1025)), "pdf-review.md"));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(DescriptionInvalidError);
    }

    [Fact]
    public async Task Upload_DescriptionWithXmlTag_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload Xml Description");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadAsync(roomId, Skill("pdf-review", "Reviews <b>PDF</b> documents"), "pdf-review.md"));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(DescriptionInvalidError);
    }

    [Theory]
    [InlineData("skill.txt")]
    [InlineData("skill.docx")]
    public async Task Upload_NotMarkdown_Returns400(string fileName)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload Not Markdown");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadAsync(roomId, Skill("pdf-review"), fileName));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(MarkdownOnlyError);
    }

    [Fact]
    public async Task Upload_InvalidMarkdown_ToCustomRoom_IsStored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Skills Upload Custom Room");

        // Act
        var file = await UploadAsync(room.Id, Text("# No frontmatter\n"), "plain.md");

        // Assert
        file.FolderId.Should().Be(room.Id);
    }

    [Fact]
    public async Task Upload_ValidNewVersion_UpdatesTheFile()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload New Version");
        var first = await UploadAsync(roomId, Skill("pdf-review"), "pdf-review.md");

        // Act
        var second = await UploadAsync(roomId, Skill("pdf-review", "Reviews PDF documents in depth"), "pdf-review.md", createNewIfExist: false);

        // Assert
        second.Id.Should().Be(first.Id);
        second.Version.Should().Be(2);
    }

    [Fact]
    public async Task Upload_InvalidNewVersion_Returns400AndKeepsThePreviousVersion()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Upload Bad Version");
        var first = await UploadAsync(roomId, Skill("pdf-review"), "pdf-review.md");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadAsync(roomId, Text("# No frontmatter\n"), "pdf-review.md", createNewIfExist: false));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await GetFile(first.Id)).Version.Should().Be(1);
    }

    [Fact]
    public async Task ChunkedUpload_ValidSkill_IsStoredInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Chunked Valid");

        // Act
        var uploaded = await UploadChunkedAsync(roomId, Skill("pdf-review"), "pdf-review.md");

        // Assert
        uploaded.FolderId.Should().Be(roomId);
        (await FileTitlesAsync(roomId)).Should().Equal("pdf-review.md");
    }

    [Fact]
    public async Task ChunkedUpload_InvalidSkill_Returns400AndStoresNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var roomId = await CreateInstructionsRoomAsync("Autotest Skills Chunked Invalid");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadChunkedAsync(roomId, Skill("Pdf Review"), "pdf-review.md"));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain(NameInvalidError);
        (await FileTitlesAsync(roomId)).Should().BeEmpty();
    }
}
