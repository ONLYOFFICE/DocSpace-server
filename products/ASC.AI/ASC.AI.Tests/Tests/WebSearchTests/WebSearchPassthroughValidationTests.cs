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

namespace ASC.AI.Tests.Tests.WebSearchTests;

/// <summary>
/// The body rules of the passthrough routes. The portal checks the fields it documents (<c>engine</c>, <c>query</c>,
/// <c>numResults</c>, <c>urls</c>) before anything is sent upstream, so a refusal is a 400 naming the field and the
/// provider is never dialled.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchPassthroughValidationTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    private const string QueryRequired = "query is required and must be a non-empty string";
    private const string UrlsRequired = "urls is required and must be a non-empty array of strings";
    private const string NumResultsInvalid = "numResults must be a positive integer";
    private const string EngineInvalid = "engine must be a string";

    public static TheoryData<string, string, string> RefusedBodies => new()
    {
        { "search", "{}", QueryRequired },
        { "search", "null", QueryRequired },
        { "search", "[]", QueryRequired },
        { "search", "\"hello\"", QueryRequired },
        { "search", """{"engine":"exa"}""", QueryRequired },
        { "search", """{"engine":"exa","query":""}""", QueryRequired },
        { "search", """{"engine":"exa","query":"   "}""", QueryRequired },
        { "search", """{"engine":"exa","query":"x","numResults":0}""", NumResultsInvalid },
        { "search", """{"engine":"exa","query":"x","numResults":-1}""", NumResultsInvalid },
        { "search", """{"engine":"exa","query":"x","numResults":1.5}""", NumResultsInvalid },
        { "contents", "{}", UrlsRequired },
        { "contents", """{"engine":"exa"}""", UrlsRequired },
        { "contents", """{"engine":"exa","urls":[]}""", UrlsRequired },
        { "contents", """{"engine":"exa","urls":[5]}""", UrlsRequired },
        { "contents", """{"engine":"exa","urls":[""]}""", UrlsRequired },
        { "contents", """{"engine":1,"urls":["https://example.com"]}""", EngineInvalid }
    };

    /// <summary>
    /// The gateway reads the body into a typed struct, so a field of the wrong JSON type used to come back as a 500
    /// that named its Go structs. The portal now refuses it itself, with a 400 naming the field.
    /// </summary>
    public static TheoryData<string, string> WrongTypedSearchBodies => new()
    {
        { """{"engine":1,"query":"x"}""", EngineInvalid },
        { """{"engine":"exa","query":5}""", QueryRequired },
        { """{"engine":"exa","query":{}}""", QueryRequired },
        { """{"engine":"exa","query":"x","numResults":"x"}""", NumResultsInvalid }
    };

    [Theory]
    [MemberData(nameof(RefusedBodies))]
    public async Task RefusedBody_Returns400NamingTheFieldAndDialsNothing(string route, string body, string message)
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);

        var reply = await PassthroughAsync(route, body);

        reply.Status.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(message);
        reply.Json.GetProperty("field").GetString().Should().NotBeNullOrEmpty();
        stub.CallsTo(WebSearchProviderStub.SearchPath).Concat(stub.CallsTo(WebSearchProviderStub.ContentsPath))
            .Should().BeEmpty(stub.Describe());
    }

    [Theory]
    [MemberData(nameof(WrongTypedSearchBodies))]
    [Trait("Bug", "84433")]
    public async Task Search_AFieldOfTheWrongType_Returns400NotA500(string body, string message)
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        var control = await PassthroughAsync("search", """{"engine":"exa"}""");

        var reply = await PassthroughAsync("search", body);

        control.Status.Should().Be(400, "a missing query is a clean 400: " + control);
        reply.Status.Should().Be(400, "a wrongly typed field is a 400, got " + reply);
        reply.Error.Should().Be(message);
        reply.Text.Should().NotContain("Go struct", "the answer must not leak internals");
    }

    [Fact]
    [Trait("Bug", "84434")]
    public async Task Contents_UrlsOfTheWrongType_Returns400NotA500()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        var control = await PassthroughAsync("contents", """{"engine":"exa"}""");

        var reply = await PassthroughAsync("contents", """{"engine":"exa","urls":"https://example.com"}""");

        control.Status.Should().Be(400, "a missing urls is a clean 400: " + control);
        reply.Status.Should().Be(400, $"urls as a string answered {reply}");
        reply.Error.Should().Be(UrlsRequired);
    }

    /// <summary>
    /// A body with a good <c>query</c> and no <c>engine</c> at all is not refused by the portal: which engines exist
    /// is the provider's rule, so the body goes on as it came.
    /// </summary>
    [Fact]
    public async Task Search_WithoutAnEngine_IsForwardedAsItCame()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);

        var reply = await PassthroughAsync("search", """{"query":"x"}""");

        reply.Status.Should().Be(200, reply.ToString());
        var calls = stub.CallsTo(WebSearchProviderStub.SearchPath);
        calls.Should().ContainSingle(stub.Describe());
        using var document = JsonDocument.Parse(calls[0].Body);
        document.RootElement.TryGetProperty("engine", out _).Should().BeFalse();
    }
}
