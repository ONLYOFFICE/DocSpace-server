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

namespace ASC.AI.Tests.Tests.SettingsTests;

/// <summary>
/// Bodies of <c>PUT /api/2.0/ai/config/user</c> the route has to refuse. <c>chatRecommendedModelVisible</c> is a
/// mandatory boolean: every spelling of "true"/"false" that is not a JSON boolean, every body without a usable value
/// and every body that is not an object is a 400, and the stored value does not move — from a stored true AND a
/// stored false, so a missing field bound to either default would show.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Settings")]
public class SettingsUserConfigBodyTests(AspireAppFixture fixture) : SettingsTestBase(fixture)
{
    private const string RequiredError = "chatRecommendedModelVisible is required and must be a boolean";

    private static Dictionary<string, object> BodyOf(string kind) => kind switch
    {
        "stringTrue" => new() { [ChatRecommendedModelVisible] = "true" },
        "stringFalse" => new() { [ChatRecommendedModelVisible] = "false" },
        "stringYes" => new() { [ChatRecommendedModelVisible] = "yes" },
        "numberOne" => new() { [ChatRecommendedModelVisible] = 1 },
        "numberZero" => new() { [ChatRecommendedModelVisible] = 0 },
        "arrayValue" => new() { [ChatRecommendedModelVisible] = Array.Empty<object>() },
        "objectValue" => new() { [ChatRecommendedModelVisible] = new Dictionary<string, object>() },
        "nullValue" => new() { [ChatRecommendedModelVisible] = null! },
        "unknownOnly" => new() { ["unknownField"] = 1 },
        "longFieldName" => new() { [new string('x', 5000)] = 1 },
        "manyUnknownFields" => Enumerable.Range(0, 500).ToDictionary(i => $"field{i}", object (i) => i),
        // Observed, not a documented promise: the name binds case-sensitively.
        "caseSensitive" => new() { ["ChatRecommendedModelVisible"] = false },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    [Theory]
    [InlineData("stringTrue")]
    [InlineData("stringFalse")]
    [InlineData("stringYes")]
    [InlineData("numberOne")]
    [InlineData("numberZero")]
    [InlineData("arrayValue")]
    [InlineData("objectValue")]
    [InlineData("nullValue")]
    [InlineData("unknownOnly")]
    [InlineData("longFieldName")]
    [InlineData("manyUnknownFields")]
    [InlineData("caseSensitive")]
    public async Task SetUserConfig_BodyWithoutAUsableBoolean_Returns400AndChangesNothingInEitherDirection(string kind)
    {
        foreach (var stored in new[] { true, false })
        {
            await WriteVisibleAsync(stored);

            var (status, error) = await OutcomeOfAsync(() => _aiSettings.AiSettingsSetUserAsync(BodyOf(kind), TestContext.Current.CancellationToken));

            // State first: the 400 is only meaningful if nothing was stored.
            (await ReadVisibleAsync()).Should().Be(stored);
            status.Should().Be(400, $"stored {stored}: {error}");
            error.Should().Contain(RequiredError);
        }
    }

    [Fact]
    [Trait("Bug", "82725")]
    public async Task SetUserConfig_EmptyBody_IsRefusedAndTheStoredPreferenceSurvives()
    {
        // The DTO carries a single flag. `{}` used to bind the missing field as false and overwrite the user's stored
        // preference: silent data loss on a partial update. The field is mandatory instead.
        (await ReadVisibleAsync()).Should().BeTrue("the shipped default is what an overwrite with false would lose");

        var (status, error) = await OutcomeOfAsync(() => _aiSettings.AiSettingsSetUserAsync([], TestContext.Current.CancellationToken));

        (await ReadVisibleAsync()).Should().BeTrue();
        status.Should().Be(400, error);
        error.Should().Contain(RequiredError);
    }

    [Theory]
    [InlineData("[1,2]")]
    [InlineData("{bad")]
    [InlineData("null")]
    public async Task SetUserConfig_BodyThatIsNotAnObject_Returns400AndChangesNothing(string json)
    {
        await WriteVisibleAsync(true);

        // Raw: the typed call always sends a JSON object.
        var (status, body) = await RawAsync(HttpMethod.Put, UserConfigRoute, json);

        (await ReadVisibleAsync()).Should().BeTrue();
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task SetUserConfig_NoBodyAtAll_Returns400AndChangesNothing()
    {
        await WriteVisibleAsync(false);

        // Raw: a request without a body cannot be sent through the typed call.
        var (status, body) = await RawAsync(HttpMethod.Put, UserConfigRoute);

        (await ReadVisibleAsync()).Should().BeFalse();
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task SetUserConfig_ValidValueNextToAnUnknownField_IsAppliedAndTheUnknownFieldIgnored()
    {
        await WriteVisibleAsync(true);

        await _aiSettings.AiSettingsSetUserAsync(
            new Dictionary<string, object> { [ChatRecommendedModelVisible] = false, ["unknownField"] = "ignored" },
            TestContext.Current.CancellationToken);

        (await ReadVisibleAsync()).Should().BeFalse();
    }

    /// <summary>
    /// A 200 has to mean "stored". Until Bug 84430 was fixed a body the JSON parser could not read (text/plain, no
    /// Content-Type) arrived as <c>{}</c> and was acknowledged without effect. A 4xx is fine; so is applying it. What is
    /// not fine is acknowledging a write that never happened.
    /// </summary>
    [Theory]
    [Trait("Bug", "84430")]
    [InlineData("text/plain")]
    [InlineData(null)]
    public async Task SetUserConfig_ValidBodyWithoutAJsonContentType_IsAppliedOrRefusedNeverAcknowledgedWithoutEffect(string? contentType)
    {
        await WriteVisibleAsync(true);

        // Raw: the content type is the thing under test.
        using var request = new HttpRequestMessage(HttpMethod.Put, UserConfigRoute)
        {
            Content = new StringContent("""{"chatRecommendedModelVisible":false}""", System.Text.Encoding.UTF8, "text/plain")
        };
        request.Content.Headers.ContentType = contentType is null ? null : new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        using var response = await _aiChatClient.SendAsync(request, TestContext.Current.CancellationToken);

        var after = await ReadVisibleAsync();

        (response.StatusCode != HttpStatusCode.OK || !after).Should().BeTrue($"PUT answered {(int)response.StatusCode} but the stored value is {after}");
    }
}
