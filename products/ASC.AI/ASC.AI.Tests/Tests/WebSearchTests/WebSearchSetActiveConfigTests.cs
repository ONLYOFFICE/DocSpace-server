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
/// <c>PUT web-search/set-active-config</c>: stores a configuration without probing the provider. In the
/// TypeScript suite every well-formed request is refused with 403 because the add-on owns the provider; a standalone
/// portal has no add-on, so a DocSpace administrator really stores it (and the other roles are refused by the AI
/// service's write rule).
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchSetActiveConfigTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    public static TheoryData<string, string> MalformedConfigs => new()
    {
        { "{}", ConfigRequired },
        { """{"config":null}""", ConfigRequired },
        { """{"config":"x"}""", ConfigRequired },
        { """{"config":[]}""", ConfigRequired },
        { """{"config":{}}""", ProviderRequired },
        { """{"config":{"provider":""}}""", ProviderRequired },
        { """{"config":{"provider":5}}""", ProviderRequired },
        { """{"config":{"provider":"stub-search","key":5}}""", "config.key must be a string" },
        { """{"config":{"provider":"stub-search","baseUrl":5}}""", "config.baseUrl must be a string" },
        { """{"config":{"provider":"stub-search","isCloudProvider":"yes"}}""", "config.isCloudProvider must be a boolean" },
        { """{"config":{"provider":"stub-search","headers":"x"}}""", "config.headers must be an object" }
    };

    [Fact]
    public async Task WellFormedConfig_IsStoredAndReadBack()
    {
        using var stub = new WebSearchProviderStub();

        await StoreStubAsync(stub);

        var active = await SdkWebSearch.AiWebSearchGetActiveConfigAsync(cancellationToken: TestContext.Current.CancellationToken);
        active.Provider.Should().Be(StubProvider);
        active.BaseUrl.Should().Be(stub.BaseUrl);
        (await SdkWebSearch.AiWebSearchIsConfiguredAsync(cancellationToken: TestContext.Current.CancellationToken)).Should().BeTrue();
    }

    [Fact]
    public async Task SecondConfig_ReplacesTheFirst()
    {
        using var first = new WebSearchProviderStub();
        using var second = new WebSearchProviderStub();
        await StoreStubAsync(first, provider: "stub-first");

        await StoreStubAsync(second, provider: "stub-second");

        var active = await GetActiveConfigAsync();
        active.Str("provider").Should().Be("stub-second", active.ToString());
        active.Str("baseUrl").Should().Be(second.BaseUrl, active.ToString());
    }

    /// <summary>
    /// The body is validated before the caller's right to write is looked at, so what a malformed one gets is a 400
    /// naming the field (the order problem for a Guest and an AI-disabled portal is the Bug 84431 / 84432 pair).
    /// </summary>
    [Theory]
    [MemberData(nameof(MalformedConfigs))]
    public async Task MalformedConfig_Returns400NamingTheField(string body, string message)
    {
        var reply = await SetActiveAsync(body);

        reply.Status.Should().Be(400, reply.ToString());
        reply.Error.Should().Be(message);
        await ExpectNothingStoredAsync();
    }

    [Theory]
    [InlineData("999999999")]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task UnresolvableEntityId_Returns404AndStoresNothing(string entityId)
    {
        var reply = await SetActiveAsync(StubProvider, "k", null, entityId);

        reply.Status.Should().Be(404, reply.ToString());
        reply.Error.Should().Be($"Entity \"{entityId}\" not found");
        await ExpectNothingStoredAsync();
    }

    [Fact]
    public async Task DocSpaceAdmin_CanStoreAConfiguration()
    {
        using var stub = new WebSearchProviderStub();
        await ActAsAsync(EmployeeType.DocSpaceAdmin);

        await StoreStubAsync(stub);

        await ExpectStoredAsync(StubProvider);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task OtherRoles_AreRefusedWith403AndStoreNothing(EmployeeType type)
    {
        using var stub = new WebSearchProviderStub();
        await ActAsAsync(type);

        var reply = await SetActiveAsync(StubProvider, StubKey, stub.BaseUrl);

        reply.Status.Should().Be(403, reply.ToString());
        await ExpectNothingStoredAsync();
    }

    /// <summary>
    /// A Guest is refused on the read side and for a valid body, so the rule exists; a malformed body and a
    /// loopback base URL must meet the same 403 before the body is judged.
    /// </summary>
    [Fact]
    [Trait("Bug", "84431")]
    public async Task Guest_IsRefusedWith403BeforeTheBodyIsValidated()
    {
        var guest = await InviteGuest(TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(guest);

        var control = await GetActiveConfigAsync();
        var valid = await SetActiveAsync(StubProvider, "k", null);
        var malformed = await SetActiveAsync("{}");
        var badHost = await SetActiveAsync("onlyoffice", "k", "http://0.0.0.0:1/");

        await ExpectNothingStoredAsync();
        control.Status.Should().Be(403, "the Guest is refused on the read side");
        valid.Status.Should().Be(403, "the Guest is refused for a valid body");
        new[] { malformed.Status, badHost.Status }.Should().Equal(
            new[] { 403, 403 },
            "a Guest must be refused with 403 before the body is ever validated");
    }

    [Fact]
    [Trait("Bug", "84432")]
    public async Task AiDisabled_MalformedBodyIsRefusedWith403BeforeValidation()
    {
        await SetPortalAiAccessAsync(false);

        var control = await GetActiveConfigAsync();
        var valid = await SetActiveAsync(StubProvider, "k", null);
        var malformed = await SetActiveAsync("{}");

        control.Status.Should().Be(403, "the switch took effect on the read side");
        valid.Status.Should().Be(403, "a valid body is refused while AI is off");
        malformed.Status.Should().Be(
            403,
            "set-active-config must be refused with 403 when AI access is disabled, before the body is parsed");
    }
}
