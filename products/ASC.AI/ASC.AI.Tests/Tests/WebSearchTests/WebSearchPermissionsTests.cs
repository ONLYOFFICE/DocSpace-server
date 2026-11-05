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
/// Who may use the web-search routes: Anonymous is 401, a Guest is refused everywhere, DocSpaceAdmin, RoomAdmin and
/// User read, and only a DocSpaceAdmin may change what is stored. The portal AI switch closes the read side and
/// <c>clear</c>. The Guest and AI-switch refusals of the write routes and of the probe are in the suites of those
/// routes, next to the bodies they send.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchPermissionsTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    public static TheoryData<string> Routes => new() { "is-configured", "get-active-config", "test-connection", "configure", "set-active-config", "clear" };

    public static TheoryData<EmployeeType, bool> ReadRoles => new()
    {
        { EmployeeType.DocSpaceAdmin, true },
        { EmployeeType.RoomAdmin, true },
        { EmployeeType.User, true },
        { EmployeeType.Guest, false }
    };

    private Task<Reply> CallRouteAsync(string route) => route switch
    {
        "is-configured" => IsConfiguredAsync(),
        "get-active-config" => GetActiveConfigAsync(),
        "test-connection" => TestConnectionAsync("""{"provider":"onlyoffice"}"""),
        "configure" => ConfigureAsync("""{"config":{"provider":"onlyoffice"}}"""),
        "set-active-config" => SetActiveAsync("""{"config":{"provider":"onlyoffice"}}"""),
        _ => ClearAsync()
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Anonymous_Returns401(string route)
    {
        await ActAsAnonymousAsync();

        var reply = await CallRouteAsync(route);

        reply.Status.Should().Be(401, reply.ToString());
    }

    [Theory]
    [MemberData(nameof(ReadRoles))]
    public async Task ReadRoutes_AreOpenToEveryRoleButGuest(EmployeeType type, bool canRead)
    {
        await ActAsAsync(type);

        var configured = await IsConfiguredAsync();
        var active = await GetActiveConfigAsync();

        if (!canRead)
        {
            new { configured = configured.Status, active = active.Status }.Should().BeEquivalentTo(new { configured = 403, active = 403 });

            return;
        }

        configured.ToString().Should().Be("200: false");
        active.ToString().Should().Be("200: null");
    }

    /// <summary>Nothing can be destroyed on an unconfigured portal, so <c>clear</c> is as open as the read side.</summary>
    [Theory]
    [MemberData(nameof(ReadRoles))]
    public async Task Clear_Unconfigured_FollowsTheReadRule(EmployeeType type, bool canRead)
    {
        await ActAsAsync(type);

        var reply = await ClearAsync();

        reply.Status.Should().Be(canRead ? 200 : 403, reply.ToString());
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task Clear_StoredConfig_IsRefusedToNonAdministratorsAndSurvives(EmployeeType type)
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        await ActAsAsync(type);

        var reply = await ClearAsync();

        reply.Status.Should().Be(403, reply.ToString());
        await ExpectStoredAsync(StubProvider);
    }

    [Fact]
    public async Task Clear_StoredConfig_IsAllowedToADocSpaceAdmin()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        await ActAsAsync(EmployeeType.DocSpaceAdmin);

        var reply = await ClearAsync();

        reply.Status.Should().Be(200, reply.ToString());
        await ExpectNothingStoredAsync();
    }

    /// <summary>
    /// With AI switched off the read side and <c>clear</c> answer 403, and switching it back on restores the
    /// configuration: the switch hides it, it does not drop it.
    /// </summary>
    [Fact]
    public async Task AiDisabled_ReadSideAndClearReturn403_AndTheConfigSurvives()
    {
        using var stub = new WebSearchProviderStub();
        await StoreStubAsync(stub);
        await SetPortalAiAccessAsync(false);

        var configured = await IsConfiguredAsync();
        var active = await GetActiveConfigAsync();
        var cleared = await ClearAsync();

        new { configured = configured.Status, active = active.Status, cleared = cleared.Status }
            .Should().BeEquivalentTo(new { configured = 403, active = 403, cleared = 403 });

        await SetPortalAiAccessAsync(true);
        await ExpectStoredAsync(StubProvider);
    }

    /// <summary>
    /// A key probe is an outbound request on the portal's behalf, so who may make one is a real permission question:
    /// every role that can read the configuration may also probe with it.
    /// </summary>
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task TestConnection_EveryNonGuestRole_MayProbe(EmployeeType type)
    {
        using var stub = new WebSearchProviderStub();
        await ActAsAsync(type);

        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", StubKey, stub.BaseUrl));

        reply.Status.Should().Be(200, reply.ToString());
        reply.IsTrue.Should().BeTrue(reply.ToString());
    }

    /// <summary>A Guest cannot read the configuration, so the portal must not let them make it dial a host either.</summary>
    [Fact]
    [Trait("Bug", "83234")]
    public async Task TestConnection_Guest_IsRefusedWith403AndNothingIsDialled()
    {
        using var stub = new WebSearchProviderStub();
        await ActAsAsync(EmployeeType.Guest);

        var control = await GetActiveConfigAsync();
        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", StubKey, stub.BaseUrl));

        control.Status.Should().Be(403, "the Guest is refused on the read side");
        reply.Status.Should().Be(403, $"a Guest must not reach the web-search provider; got {reply}");
        stub.CallsTo(WebSearchProviderStub.SearchPath).Should().BeEmpty(stub.Describe());
    }

    /// <summary>
    /// The portal AI switch is checked on the read side; the probe is an outbound call the portal makes on its own
    /// behalf and must be gated the same way.
    /// </summary>
    [Fact]
    [Trait("Bug", "83235")]
    public async Task TestConnection_AiDisabled_IsRefusedWith403AndNothingIsDialled()
    {
        using var stub = new WebSearchProviderStub();
        await SetPortalAiAccessAsync(false);

        var control = await IsConfiguredAsync();
        var reply = await TestConnectionAsync(ProbeJson("onlyoffice", StubKey, stub.BaseUrl));

        control.Status.Should().Be(403, "the switch took effect on the read side");
        reply.Status.Should().Be(403, $"test-connection answered {reply} with AI access disabled");
        stub.CallsTo(WebSearchProviderStub.SearchPath).Should().BeEmpty(stub.Describe());
    }
}
