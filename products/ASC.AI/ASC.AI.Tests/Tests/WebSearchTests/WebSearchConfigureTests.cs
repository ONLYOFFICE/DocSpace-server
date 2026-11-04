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
/// <c>PUT web-search/configure</c>: probes the provider first and stores the config only when the probe passes.
/// The TypeScript suite can only watch it refuse (the add-on owns the provider there); against a stub provider the
/// route really stores, and the body, role and AI-switch rules of the TypeScript bug tests are checked on it.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchConfigureTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    [Fact]
    public async Task ValidConfig_IsProbedAtTheProviderAndStored()
    {
        using var stub = new WebSearchProviderStub();

        var result = await SdkWebSearch.AiWebSearchConfigureAsync(
            new AiWebSearchConfigureRequest(new AiWebSearchConfig("onlyoffice", StubKey, stub.BaseUrl)),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        var probes = stub.CallsTo(WebSearchProviderStub.SearchPath);
        probes.Should().ContainSingle(stub.Describe());
        probes[0].Authorization.Should().Be($"Bearer {StubKey}");
        await ExpectStoredAsync("onlyoffice");
    }

    [Fact]
    public async Task WrongKey_Returns400FromTheProbeAndStoresNothing()
    {
        using var stub = new WebSearchProviderStub();
        stub.RespondTo(WebSearchProviderStub.SearchPath, 401, """{"error":"bad key"}""");

        var reply = await ConfigureAsync("onlyoffice", "not-a-real-key", stub.BaseUrl);

        reply.Status.Should().Be(400, reply.ToString());
        reply.Json.GetProperty("success").GetBoolean().Should().BeFalse();
        reply.Json.GetProperty("error").GetProperty("field").GetString().Should().Be("key");
        reply.Json.GetProperty("error").GetProperty("message").GetString().Should().Be("Invalid API key");
        reply.Text.Should().NotContain("not-a-real-key", "the key that was sent is not echoed back in the error");
        await ExpectNothingStoredAsync();
    }

    [Fact]
    public async Task UnreachableProvider_Returns400AndStoresNothing()
    {
        var reply = await ConfigureAsync("onlyoffice", "k", WebSearchProviderStub.DeadBaseUrl);

        reply.Status.Should().Be(400, reply.ToString());
        reply.Json.GetProperty("error").GetProperty("field").GetString().Should().Be("url");
        await ExpectNothingStoredAsync();
    }

    /// <summary>
    /// A structurally broken body used to crash the route with a 500 (an unwrapped config, an empty body); it is a
    /// client error.
    /// </summary>
    [Theory]
    [InlineData("""{"provider":"onlyoffice","key":"k"}""")]
    [InlineData("{}")]
    [Trait("Bug", "82812")]
    public async Task MalformedBody_Returns400InsteadOfCrashing(string body)
    {
        var reply = await ConfigureAsync(body);

        reply.Status.Should().Be(400, reply.ToString());
        await ExpectNothingStoredAsync();
    }

    /// <summary>
    /// A well-formed config that fails a business rule answered HTTP 200 with <c>success:false</c>; every other
    /// refusal of this route is a 4xx, so it is a 400.
    /// </summary>
    [Theory]
    [InlineData("""{"config":{"provider":"onlyoffice","key":"k","isCloudProvider":true}}""")]
    [InlineData("""{"config":{"provider":"onlyoffice"}}""")]
    [InlineData("""{"config":{}}""")]
    [Trait("Bug", "83994")]
    public async Task BusinessInvalidConfig_Returns400NotA200WithSuccessFalse(string body)
    {
        var reply = await ConfigureAsync(body);

        reply.Status.Should().Be(400, reply.ToString());
        await ExpectNothingStoredAsync();
    }

    [Fact]
    public async Task DocSpaceAdmin_CanConfigure()
    {
        using var stub = new WebSearchProviderStub();
        await ActAsAsync(EmployeeType.DocSpaceAdmin);

        var reply = await ConfigureAsync("onlyoffice", StubKey, stub.BaseUrl);

        reply.Status.Should().Be(200, reply.ToString());
        await ExpectStoredAsync("onlyoffice");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task OtherRoles_AreRefusedWith403AndStoreNothing(EmployeeType type)
    {
        using var stub = new WebSearchProviderStub();
        await ActAsAsync(type);

        var reply = await ConfigureAsync("onlyoffice", StubKey, stub.BaseUrl);

        reply.Status.Should().Be(403, reply.ToString());
        await ExpectNothingStoredAsync();
    }

    /// <summary>
    /// The read side refuses a Guest, so the authorization rule exists; the write side let a well-formed and a
    /// malformed body through to validation instead of rejecting them by role.
    /// </summary>
    [Theory]
    [InlineData("""{"config":{"provider":"onlyoffice","key":"k"}}""")]
    [InlineData("{}")]
    [Trait("Bug", "84312")]
    public async Task Guest_IsRefusedWith403BeforeTheBodyIsValidated(string body)
    {
        await ActAsAsync(EmployeeType.Guest);

        var control = await GetActiveConfigAsync();
        var reply = await ConfigureAsync(body);

        await ExpectNothingStoredAsync();
        control.Status.Should().Be(403, "the Guest is refused on the read side");
        reply.Status.Should().Be(403, "a Guest must be refused with 403 before the body is ever validated");
    }

    [Theory]
    [InlineData("""{"config":{"provider":"onlyoffice","key":"k"}}""")]
    [InlineData("{}")]
    [Trait("Bug", "84313")]
    public async Task AiDisabled_IsRefusedWith403BeforeTheBodyIsValidated(string body)
    {
        await SetPortalAiAccessAsync(false);

        var control = await GetActiveConfigAsync();
        var reply = await ConfigureAsync(body);

        control.Status.Should().Be(403, "the switch took effect on the read side");
        reply.Status.Should().Be(403, "configure must be refused with 403 when AI access is disabled, before the body is validated");
    }
}
