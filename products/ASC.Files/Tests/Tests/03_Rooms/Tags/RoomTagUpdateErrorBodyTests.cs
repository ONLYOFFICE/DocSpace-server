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
/// PUT /files/tags (updateRoomTag) — shape of the error body for a rename with a blank or null name, and
/// that the rejected rename leaves the existing tag alone.
/// </summary>
/// <remarks>
/// Bug 84319: the request DTO validates the names itself, so a blank or null name is refused before the
/// action runs, with the framework's validation-error body <c>{"response":{"errors":{"NewName":[...]}}}</c>.
/// That body used to have no <c>error.message</c>, which every other 400 of the API carries, so clients
/// reading it got nothing. Every validation 400 now carries both: the per-field <c>response.errors</c> and
/// an <c>error.message</c> listing each message with its field, as "... (Parameter 'NewName')".
/// </remarks>
[Trait("Category", "Rooms")]
public class RoomTagUpdateErrorBodyTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string ExistingTag = "Autotest Rename Error Body";
    private const string BlankNameError = "A tag name cannot be empty or consist of whitespace only.";

    [Fact]
    [Trait("Bug", "84319")]
    public async Task UpdateTag_EmptyNewName_ReturnsValidationErrorForNewName()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(ExistingTag), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto(ExistingTag, ""),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        ValidationErrorsOf(exception.ErrorContent?.ToString()).Should().BeEquivalentTo(
            new Dictionary<string, string[]> { ["NewName"] = [BlankNameError] });
        ErrorMessageOf(exception.ErrorContent?.ToString()).Should().Contain(BlankNameError).And.Contain("NewName");
        await ExpectTagUnchangedAsync();
    }

    /// <summary>
    /// The null cases go over raw HTTP: the generated <c>UpdateTagRequestDto</c> refuses a null name in
    /// its constructor, so the typed client never sends such a request.
    /// </summary>
    [Theory]
    [Trait("Bug", "84319")]
    [InlineData(null, "Autotest Rename Error Body New", false, new[] { "OldName" })]
    [InlineData(ExistingTag, null, true, new[] { "NewName" })]
    [InlineData(null, null, false, new[] { "OldName", "NewName" })]
    public async Task UpdateTag_NullName_ReturnsValidationErrorForEachNullField(string? oldName, string? newName, bool tagExists, string[] fields)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        if (tagExists)
        {
            await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(ExistingTag), TestContext.Current.CancellationToken);
        }

        // Act
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/2.0/files/tags")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { oldName, newName }), Encoding.UTF8, "application/json")
        };
        using var response = await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ValidationErrorsOf(body).Should().BeEquivalentTo(fields.ToDictionary(field => field, _ => new[] { BlankNameError }));

        var errorMessage = ErrorMessageOf(body);
        errorMessage.Should().Contain(BlankNameError, "the 400 body must carry error.message (body was: {0})", body);
        errorMessage.Should().ContainAll(fields);

        if (tagExists)
        {
            await ExpectTagUnchangedAsync();
        }
    }

    private async Task ExpectTagUnchangedAsync()
    {
        var tags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        tags.Should().Contain(ExistingTag);
    }

    /// <summary>The <c>error.message</c> of an error body, or an empty string when it has none.</summary>
    private static string ErrorMessageOf(string? body)
    {
        using var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);

        return json.RootElement.TryGetProperty("error", out var error)
            && error.ValueKind == JsonValueKind.Object
            && error.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String
                ? message.GetString() ?? ""
                : "";
    }

    /// <summary>The <c>response.errors</c> of a validation-error body, field name to messages.</summary>
    private static Dictionary<string, string[]> ValidationErrorsOf(string? body)
    {
        using var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);

        if (!json.RootElement.TryGetProperty("response", out var response)
            || response.ValueKind != JsonValueKind.Object
            || !response.TryGetProperty("errors", out var errors)
            || errors.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        return errors.EnumerateObject().ToDictionary(
            field => field.Name,
            field => field.Value.EnumerateArray().Select(message => message.GetString() ?? "").ToArray());
    }
}
