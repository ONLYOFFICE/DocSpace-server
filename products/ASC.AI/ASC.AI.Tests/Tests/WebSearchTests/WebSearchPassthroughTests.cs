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
/// <c>POST ai/websearch/v1/search</c> and <c>/contents</c>, the routes the document editor's AI plugin uses: the plugin
/// holds only a placeholder configuration, the portal resolves the active provider and key itself, forwards the body
/// and relays the provider's status, body and content type.
/// </summary>
/// <remarks>
/// The TypeScript suite runs them against the ONLYOFFICE gateway and bills real searches; here the provider is
/// <see cref="WebSearchProviderStub"/>. What the gateway validates (<c>engine is required</c>, <c>unknown engine</c>),
/// what it bills and what the real provider returns for a page it cannot fetch are the gateway's and are not ported.
/// </remarks>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchPassthroughTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    private const string NotConfigured = "Web search is not configured";

    public static TheoryData<string> Routes => new() { "search", "contents" };

    public static TheoryData<int> RelayedClientErrors => new() { 400, 404, 429 };

    public static TheoryData<int> HiddenServerErrors => new() { 500, 503 };

    internal static string ValidBody(string route) => route == "search"
        ? """{"engine":"exa","query":"ONLYOFFICE Docs","numResults":1}"""
        : """{"engine":"exa","urls":["https://example.com"]}""";

    private static string PathOf(string route) => route == "search" ? WebSearchProviderStub.SearchPath : WebSearchProviderStub.ContentsPath;

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task NoConfiguration_Returns404InThePortalsOwnEnvelope(string route)
    {
        var reply = await PassthroughAsync(route, ValidBody(route));

        reply.Status.Should().Be(404, reply.ToString());
        reply.Error.Should().Be(NotConfigured);
    }

    [Fact]
    public async Task ClearingTheConfig_TakesBothRoutesBackTo404()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        var before = await PassthroughAsync("search", ValidBody("search"));
        await ClearAsync();

        var search = await PassthroughAsync("search", ValidBody("search"));
        var contents = await PassthroughAsync("contents", ValidBody("contents"));

        before.Status.Should().Be(200, "the config was in effect: " + before);
        new { search = search.Status, contents = contents.Status }.Should().BeEquivalentTo(new { search = 404, contents = 404 });
        search.Error.Should().Be(NotConfigured);
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Anonymous_Returns401_EvenOnAConfiguredPortal(string route)
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        await ActAsAnonymousAsync();

        var reply = await PassthroughAsync(route, ValidBody(route));

        reply.Status.Should().Be(401, reply.ToString());
    }

    [Fact]
    public async Task Search_RelaysTheProvidersAnswerAndSendsItTheQueryAndTheKey()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        // Cyrillic on purpose: the body is forwarded as it came, so a query mangled on the way would show up here.
        const string query = "редактор документов ONLYOFFICE";

        var reply = await PassthroughAsync("search", JsonSerializer.Serialize(new { engine = "exa", query, numResults = 2 }));

        reply.Status.Should().Be(200, reply.ToString());
        reply.ContentType.Should().Be("application/json");
        reply.Str("requestId").Should().Be("stub-search-1");
        reply.Json.GetProperty("results").GetArrayLength().Should().Be(1);
        reply.Text.Should().NotContain(StubKey, "the key never goes back to the page");
        var calls = stub.CallsTo(WebSearchProviderStub.SearchPath);
        calls.Should().ContainSingle(stub.Describe());
        calls[0].Authorization.Should().Be($"Bearer {StubKey}");
        using var document = JsonDocument.Parse(calls[0].Body);
        var sent = document.RootElement;
        sent.GetProperty("query").GetString().Should().Be(query);
        sent.GetProperty("numResults").GetInt32().Should().Be(2);
        sent.TryGetProperty("metadata", out _).Should().BeFalse("no entry was named");
    }

    [Fact]
    public async Task Contents_RelaysTheProvidersAnswerAndSendsItTheUrls()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);

        var reply = await PassthroughAsync("contents", """{"engine":"exa","urls":["https://example.com/a","https://example.com/b"]}""");

        reply.Status.Should().Be(200, reply.ToString());
        reply.ContentType.Should().Be("application/json");
        reply.Str("requestId").Should().Be("stub-contents-1");
        var calls = stub.CallsTo(WebSearchProviderStub.ContentsPath);
        calls.Should().ContainSingle(stub.Describe());
        using var document = JsonDocument.Parse(calls[0].Body);
        document.RootElement.GetProperty("urls").EnumerateArray()
            .Select(u => u.GetString()).Should().Equal("https://example.com/a", "https://example.com/b");
    }

    /// <summary>The plugin treats any non-2xx as an error payload, so a provider's 4xx passes through with its status and body.</summary>
    [Theory]
    [MemberData(nameof(RelayedClientErrors))]
    public async Task ProviderClientError_IsRelayedWithItsStatusAndBody(int status)
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        stub.RespondTo(WebSearchProviderStub.SearchPath, status, """{"error":{"code":"provider_says_no"}}""");

        var reply = await PassthroughAsync("search", ValidBody("search"));

        reply.Status.Should().Be(status, reply.ToString());
        reply.Text.Should().Contain("provider_says_no");
    }

    /// <summary>A provider 5xx carries internals (a stack trace, type names): it is answered with a fixed 502 and never relayed.</summary>
    [Theory]
    [MemberData(nameof(HiddenServerErrors))]
    public async Task ProviderServerError_IsAnswered502WithoutItsBody(int status)
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        stub.RespondTo(WebSearchProviderStub.ContentsPath, status, "panic: runtime error in SearchRequest.query of type string");

        var reply = await PassthroughAsync("contents", ValidBody("contents"));

        reply.Status.Should().Be(502, reply.ToString());
        reply.Error.Should().Be("Upstream request failed");
        reply.Text.Should().NotContain("panic").And.NotContain("SearchRequest");
    }

    [Fact]
    public async Task UnreachableProvider_IsAnswered502WithoutTheProviderUrl()
    {
        var stored = await SetActiveAsync(StubProvider, StubKey, WebSearchProviderStub.DeadBaseUrl);
        stored.Status.Should().Be(200, stored.ToString());

        var reply = await PassthroughAsync("search", ValidBody("search"));

        reply.Status.Should().Be(502, reply.ToString());
        reply.Error.Should().Be("Upstream request failed");
        reply.Text.Should().NotContain("127.0.0.1");
    }

    [Fact]
    public async Task EverySearchGoesToTheProvidersOwnPath()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);

        await PassthroughAsync("search", ValidBody("search"));
        await PassthroughAsync("contents", ValidBody("contents"));

        new[] { PathOf("search"), PathOf("contents") }
            .Select(path => stub.CallsTo(path).Count)
            .Should().Equal(1, 1);
    }
}
