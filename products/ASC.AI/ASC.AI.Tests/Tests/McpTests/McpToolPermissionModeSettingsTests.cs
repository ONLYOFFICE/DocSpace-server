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

namespace ASC.AI.Tests.Tests.McpTests;

/// <summary>
/// The tool permission mode setting (<c>preferences/get|set-tool-permission-mode</c>): <c>ask</c>, <c>auto</c> or
/// <c>allow</c>, one per user. It is also readable as a number in the user's own config and the portal config
/// (<c>ai/config</c>), which has to agree with it.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Mcp")]
public class McpToolPermissionModeSettingsTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private Task<AiChatToolPermissionMode> ModeAsync() =>
        _aiPreferences.AiPreferencesGetToolPermissionModeAsync(cancellationToken: TestContext.Current.CancellationToken);

    private async Task SetModeAsync(AiChatToolPermissionMode mode)
    {
        var result = await _aiPreferences.AiPreferencesSetToolPermissionModeAsync(
            new AiPreferencesSetToolPermissionModeRequest(mode),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    private async Task<AiToolPermissionMode?> UserConfigModeAsync() =>
        (await _aiSettings.AiSettingsGetUserAsync(TestContext.Current.CancellationToken)).Response.ToolPermissionMode;

    private async Task<AiToolPermissionMode?> PortalConfigModeAsync() =>
        (await _aiSettings.AiSettingsGetAsync(TestContext.Current.CancellationToken)).Response.ToolPermissionMode;

    /// <summary>The observed backend default, not a guaranteed contract.</summary>
    [Fact]
    public async Task Get_AFreshUserIsOnAuto()
    {
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Auto);
        (await UserConfigModeAsync()).Should().Be(AiToolPermissionMode.Auto);
        (await PortalConfigModeAsync()).Should().Be(AiToolPermissionMode.Auto);
    }

    [Theory]
    [InlineData(AiChatToolPermissionMode.Ask, AiToolPermissionMode.Ask)]
    [InlineData(AiChatToolPermissionMode.Allow, AiToolPermissionMode.Allow)]
    [InlineData(AiChatToolPermissionMode.Auto, AiToolPermissionMode.Auto)]
    public async Task Set_EachMode_RoundTripsThroughTheStringAndTheNumericRead(AiChatToolPermissionMode value, AiToolPermissionMode numeric)
    {
        await SetModeAsync(value);

        (await ModeAsync()).Should().Be(value);
        (await UserConfigModeAsync()).Should().Be(numeric);
        (await PortalConfigModeAsync()).Should().Be(numeric);
    }

    [Theory]
    [InlineData("""{"value":"sometimes"}""")]
    [InlineData("""{"value":2}""")]
    [InlineData("""{"mode":"allow"}""")]
    public async Task Set_AnUnknownStringANumberOrAWrongKey_Is400AndLeavesTheModeAlone(string body)
    {
        await SetModeAsync(AiChatToolPermissionMode.Ask);

        // Raw: a string outside the enum, a number and a wrong key cannot be built from the typed request.
        using var response = await new RawApiClient(_aiChatClient).PutRawAsync(
            "/api/2.0/ai/preferences/set-tool-permission-mode",
            body,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Ask);
        (await UserConfigModeAsync()).Should().Be(AiToolPermissionMode.Ask);
    }

    [Fact]
    public async Task Mode_IsIsolatedBetweenUsers()
    {
        var member = await InviteRoleAsync(McpRole.User);
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Auto);

        await SetModeAsync(AiChatToolPermissionMode.Allow);
        await ActAsAsync(member);
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Auto, "the owner's write leaves the member alone");
        (await UserConfigModeAsync()).Should().Be(AiToolPermissionMode.Auto);

        await SetModeAsync(AiChatToolPermissionMode.Ask);
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Ask);
        (await UserConfigModeAsync()).Should().Be(AiToolPermissionMode.Ask);

        await ActAsAsync(Owner);
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Allow, "the member's write leaves the owner alone");
        (await UserConfigModeAsync()).Should().Be(AiToolPermissionMode.Allow);
    }

    /// <summary>Saving another preference of the user config must leave the mode alone; an empty body is refused.</summary>
    [Fact]
    [Trait("Bug", "82725")]
    public async Task SetUserConfig_SavingAnotherPreference_LeavesTheModeAlone()
    {
        await SetModeAsync(AiChatToolPermissionMode.Allow);

        var saved = await _aiSettings.AiSettingsSetUserAsync(
            new Dictionary<string, object> { ["chatRecommendedModelVisible"] = false },
            TestContext.Current.CancellationToken);

        saved.Response.ChatRecommendedModelVisible.Should().BeFalse();
        var read = (await _aiSettings.AiSettingsGetUserAsync(TestContext.Current.CancellationToken)).Response;
        read.ChatRecommendedModelVisible.Should().BeFalse();
        read.ToolPermissionMode.Should().Be(AiToolPermissionMode.Allow);
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Allow);

        var empty = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiSettings.AiSettingsSetUserAsync(new Dictionary<string, object>(), TestContext.Current.CancellationToken));
        empty.ErrorCode.Should().Be(400);
        (await ModeAsync()).Should().Be(AiChatToolPermissionMode.Allow);
        (await UserConfigModeAsync()).Should().Be(AiToolPermissionMode.Allow);
    }
}
