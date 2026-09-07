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

namespace ASC.Files.Tests.Tests._03_Rooms.Tags;

/// <summary>
/// PUT /files/tags (updateRoomTag) — whitespace/case/Unicode handling, chained renames and the
/// "no side effects on a failed rename" cases. Split from <see cref="RoomTagUpdateTests"/>
/// purely to stay under the ~24-case class-size guideline; both classes cover the same endpoint.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagUpdateEdgeCasesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <remarks>
    /// An empty <c>newName</c> is rejected with 400 (see <see cref="RoomTagUpdateTests.UpdateTag_EmptyNewName_ReturnsBadRequest"/>),
    /// but a whitespace-only one is accepted with 200 — validation should treat the two the same.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82372")]
    public async Task UpdateTag_WhitespaceOnlyNewName_ShouldBeRejectedButIsAccepted()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Whitespace New"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Whitespace New", "   "),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateTag_NewNameWithSurroundingSpaces_StoredVerbatim()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Trim Source"), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Trim Source", "  Autotest Trimmed Tag  "),
            TestContext.Current.CancellationToken);

        // Assert — the API does not trim newName.
        var tags = await GetTagCatalog();
        tags.Should().Contain("  Autotest Trimmed Tag  ").And.NotContain("Autotest Trimmed Tag");
    }

    [Fact]
    public async Task UpdateTag_OldNameWithSurroundingSpaces_DoesNotMatch_ReturnsNotFound()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Old Trim Match"), TestContext.Current.CancellationToken);

        // Act — oldName is matched exactly, so a padded oldName does not match the stored tag.
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("  Autotest Old Trim Match  ", "Autotest Old Trim Match Renamed"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);

        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest Old Trim Match").And.NotContain("Autotest Old Trim Match Renamed");
    }

    /// <remarks>Tags are case-insensitive: renaming a tag to a case-variant of itself collides with itself.</remarks>
    [Fact]
    public async Task UpdateTag_RenameToDifferentCase_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Case Old"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Case Old", "autotest case old"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("already exists");

        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest Case Old");
    }

    [Fact]
    public async Task UpdateTag_RenameToCaseVariantOfExistingTag_ReturnsBadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest CaseConflict Tag"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest CaseConflict Source"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest CaseConflict Source", "autotest caseconflict tag"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateTag_NewNameWithCyrillicCharacters_StoredVerbatim()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Cyrillic Source"), TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Cyrillic Source", "Автотест Тег Кириллица"),
            TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be("Автотест Тег Кириллица");

        var tags = await GetTagCatalog();
        tags.Should().Contain("Автотест Тег Кириллица");
    }

    /// <remarks>
    /// Bug 82374, closed as by-design for the same reason as bug 81682 on create: <c>files_tag.name</c>
    /// is a <c>utf8</c> column, which cannot hold a character outside the Basic Multilingual Plane.
    /// Cyrillic and other BMP Unicode are accepted (see the Cyrillic test above); a rename to an emoji
    /// is refused, and the old name must survive the refusal.
    /// </remarks>
    [Fact]
    [Trait("Bug", "82374")]
    public async Task UpdateTag_NewNameOutsideBmp_Rejected()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Emoji Source"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Emoji Source", "Autotest Emoji 🚀🔥"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(500);

        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest Emoji Source", "a refused rename must leave the old name in place");
    }

    [Fact]
    public async Task UpdateTag_NewNameWithPunctuation_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Punct Source"), TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Punct Source", "Autotest-Punct_Tag.v2"),
            TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be("Autotest-Punct_Tag.v2");
    }

    [Fact]
    public async Task UpdateTag_NewNameWithQuotes_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Quotes Source"), TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest Quotes Source", "Autotest \"Quoted\" Tag"),
            TestContext.Current.CancellationToken);

        // Assert
        var tags = await GetTagCatalog();
        tags.Should().Contain(response.Response);
    }

    [Fact]
    public async Task UpdateTag_NewNameWithHtmlLikeString_DoesNotBreakApi()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest XSS Source"), TestContext.Current.CancellationToken);

        // Act
        var response = await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto("Autotest XSS Source", "<script>alert(1)</script>"),
            TestContext.Current.CancellationToken);

        // Assert — the API must not 500; capture whatever the sanitized/stored value is.
        var tags = await GetTagCatalog();
        tags.Should().Contain(response.Response);
        tags.Should().NotContain("Autotest XSS Source");
    }

    [Fact]
    public async Task UpdateTag_VeryLongNewName_DoesNotReturnServerError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Long Source"), TestContext.Current.CancellationToken);
        var longName = new string('A', 1000);

        // Act & Assert — must not be a server error, regardless of whether the length limit is enforced.
        try
        {
            await _roomsApi.UpdateRoomTagAsync(new UpdateTagRequestDto("Autotest Long Source", longName), TestContext.Current.CancellationToken);
        }
        catch (ApiException exception)
        {
            exception.ErrorCode.Should().NotBe(500);
        }
    }

    [Fact]
    public async Task UpdateTag_ChainedRename_LeavesOnlyFinalName()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Chain A"), TestContext.Current.CancellationToken);
        var room = await CreateCustomRoom("Autotest Chain Room");
        await _roomsApi.AddRoomTagsAsync(room.Id, new BatchTagsRequestDto(["Autotest Chain A"]), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(new UpdateTagRequestDto("Autotest Chain A", "Autotest Chain B"), TestContext.Current.CancellationToken);
        await _roomsApi.UpdateRoomTagAsync(new UpdateTagRequestDto("Autotest Chain B", "Autotest Chain C"), TestContext.Current.CancellationToken);

        // Assert
        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest Chain C").And.NotContain("Autotest Chain A").And.NotContain("Autotest Chain B");

        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        roomInfo.Tags.Should().Contain("Autotest Chain C");
    }

    [Fact]
    public async Task UpdateTag_RepeatedRename_ReturnsNotFoundSecondTime()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Idempotent Old"), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(new UpdateTagRequestDto("Autotest Idempotent Old", "Autotest Idempotent New"), TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Idempotent Old", "Autotest Idempotent New"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);

        var tags = await GetTagCatalog();
        tags.Count(t => t == "Autotest Idempotent New").Should().Be(1);
    }

    [Fact]
    public async Task UpdateTag_ConflictingRename_LeavesBothTagsIntact()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest SideEffect Source"), TestContext.Current.CancellationToken);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest SideEffect Target"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest SideEffect Source", "Autotest SideEffect Target"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);

        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest SideEffect Source").And.Contain("Autotest SideEffect Target");
    }

    [Fact]
    public async Task UpdateTag_FailedRenameOfNonExistentTag_DoesNotCreateIt()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Ghost Old 424242", "Autotest Ghost New 424242"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(404);

        var tags = await GetTagCatalog();
        tags.Should().NotContain("Autotest Ghost New 424242").And.NotContain("Autotest Ghost Old 424242");
    }

    [Fact]
    public async Task UpdateTag_RenamingOneOfMultipleRoomTags_LeavesOthersUnchanged()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest MultiTag Room");
        await _roomsApi.AddRoomTagsAsync(
            room.Id,
            new BatchTagsRequestDto(["Autotest Multi A", "Autotest Multi B", "Autotest Multi C"]),
            TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(new UpdateTagRequestDto("Autotest Multi B", "Autotest Multi B Renamed"), TestContext.Current.CancellationToken);

        // Assert
        var roomInfo = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        roomInfo.Tags.Should().Contain("Autotest Multi A")
            .And.Contain("Autotest Multi B Renamed")
            .And.Contain("Autotest Multi C")
            .And.NotContain("Autotest Multi B");
    }

    [Fact]
    public async Task UpdateTag_RenamingUnattachedTag_Succeeds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Unused Old"), TestContext.Current.CancellationToken);

        // Act
        await _roomsApi.UpdateRoomTagAsync(new UpdateTagRequestDto("Autotest Unused Old", "Autotest Unused New"), TestContext.Current.CancellationToken);

        // Assert
        var tags = await GetTagCatalog();
        tags.Should().Contain("Autotest Unused New").And.NotContain("Autotest Unused Old");
    }

    /// <summary>Reads the tag catalog and unwraps it into plain strings.</summary>
    private async Task<List<string>> GetTagCatalog()
    {
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return tags.ConvertAll(t => t.ToString()!);
    }
}
