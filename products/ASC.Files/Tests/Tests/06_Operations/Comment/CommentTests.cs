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

namespace ASC.Files.Tests.Tests._06_Operations.Comment;

/// <summary>
/// <c>PUT /api/2.0/files/file/{fileId}/comment</c> — functional coverage: setting, clearing and
/// overwriting a file version comment, unicode/HTML payloads, and the known defects around length
/// and id/version validation. Access control lives in <see cref="CommentPermissionsTests"/>.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class CommentTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    [Fact]
    public async Task Comment_Update_ReturnsUpdatedText()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Basic.docx", Owner);

        // Act
        var comment = (await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Initial comment"), TestContext.Current.CancellationToken)).Response;

        // Assert
        comment.Should().Be("Initial comment");
    }

    [Fact]
    public async Task Comment_UpdateToEmptyString_ReturnsEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Empty.docx", Owner);

        // Act
        var comment = (await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: ""), TestContext.Current.CancellationToken)).Response;

        // Assert
        comment.Should().BeEmpty();
    }

    [Fact]
    public async Task Comment_UpdateToNull_ClearsComment()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Null.docx", Owner);

        // Act
        var comment = (await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: null), TestContext.Current.CancellationToken)).Response;

        // Assert
        comment.Should().BeEmpty();
    }

    [Fact]
    public async Task Comment_Overwrite_ReturnsNewText()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Overwrite.docx", Owner);

        await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "First comment"), TestContext.Current.CancellationToken);

        // Act
        var comment = (await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: "Updated comment"), TestContext.Current.CancellationToken)).Response;

        // Assert
        comment.Should().Be("Updated comment");
    }

    [Fact]
    public async Task Comment_WithDiacriticsAndUnicode_ReturnsUnchangedText()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Unicode.docx", Owner);
        const string text = "Ñoño café über naïve Ångström 山島あ";

        // Act
        var comment = (await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: text), TestContext.Current.CancellationToken)).Response;

        // Assert
        comment.Should().Be(text);
    }

    [Fact]
    public async Task Comment_WithHtmlLikeCharacters_ReturnsUnchangedText()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment HTML.docx", Owner);
        const string text = "<b>bold</b> & 'quoted' \"double\"";

        // Act
        var comment = (await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: text), TestContext.Current.CancellationToken)).Response;

        // Assert
        comment.Should().Be(text);
    }

    /// <remarks>
    /// BUG 82266: a comment longer than 255 characters is silently truncated instead of being
    /// rejected. Asserts the behaviour the product should have.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82266")]
    public async Task Comment_TooLong_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Long.docx", Owner);
        var comment = new string('A', 2000);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 1, comment: comment), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// BUG 82268: a non-existent file id is reported as 403 (SecurityException) instead of 404.
    /// Asserts the behaviour the product should have.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82268")]
    public async Task Comment_NonExistentFileId_Returns404()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            999999999, new UpdateComment(version: 1, comment: "test"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);
    }

    /// <remarks>
    /// BUG 82271: a non-existent version is reported as 403 (SecurityException) instead of 400.
    /// Asserts the behaviour the product should have.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82271")]
    public async Task Comment_NonExistentVersion_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment BadVersion.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 999, comment: "test"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// BUG 82271: version 0 is reported as 403 (SecurityException) instead of 400. Asserts the
    /// behaviour the product should have.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82271")]
    public async Task Comment_VersionZero_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment Version0.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: 0, comment: "test"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    /// <remarks>
    /// BUG 82271: a negative version is reported as 403 (SecurityException) instead of 400. Asserts
    /// the behaviour the product should have.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82271")]
    public async Task Comment_NegativeVersion_Returns400()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest UpdateComment NegVer.docx", Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesOperationsApi.UpdateFileCommentAsync(
            file.Id, new UpdateComment(version: -1, comment: "test"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
