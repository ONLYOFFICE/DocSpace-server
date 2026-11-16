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
/// <c>web-search/is-configured</c>, <c>get-active-config</c> and <c>clear</c>: the state a portal has, in the portal
/// scope and in the scope of an AI room, a room or a folder, and how a bad <c>entityId</c> is answered.
/// </summary>
/// <remarks>
/// The TypeScript suite reads the state the AI search add-on leaves behind (<c>setAiSearchAddon</c>, a billing
/// call); a standalone portal has no add-on, so the configured state is the one an administrator stores. The store
/// is one per portal: the chat service ignores the scope the config is stored in, which is why a scoped read of a
/// stored config answers the portal-wide one and why no "a room keeps its own provider" case exists.
/// </remarks>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchStateTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    [Fact]
    public async Task FreshPortal_HasNoProvider()
    {
        var configured = await SdkWebSearch.AiWebSearchIsConfiguredAsync(cancellationToken: TestContext.Current.CancellationToken);
        var active = await GetActiveConfigAsync();

        configured.Should().BeFalse();
        active.ToString().Should().Be("200: null");
    }

    [Theory]
    [MemberData(nameof(ScopeKinds))]
    public async Task UnconfiguredPortal_EveryScopeIsUnconfiguredToo(string kind)
    {
        var entityId = await CreateScopeAsync(kind);

        await ExpectNothingStoredAsync(entityId);
    }

    [Theory]
    [MemberData(nameof(ScopeKinds))]
    public async Task StoredConfig_IsReportedPortalWideAndInEveryScope(string kind)
    {
        using var stub = new WebSearchProviderStub();
        var entityId = await CreateScopeAsync(kind);
        await ActAsOwnerAsync();
        await StoreStubAsync(stub);

        var portal = await GetActiveConfigAsync();
        var configured = await IsConfiguredAsync(entityId);
        var active = await GetActiveConfigAsync(entityId);

        portal.Str("provider").Should().Be(StubProvider, portal.ToString());
        configured.ToString().Should().Be("200: true");
        active.Status.Should().Be(200, active.ToString());
        active.Str("provider").Should().Be(StubProvider, active.ToString());
        active.Str("baseUrl").Should().Be(stub.BaseUrl, active.ToString());
    }

    [Fact]
    public async Task Clear_Unconfigured_IsAcceptedAnyNumberOfTimes()
    {
        var first = await ClearAsync();
        var second = await ClearAsync();

        new[] { first.Status, second.Status }.Should().Equal(200, 200);
        first.Text.Should().Contain("\"success\":true");
        second.Text.Should().Contain("\"success\":true");
        await ExpectNothingStoredAsync();
    }

    [Fact]
    public async Task Clear_StoredConfig_RemovesIt()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        await ExpectStoredAsync(StubProvider);

        var cleared = await ClearAsync();

        cleared.Status.Should().Be(200, cleared.ToString());
        cleared.Text.Should().Contain("\"success\":true");
        await ExpectNothingStoredAsync();
    }

    /// <summary>
    /// A body that names a room, or an entity that cannot exist, does not make the clear fail: the state of an
    /// unconfigured portal is the same afterwards.
    /// </summary>
    [Theory]
    [InlineData("room")]
    [InlineData(MissingEntityId)]
    [InlineData("abc")]
    public async Task Clear_Unconfigured_WithAnEntityBody_IsAcceptedAsNoOp(string entity)
    {
        var entityId = entity == "room" ? (await CreateCustomRoomAsync()).ToString() : entity;
        await ActAsOwnerAsync();

        var cleared = await ClearAsync(JsonSerializer.Serialize(new { entityId }));

        cleared.Status.Should().Be(200, cleared.ToString());
        cleared.Text.Should().Contain("\"success\":true");
        await ExpectNothingStoredAsync();
    }

    /// <summary>
    /// Only an entirely empty <c>entityId</c> gets the soft "unconfigured" reading; anything else that does not
    /// resolve to an entity the caller can open — a number or not, 0 included — is a 404 naming it.
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData(MissingEntityId)]
    [InlineData("abc")]
    public async Task Reads_UnresolvableEntityId_Return404(string entityId)
    {
        var configured = await IsConfiguredAsync(entityId);
        var active = await GetActiveConfigAsync(entityId);

        new { configured = configured.Status, active = active.Status }.Should().BeEquivalentTo(new { configured = 404, active = 404 });
        configured.Error.Should().Be($"Entity \"{entityId}\" not found");
        active.Error.Should().Be($"Entity \"{entityId}\" not found");
    }

    [Fact]
    public async Task Reads_EmptyEntityId_AreAnsweredAsUnconfigured()
    {
        var configured = await IsConfiguredAsync(string.Empty);
        var active = await GetActiveConfigAsync(string.Empty);

        configured.ToString().Should().Be("200: false");
        active.ToString().Should().Be("200: null");
    }

    /// <summary>
    /// <c>entityId</c> names a location the caller chooses, so it is checked against what they can open: a User who
    /// is no member of a room is refused that room's scope. The caller's own portal-wide scope answers, so the 403 is
    /// about the room and not about the role.
    /// </summary>
    [Fact]
    [Trait("Bug", "82901")]
    public async Task Reads_ARoomTheCallerCannotOpen_Return403()
    {
        var roomId = await CreateCustomRoomAsync("Autotest Private Room");
        await ActAsAsync(EmployeeType.User);

        var own = await IsConfiguredAsync();
        var configured = await IsConfiguredAsync(roomId.ToString());
        var active = await GetActiveConfigAsync(roomId.ToString());

        own.Status.Should().Be(200, "the caller's own scope is readable");
        new { configured = configured.Status, active = active.Status }.Should().BeEquivalentTo(new { configured = 403, active = 403 });
    }

    /// <summary>
    /// The configuration is readable by every non-guest role, so what it returns must not carry the provider's key.
    /// </summary>
    /// <remarks>
    /// The chat service answers <c>get-active-config</c> with the stored record as it is, key included, and the
    /// store keeps it in clear. A User who reads the configuration thereby reads the key an administrator entered.
    /// Not filed; the TypeScript header expects "the stored key comes back masked".
    /// </remarks>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task GetActiveConfig_AsAUser_DoesNotReturnTheKey()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        await ActAsAsync(EmployeeType.User);

        var active = await GetActiveConfigAsync();

        active.Status.Should().Be(200, active.ToString());
        active.Text.Should().NotContain(StubKey, "a plain member must not read the provider's key");
    }

    [Fact]
    public async Task Reads_RepeatedAndParallel_AgreeAndNeverChangeTheState()
    {
        using var stub = new WebSearchProviderStub();
        var off = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => IsConfiguredAsync()));
        await StoreStubAsync(stub);
        var on = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => IsConfiguredAsync()));
        var active = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => GetActiveConfigAsync()));

        off.Select(r => r.ToString()).Distinct().Should().Equal("200: false");
        on.Select(r => r.ToString()).Distinct().Should().Equal("200: true");
        active.Select(r => r.ToString()).Distinct().Should().ContainSingle("every read answers the same config");
        await ExpectStoredAsync(StubProvider);
    }
}
