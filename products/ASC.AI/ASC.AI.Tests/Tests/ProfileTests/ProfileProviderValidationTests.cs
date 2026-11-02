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

namespace ASC.AI.Tests.Tests.ProfileTests;

/// <summary>
/// Request validation of <c>profiles/list-provider-models</c>: a missing field is named, an unparsable URL is refused
/// before any connection, a key that cannot travel in an HTTP header is a 400 and no input is ever echoed back.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Profiles")]
public class ProfileProviderValidationTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    private const string ListProviderModelsPath = $"{ProfilesRoute}/list-provider-models";

    private static System.Text.Json.Nodes.JsonObject ValidBody() =>
        new()
        {
            ["providerType"] = "openai",
            ["baseUrl"] = "https://api.example.invalid/v1",
            ["apiKey"] = "sk-autotest-not-a-real-key"
        };

    [Theory]
    [InlineData("providerType", "omitted")]
    [InlineData("providerType", "null")]
    [InlineData("providerType", "empty")]
    [InlineData("baseUrl", "omitted")]
    [InlineData("baseUrl", "null")]
    [InlineData("baseUrl", "empty")]
    public async Task ListProviderModels_MissingOrEmptyRequiredField_NamesExactlyThatField(string field, string mode)
    {
        var body = ValidBody();

        if (mode == "omitted")
        {
            body.Remove(field);
        }
        else
        {
            body[field] = mode == "null" ? null : string.Empty;
        }

        // Raw on purpose: the typed request refuses null for both fields client-side, so it sends nothing.
        var outcome = await RawPostAsync(ListProviderModelsPath, body.ToJsonString());

        outcome.Status.Should().Be(400, outcome.Body);
        using var document = JsonDocument.Parse(outcome.Body);
        document.RootElement.GetProperty("field").GetString().Should().Be(field);
        document.RootElement.GetProperty("error").GetString().Should().Be($"{field} required");
    }

    [Fact]
    public async Task ListProviderModels_EmptyBody_Returns400()
    {
        // Raw on purpose: the typed request cannot be built without its required fields.
        var outcome = await RawPostAsync(ListProviderModelsPath, "{}");

        outcome.Status.Should().Be(400, outcome.Body);
    }

    /// <summary>
    /// Bug 83116 (fixed): a missing base URL and a missing provider type were both reported with the same message
    /// that named both fields. Each is now reported without blaming the other.
    /// </summary>
    [Fact]
    [Trait("Bug", "83116")]
    public async Task ListProviderModels_MissingBaseUrlAndMissingProviderType_AreReportedSeparately()
    {
        var noBaseUrl = ValidBody();
        noBaseUrl.Remove("baseUrl");
        var noProviderType = ValidBody();
        noProviderType.Remove("providerType");

        // Raw on purpose: see above, the typed request cannot omit a required field.
        var missingBaseUrl = await RawPostAsync(ListProviderModelsPath, noBaseUrl.ToJsonString());
        var missingProviderType = await RawPostAsync(ListProviderModelsPath, noProviderType.ToJsonString());

        missingBaseUrl.Status.Should().Be(400);
        missingProviderType.Status.Should().Be(400);
        // The body names the field in `field` too, so only the message is compared with the other field's name.
        using var baseUrlDocument = JsonDocument.Parse(missingBaseUrl.Body);
        using var providerTypeDocument = JsonDocument.Parse(missingProviderType.Body);
        baseUrlDocument.RootElement.GetProperty("error").GetString().Should().NotContain("providerType");
        providerTypeDocument.RootElement.GetProperty("error").GetString().Should().NotContain("baseUrl");
    }

    [Theory]
    [InlineData("not-a-url", "baseUrl is not a valid URL")]
    [InlineData("api.deepseek.com", "baseUrl is not a valid URL")]
    [InlineData("https://api.deep seek.com", "baseUrl is not a valid URL")]
    [InlineData("https://api.deepseek.com:99999", "baseUrl is not a valid URL")]
    [InlineData("https://user:pass@api.deepseek.com", "baseUrl must not contain credentials")]
    public async Task ListProviderModels_UnparsableBaseUrl_IsRefusedBeforeAnyConnection(string baseUrl, string message)
    {
        var outcome = await ProviderModelsOutcomeAsync("deepseek", baseUrl, "sk-autotest-not-a-real-key");

        outcome.Status.Should().Be(400, outcome.Body);
        outcome.Body.Should().Contain(message);
    }

    /// <summary>
    /// Bug 84428: a key the HTTP client cannot put into a header (non-ASCII, a byte-order mark, NUL, a line break)
    /// made the request fail inside the portal and read as a provider outage (502). It is the caller's input error,
    /// a 400, on every transport - ollama included, which needs no key but is refused the same way.
    /// </summary>
    [Theory]
    [Trait("Bug", "84428")]
    [InlineData("deepseek")]
    [InlineData("openai")]
    [InlineData("anthropic")]
    [InlineData("genai")]
    [InlineData("ollama")]
    public async Task ListProviderModels_KeyThatCannotBeSentAsAnHttpHeader_Returns400(string transport)
    {
        string[] unsendableKeys =
        [
            "ключ-abc",
            "密钥-abc",
            "k-\U0001F511",
            "﻿sk-abc",
            "sk-abc\u0000def",
            "sk-abc\nX-A: b"
        ];
        var notRefused = new List<string>();

        foreach (var key in unsendableKeys)
        {
            var outcome = await ProviderModelsOutcomeAsync(transport, ModelProvider.BaseUrl, key);

            if (outcome.Status != 400 || !outcome.Body.Contains("cannot be sent in an HTTP header"))
            {
                notRefused.Add($"{key.Length} chars starting {(int)key[0]:X4}: {outcome.Status} {outcome.Body}");
            }
        }

        notRefused.Should().BeEmpty($"{transport}: keys that were not refused as unsendable");
    }

    [Fact]
    public async Task ListProviderModels_KeyIsNeverEchoedBack_AndNothingIsStored()
    {
        await CreateProfileAsync();
        var before = await CatalogueSnapshotAsync();
        using var provider = CreateKeyCheckingProvider();

        // A key the provider rejects, and a request refused on its URL while carrying the key: both answers could quote their input.
        var rejected = await ProviderModelsOutcomeAsync("openai", provider.BaseUrl, LeakProbeKey);
        var refused = await ProviderModelsOutcomeAsync("openai", "http://169.254.169.254/latest/meta-data/", LeakProbeKey);

        rejected.Status.Should().Be(400, "the key reached the provider check");
        rejected.Body.Should().Contain(InvalidKeyMessage);
        refused.Status.Should().Be(400, "the URL was refused");

        rejected.Body.Should().NotContain(LeakProbeKey);
        refused.Body.Should().NotContain(LeakProbeKey);

        var after = await CatalogueSnapshotAsync();
        after.Should().Be(before, "discovery stores nothing");
        after.Should().NotContain(LeakProbeKey);
    }
}
