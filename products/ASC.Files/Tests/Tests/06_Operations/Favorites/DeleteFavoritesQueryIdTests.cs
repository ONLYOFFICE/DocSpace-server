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

namespace ASC.Files.Tests.Tests._06_Operations.Favorites;

/// <summary>
/// <c>DELETE /api/2.0/files/favorites</c> with the ids in the query. The query binder used to paste each id into a
/// JSON literal and parse it, so an id with a double quote, a control character or a backslash, and a number with a
/// sign or a leading zero (which <c>int.TryParse</c> accepts and JSON does not), ended the request as 500. A text id
/// is now taken as it is and names nothing, like any other unknown string id; a number is read as
/// <c>int.TryParse</c> reads it.
/// </summary>
/// <remarks>
/// Sent raw: the generated client has only the body form of this operation (both forms are one operation in the
/// OpenAPI document), so it cannot put the ids into the query.
/// </remarks>
[Trait("Category", "Operations")]
[Trait("Feature", "Favorites")]
public class DeleteFavoritesQueryIdTests(
    AspireAppFixture fixture)
    : FavoritesOperationsTestBase(fixture)
{
    public static TheoryData<string> UnreadableAsJson() => ["a\"b", "a\\qb", "a\u0001b"];

    [Theory]
    [MemberData(nameof(UnreadableAsJson))]
    public async Task DeleteFavoritesFromQuery_IdUnreadableAsJson_ReturnsTrue(string id)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await DeleteFavoritesByQuery($"fileIds={Uri.EscapeDataString(id)}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ResponseOf(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .Should().Be(JsonValueKind.True);
    }

    [Fact]
    public async Task DeleteFavoritesFromQuery_SignedNumberBesideQuote_DeletesTheMark()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var file = await CreateFileInMy("Autotest favorites query signed id.docx", Owner);
        await AddFilesToFavorites(file.Id);
        var before = await PollFavorites(f => f.Files.Any(e => e.Title == file.Title));
        before.Files.Select(e => e.Title).Should().Contain(file.Title);

        // Act
        using var response = await DeleteFavoritesByQuery(
            $"fileIds={Uri.EscapeDataString("+" + file.Id.ToString(CultureInfo.InvariantCulture))}&fileIds={Uri.EscapeDataString("a\"b")}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await PollFavorites(f => f.Files.All(e => e.Title != file.Title));
        after.Files.Select(e => e.Title).Should().NotContain(file.Title);
    }

    private async Task<HttpResponseMessage> DeleteFavoritesByQuery(string query)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"api/2.0/files/favorites?{query}");

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>The kind of the <c>response</c> value of a success body, or <c>Undefined</c> when it has none.</summary>
    private static JsonValueKind ResponseOf(string? body)
    {
        using var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);

        return json.RootElement.TryGetProperty("response", out var value) ? value.ValueKind : JsonValueKind.Undefined;
    }
}
