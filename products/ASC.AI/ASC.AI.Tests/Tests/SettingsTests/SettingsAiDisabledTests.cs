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
/// The AI settings with the portal AI switch off, and as a cycle (off, then on again). Each test proves a transition
/// rather than an end state: the route answers first, the state is changed and read back, and only then is the 403 (or
/// the flipped flag) asserted — a bare 403 would also pass if the route were permanently forbidden.
/// <c>GET /ai/config</c> and <c>/ai/config/user</c> are deliberately NOT gated by the switch.
/// </summary>
/// <remarks>
/// The TypeScript suite's second off-state, the unpaid AI Tools wallet service, is replaced by what decides readiness
/// on the standalone stand: a portal with no profile is not AI-ready (see <see cref="SettingsGetTests"/>). The
/// wallet-only cases (enabling the service without a payment, flags flipping with the gateway) are not ported.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Settings")]
public class SettingsAiDisabledTests(AspireAppFixture fixture) : SettingsTestBase(fixture)
{
    [Theory]
    [InlineData(WebSearchIsConfiguredRoute)]
    [InlineData(WebSearchActiveConfigRoute)]
    public async Task WebSearchRead_AiDisabled_Returns403(string route)
    {
        (await ReadWebSearchRouteAsync(route)).Status.Should().Be(HttpStatusCode.OK, "the route is reachable before the flip");

        await SetPortalAiAccessAsync(false);
        var (status, body) = await ReadWebSearchRouteAsync(route);

        status.Should().Be(HttpStatusCode.Forbidden, body);
        body.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task StartVectorizationTask_AiDisabled_Returns403()
    {
        var missingFile = new AiVectorizationStartTaskRequest([999999999]);

        // A missing file id is a 404: enough to show the route is reachable, and past the AI-access gate, before the
        // switch is flipped. The route is not gated by any payment either.
        var before = await OutcomeOfAsync(() => Vectorization.AiVectorizationStartTaskAsync(missingFile, TestContext.Current.CancellationToken));

        await SetPortalAiAccessAsync(false);
        var (status, error) = await OutcomeOfAsync(() => Vectorization.AiVectorizationStartTaskAsync(missingFile, TestContext.Current.CancellationToken));

        before.Status.Should().Be(404, before.Error);
        status.Should().Be(403, error);
        error.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task GetAiSettings_AiDisabled_StaysReadableAndReportsAiAsOff()
    {
        // Run on a ready portal, otherwise the flag is already false and flipping the switch would prove nothing.
        await MakePortalAiReadyAsync();
        (await ReadConfigAsync()).AiReady.Should().BeTrue();

        await SetPortalAiAccessAsync(false);
        var disabled = await ReadConfigAsync();

        disabled.AiReady.Should().BeFalse();
        disabled.VectorizationEnabled.Should().BeFalse();
        disabled.SystemAiEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UserConfig_AiDisabled_StaysUsable()
    {
        // Deliberately pinned: this is a per-user UI preference, not an AI call, and it is not gated by the switch.
        await SetPortalAiAccessAsync(false);
        var target = !await ReadVisibleAsync();

        await WriteVisibleAsync(target);

        (await ReadVisibleAsync()).Should().Be(target);
    }

    [Fact]
    public async Task GetAiSettings_SwitchedBackOn_RestoresTheFlagsOfAReadyPortal()
    {
        await MakePortalAiReadyAsync();
        var ready = await ReadConfigAsync();
        ready.AiReady.Should().BeTrue();

        await SetPortalAiAccessAsync(false);
        (await ReadConfigAsync()).AiReady.Should().BeFalse("the switch really closed");
        await SetPortalAiAccessAsync(true);

        (await ReadConfigAsync()).ToJson().Should().Be(ready.ToJson());
    }

    [Fact]
    public async Task GetAiSettings_SwitchedOnAgain_DoesNotMakeAPortalWithoutAProfileReady()
    {
        // The flags follow the profile as well as the switch: on a portal without one, the switch can be turned off
        // and on without ever reaching `true`. It is a necessary condition, not a sufficient one.
        await SetPortalAiAccessAsync(false);
        await SetPortalAiAccessAsync(true);

        var config = await ReadConfigAsync();

        config.AiReady.Should().BeFalse();
        config.VectorizationEnabled.Should().BeFalse();
        config.SystemAiEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task UserConfig_ChangedWhileAiIsOff_SurvivesSwitchingItBackOn()
    {
        await WriteVisibleAsync(false);
        (await ReadVisibleAsync()).Should().BeFalse();

        await SetPortalAiAccessAsync(false);

        // Written before the cycle, read while it is off: the switch must not have reset it. And a write made while
        // the switch is off must stick.
        var storedWhileOff = await ReadVisibleAsync();
        await WriteVisibleAsync(true);
        var writtenWhileOff = await ReadVisibleAsync();
        await SetPortalAiAccessAsync(true);

        storedWhileOff.Should().BeFalse("stored value, switch off");
        writtenWhileOff.Should().BeTrue("written while the switch was off");
        (await ReadVisibleAsync()).Should().BeTrue("after the switch came back on");
    }

    [Fact]
    public async Task WebSearch_ANewProfile_DoesNotConfigureAProvider()
    {
        // Web-search availability is separate portal configuration: adding a model changes nothing about it.
        (await RawAsync(HttpMethod.Get, WebSearchIsConfiguredRoute)).Body.Trim().Should().Be("false");
        (await ReadActiveConfigAsync()).Body.Trim().Should().Be("null");

        await MakePortalAiReadyAsync();
        (await ReadConfigAsync()).AiReady.Should().BeTrue("the portal really became AI-ready");

        var configured = await RawAsync(HttpMethod.Get, WebSearchIsConfiguredRoute);
        var active = await ReadActiveConfigAsync();

        configured.Status.Should().Be(HttpStatusCode.OK, configured.Body);
        configured.Body.Trim().Should().Be("false");
        active.Status.Should().Be(HttpStatusCode.OK, active.Body);
        active.Body.Trim().Should().Be("null");
    }
}
