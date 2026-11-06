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

namespace ASC.AI.Tests.Tests.PreferenceTests;

/// <summary>
/// With the portal AI switch off the preferences answer 403 — deep mode, the reasoning level and the tool permission
/// mode alike. The refusal comes ahead of validation (an invalid body is a 403 too), and a refused write is checked
/// with the switch back on: nothing changed.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Preferences")]
public class PreferenceAiDisabledTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    public static TheoryData<string> DeepModeRoutes => new()
    {
        "get-deep-mode",
        "is-deep-mode-set",
        "set-deep-mode",
        "clear-deep-mode",
        "get-reasoning-level",
        "set-reasoning-level"
    };

    [Theory]
    [MemberData(nameof(DeepModeRoutes))]
    public async Task DeepModeRoute_AiDisabled_Returns403AndChangesNothing(string route)
    {
        // Seeded as `true` on an unset scope, so `medium`; the refused write or clear must change neither.
        await SetDeepModeAsync(true);
        await SetPortalAiAccessAsync(false);

        var (status, body) = await CallRouteAsync(route);

        await SetPortalAiAccessAsync(true);
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium), "switching AI back on shows the refused call really was refused");
        status.Should().Be(HttpStatusCode.Forbidden, body);
    }

    /// <summary>
    /// The tool permission mode decides whether AI tool calls need approval, and with AI off there are none, so its
    /// routes are refused like the other preferences — ahead of validation.
    /// </summary>
    [Fact]
    [Trait("Bug", "84302")]
    public async Task ToolPermissionMode_AiDisabled_ReadAndWriteReturn403()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // Stored while AI is on, so "unchanged" below is a fact about a value.
        await _aiPreferences.AiPreferencesSetToolPermissionModeAsync(
            new AiPreferencesSetToolPermissionModeRequest(AiChatToolPermissionMode.Allow, null!),
            cancellationToken);
        await SetPortalAiAccessAsync(false);

        // The control that the switch is in effect: the sibling preference is refused.
        var control = await CallRouteAsync("get-deep-mode");

        var read = await CallRouteAsync("get-tool-permission-mode");
        var write = await CallRouteAsync("set-tool-permission-mode");
        var invalid = await RawAsync(HttpMethod.Put, "set-tool-permission-mode", """{"value":"sometimes"}""");

        // The side effects first. The config route stays readable while AI is off, so it shows whether the refused
        // write got through.
        var stored = (await _aiSettings.AiSettingsGetUserAsync(cancellationToken)).Response.ToolPermissionMode;
        await SetPortalAiAccessAsync(true);
        var afterwards = await _aiPreferences.AiPreferencesGetToolPermissionModeAsync(cancellationToken: cancellationToken);

        stored.Should().Be(AiToolPermissionMode.Allow, "the refused write changed nothing");
        afterwards.Should().Be(AiChatToolPermissionMode.Allow, "the stored mode is the one set while AI was on");
        control.Status.Should().Be(HttpStatusCode.Forbidden, control.Body);
        read.Status.Should().Be(HttpStatusCode.Forbidden, "GET: " + read.Body);
        write.Status.Should().Be(HttpStatusCode.Forbidden, "PUT with a valid value: " + write.Body);
        invalid.Status.Should().Be(HttpStatusCode.Forbidden, "PUT with an invalid value, the AI-access check comes before validation: " + invalid.Body);
    }
}
