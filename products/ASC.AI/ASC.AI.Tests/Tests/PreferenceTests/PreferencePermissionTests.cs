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
/// Who may use the preferences: an anonymous caller is refused with 401 on every route; deep mode is a per-user
/// preference writable by every member type but a Guest, who is refused (403); one user's choice is invisible to
/// another; and the entity a request names is no access gate — the value is the caller's own whatever the id.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Preferences")]
public class PreferencePermissionTests(AspireAppFixture fixture) : PreferenceTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(AllRoutes))]
    public async Task Route_Anonymous_Returns401AndTheOwnersPreferencesAreUntouched(string route)
    {
        await _aiChatClient.Authenticate(null);
        var (status, body) = await CallRouteAsync(route);

        // The refused write left the owner's own preference untouched.
        await _aiChatClient.Authenticate(Owner);
        (await ReadStateAsync()).Should().Be(_unset);
        (await _aiPreferences.AiPreferencesGetToolPermissionModeAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Should().Be(AiChatToolPermissionMode.Auto);
        status.Should().Be(HttpStatusCode.Unauthorized, body);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task DeepMode_Member_ManagesTheirOwnValue(EmployeeType type)
    {
        await ActAsMemberAsync(type);

        (await ReadStateAsync()).Should().Be(_unset);

        await SetDeepModeAsync(true);
        (await ReadStateAsync()).Deep.Should().BeTrue();

        await ClearAsync();
        (await ReadStateAsync()).Deep.Should().BeFalse();
    }

    [Fact]
    public async Task AllDeepModeRoutes_Guest_Return403()
    {
        await ActAsMemberAsync(EmployeeType.Guest);

        var statuses = new Dictionary<string, HttpStatusCode>();
        foreach (var route in new[] { "get-deep-mode", "is-deep-mode-set", "set-deep-mode", "clear-deep-mode", "get-reasoning-level", "set-reasoning-level" })
        {
            statuses[route] = (await CallRouteAsync(route)).Status;
        }

        statuses.Values.Should().OnlyContain(s => s == HttpStatusCode.Forbidden, $"statuses were {string.Join(", ", statuses)}");
    }

    [Fact]
    public async Task DeepMode_OneUsersValue_IsInvisibleToAnotherAndSurvivesTheirWrite()
    {
        // All of the owner's setup happens before the member is signed in.
        await SetDeepModeAsync(true);
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium));
        var member = await InviteMemberAsync(EmployeeType.User);

        await _aiChatClient.Authenticate(member);
        (await ReadStateAsync()).Should().Be(_unset, "the member's own value");

        // The member's own write does not reach the owner's value either.
        await SetDeepModeAsync(false);

        await _aiChatClient.Authenticate(Owner);
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium), "the owner's value survives the member's write");
    }

    [Fact]
    [Trait("Bug", "82816")]
    public async Task EntityId_OfARoomTheCallerCannotOpen_IsNoGate_TheValueIsTheCallersOwn()
    {
        // The TypeScript suite expected 403 for reading, and for writing the switch of, an agent the caller is not
        // in. The service validates the id and drops it (the value is per user), so there is no other user's data to
        // guard: the call is the caller's own read or write, and it must neither crash nor reach the owner's value.
        var roomId = (await CreateRoomAsync("Autotest Preference Private AI Room")).ToString();
        await SetLevelAsync(AiReasoningLevel.High, roomId);
        var member = await InviteMemberAsync(EmployeeType.User);

        await _aiChatClient.Authenticate(member);
        var read = await RawReadAsync("get-reasoning-level", roomId);
        await SetDeepModeAsync(true, roomId);
        var memberState = await ReadStateAsync(roomId);

        await _aiChatClient.Authenticate(Owner);
        (await ReadStateAsync(roomId)).Should().Be(new PreferenceState(true, true, AiReasoningLevel.High), "the owner's value is untouched");
        read.Status.Should().Be(HttpStatusCode.OK, read.Body);
        read.Body.Should().Contain("off", "the member starts from their own unset value, not the owner's");
        memberState.Should().Be(new PreferenceState(true, true, AiReasoningLevel.Medium));
    }

    [Fact]
    public async Task ReasoningLevel_TwoMembers_EachKeepTheirOwnValue()
    {
        var first = await InviteMemberAsync(EmployeeType.User);
        var second = await InviteMemberAsync(EmployeeType.RoomAdmin);

        await _aiChatClient.Authenticate(first);
        await SetLevelAsync(AiReasoningLevel.High);
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.High));

        // The first member's write is invisible to the second, which is what separates "per user" from "per portal".
        await _aiChatClient.Authenticate(second);
        (await ReadStateAsync()).Should().Be(_unset, "the other member's view");
        await SetLevelAsync(AiReasoningLevel.Max);

        await _aiChatClient.Authenticate(first);
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.High), "the second member's write did not overwrite it");

        // A clear by one member leaves the other's value in place.
        await ClearAsync();
        (await ReadStateAsync()).Should().Be(_unset);

        await _aiChatClient.Authenticate(second);
        (await ReadStateAsync()).Should().Be(new PreferenceState(true, true, AiReasoningLevel.Max));
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task ToolPermissionMode_Member_ManagesTheirOwnMode(EmployeeType type)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        // The owner's value is neither the default nor the one the member will pick, so a member who inherited it,
        // or who overwrote it, shows.
        await _aiPreferences.AiPreferencesSetToolPermissionModeAsync(
            new AiPreferencesSetToolPermissionModeRequest(AiChatToolPermissionMode.Ask, null!),
            cancellationToken);
        var member = await InviteMemberAsync(type);

        await _aiChatClient.Authenticate(member);
        (await _aiPreferences.AiPreferencesGetToolPermissionModeAsync(cancellationToken: cancellationToken))
            .Should().Be(AiChatToolPermissionMode.Auto, "the member starts from the default");

        var written = await _aiPreferences.AiPreferencesSetToolPermissionModeAsync(
            new AiPreferencesSetToolPermissionModeRequest(AiChatToolPermissionMode.Allow, null!),
            cancellationToken);

        written.Success.Should().BeTrue();
        (await _aiPreferences.AiPreferencesGetToolPermissionModeAsync(cancellationToken: cancellationToken))
            .Should().Be(AiChatToolPermissionMode.Allow);
        (await _aiSettings.AiSettingsGetUserAsync(cancellationToken)).Response.ToolPermissionMode
            .Should().Be(AiToolPermissionMode.Allow, "the user config reports the same mode");

        await _aiChatClient.Authenticate(Owner);
        (await _aiPreferences.AiPreferencesGetToolPermissionModeAsync(cancellationToken: cancellationToken))
            .Should().Be(AiChatToolPermissionMode.Ask, "the owner's own value survives");
    }

    /// <summary>
    /// A Guest has no AI access and is refused by the other preferences routes. The refusal is decided on the Guest
    /// and so comes before the body is looked at: an invalid value is a 403 too, not a 400.
    /// </summary>
    [Fact]
    [Trait("Bug", "84303")]
    public async Task ToolPermissionMode_Guest_CannotReadOrChangeIt()
    {
        await ActAsMemberAsync(EmployeeType.Guest);

        // The control that this caller really is treated as a Guest: the sibling preference refuses them.
        var control = await CallRouteAsync("get-deep-mode");

        var read = await CallRouteAsync("get-tool-permission-mode");
        var write = await HttpOutcomeOfAsync(() => _aiPreferences.AiPreferencesSetToolPermissionModeAsync(
            new AiPreferencesSetToolPermissionModeRequest(AiChatToolPermissionMode.Allow, null!),
            TestContext.Current.CancellationToken));
        var invalid = await RawAsync(HttpMethod.Put, "set-tool-permission-mode", """{"value":"sometimes"}""");

        // The side effect first: the Guest's own config still shows the default (Auto).
        var stored = (await _aiSettings.AiSettingsGetUserAsync(TestContext.Current.CancellationToken)).Response.ToolPermissionMode;
        stored.Should().Be(AiToolPermissionMode.Auto, "the refused write changed nothing");
        control.Status.Should().Be(HttpStatusCode.Forbidden, control.Body);
        read.Status.Should().Be(HttpStatusCode.Forbidden, read.Body);
        write.Status.Should().Be(HttpStatusCode.Forbidden, write.Body);
        invalid.Status.Should().Be(HttpStatusCode.Forbidden, "authorization comes before validation: " + invalid.Body);
    }
}
