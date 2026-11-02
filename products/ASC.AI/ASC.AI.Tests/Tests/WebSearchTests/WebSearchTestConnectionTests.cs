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
/// <c>POST web-search/test-connection</c>: the key probe. The answer is a bare <c>true</c>, or a
/// <c>{field, message}</c> pair under HTTP 200. Everything the probe dials is the stub provider; the TypeScript
/// cases that send an Exa key to the real <c>api.exa.ai</c> are not ported.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchTestConnectionTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    /// <summary>Provider names the TypeScript suite and the client send that the portal does not know.</summary>
    public static TheoryData<string> UnknownProviders => new() { "tavily", "brave", "bing", "google", "serper", "duckduckgo" };

    public static TheoryData<int, string?, string, string> ProviderRefusals => new()
    {
        { 401, StubKey, "key", "Invalid API key" },
        { 404, StubKey, "url", "Invalid URL" },
        { 500, StubKey, "key", "Request failed with status 500" },
        { 403, StubKey, "key", "Request failed with status 403" },
        { 500, null, "key", "Empty key" }
    };

    [Theory]
    [InlineData("""{"provider":""}""")]
    [InlineData("""{"key":"k"}""")]
    [InlineData("{}")]
    public async Task MissingProvider_IsReportedAsRequired(string body)
    {
        var reply = await TestConnectionAsync(body);

        reply.Status.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(ProviderRequired);
    }

    [Fact]
    public async Task OnlyofficeProvider_DialsTheBaseUrlAndAnswersTrue()
    {
        using var stub = new WebSearchProviderStub();

        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", StubKey, stub.BaseUrl));

        reply.Status.Should().Be(200, reply.ToString());
        reply.IsTrue.Should().BeTrue(reply.ToString());
        var calls = stub.CallsTo(WebSearchProviderStub.SearchPath);
        calls.Should().ContainSingle(stub.Describe());
        calls[0].Authorization.Should().Be($"Bearer {StubKey}");
        var body = JsonDocument.Parse(calls[0].Body).RootElement;
        body.GetProperty("engine").GetString().Should().Be("exa");
        body.GetProperty("numResults").GetInt32().Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(ProviderRefusals))]
    public async Task ProviderRefusal_IsReportedWithTheFieldItBelongsTo(int status, string? key, string field, string message)
    {
        using var stub = new WebSearchProviderStub();
        stub.RespondTo(WebSearchProviderStub.SearchPath, status, """{"error":"refused"}""");

        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", key, stub.BaseUrl));

        reply.Status.Should().Be(200, reply.ToString());
        reply.Str("field").Should().Be(field, reply.ToString());
        reply.Str("message").Should().Be(message, reply.ToString());
    }

    [Fact]
    public async Task UnreachableBaseUrl_IsReportedAsAConnectionFailure()
    {
        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", "k", WebSearchProviderStub.DeadBaseUrl));

        reply.Status.Should().Be(200, reply.ToString());
        reply.Str("field").Should().Be("url", reply.ToString());
        reply.Str("message").Should().Be("Failed to connect", reply.ToString());
    }

    [Fact]
    public async Task OnlyofficeWithoutABaseUrl_AsksForIt()
    {
        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", StubKey, null));

        reply.Status.Should().Be(200, reply.ToString());
        reply.Str("field").Should().Be("url", reply.ToString());
        reply.Str("message").Should().Be("Base URL is required for cloud provider", reply.ToString());
    }

    /// <summary>
    /// Exa and ONLYOFFICE are the two names the portal understands; the name match tolerates case and padding. Only
    /// the ONLYOFFICE half can be checked against a stub (Exa is dialled at its fixed public address).
    /// </summary>
    [Theory]
    [InlineData("onlyoffice")]
    [InlineData("ONLYOFFICE")]
    [InlineData(" onlyoffice ")]
    [Trait("Bug", "82811")]
    public async Task OnlyofficeProvider_IsRecognisedWhateverItsSpelling(string provider)
    {
        using var stub = new WebSearchProviderStub();

        var reply = await TestConnectionAsync(ProbeJson(provider, StubKey, stub.BaseUrl));

        reply.IsTrue.Should().BeTrue(reply.ToString());
    }

    [Theory]
    [MemberData(nameof(UnknownProviders))]
    [Trait("Bug", "82811")]
    public async Task UnknownProvider_IsNamedInTheAnswer(string provider)
    {
        var reply = await TestConnectionAsync(ProbeJson(provider, StubKey, null));

        reply.Status.Should().Be(200, reply.ToString());
        reply.Str("field").Should().Be("name", reply.ToString());
        reply.Str("message").Should().Be($"Unknown web-search provider: {provider}", reply.ToString());
    }

    [Fact]
    public async Task AProbe_IsNotASave()
    {
        using var stub = new WebSearchProviderStub();

        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", StubKey, stub.BaseUrl));

        reply.IsTrue.Should().BeTrue(reply.ToString());
        await ExpectNothingStoredAsync();
    }
}
