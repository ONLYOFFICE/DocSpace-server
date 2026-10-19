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
/// PUT /files/tags (updateRoomTag) — shape of the error body for a rejected rename.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTagUpdateErrorBodyTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <remarks>
    /// Bug 84319: the 400 for an empty <c>newName</c> used to carry <c>{"error":{"message":...}}</c>;
    /// validation failures on this field now return ASP.NET's ProblemDetails shape
    /// (<c>{"response":{"errors":{"NewName":[...]}}}</c>), so <c>error.message</c> is gone and
    /// clients that read it get nothing. The TypeScript suite expects the legacy text
    /// "The value cannot be an empty string. (Parameter 'newName')"; that exact text comes from an
    /// ArgumentException the DTO validation now pre-empts, so only the presence of a non-empty
    /// <c>error.message</c> is asserted here.
    /// </remarks>
    [Fact]
    [Trait("Bug", "84319")]
    public async Task UpdateTag_EmptyNewName_ErrorBodyIncludesErrorMessage()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Empty New Name Body"), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Empty New Name Body", ""),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);

        using var json = JsonDocument.Parse(exception.ErrorContent?.ToString() ?? "{}");
        var hasMessage = json.RootElement.TryGetProperty("error", out var error)
            && error.ValueKind == JsonValueKind.Object
            && error.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(message.GetString());

        hasMessage.Should().BeTrue("the 400 body must carry error.message (body was: {0})", exception.ErrorContent);
    }
}
