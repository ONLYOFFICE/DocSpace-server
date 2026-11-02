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
/// Helpers of the preferences suites (<c>/api/2.0/ai/preferences</c>, ported from the preferences specs of the
/// TypeScript suite): deep mode, the reasoning level and the tool permission mode.
/// </summary>
/// <remarks>
/// Publicly every preference is ONE value per user: the chat service validates the <c>entityId</c> of a read or a write
/// as a room id (a positive integer, <c>""</c> and <c>"0"</c> meaning no scope) and then drops it, so a write from any
/// room, folder or id lands where every read looks (Bug 82900). The TypeScript suite expected a separate value per
/// agent; the suites here assert the single-value contract. Deep mode is a view of the reasoning level: <c>off</c> is
/// off, any depth is on, and turning it on without a depth stores <c>medium</c>.
/// </remarks>
public abstract class PreferenceTestBase(AspireAppFixture fixture) : ChatTests.ChatPermissionTestBase(fixture)
{
    protected const string PreferencesRoute = "/api/2.0/ai/preferences/";

    /// <summary>The state of a scope that nothing was ever written to.</summary>
    protected static readonly PreferenceState _unset = new(false, false, AiReasoningLevel.Off);

    /// <summary>Deep mode, "was it ever chosen" and the level, as the three read routes report them.</summary>
    protected sealed record PreferenceState(bool Deep, bool IsSet, AiReasoningLevel Level);

    protected async Task<PreferenceState> ReadStateAsync(string? entityId = null)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        return new PreferenceState(
            await _aiPreferences.AiPreferencesGetDeepModeAsync(entityId, cancellationToken),
            await _aiPreferences.AiPreferencesIsDeepModeSetAsync(entityId, cancellationToken),
            await _aiPreferences.AiPreferencesGetReasoningLevelAsync(entityId, cancellationToken));
    }

    protected async Task SetDeepModeAsync(bool value, string? entityId = null)
    {
        // The generated constructor is not annotated for nullability; null leaves the field out of the body.
        var result = await _aiPreferences.AiPreferencesSetDeepModeAsync(
            new AiPreferencesSetDeepModeRequest(value, entityId!),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    protected async Task SetLevelAsync(AiReasoningLevel level, string? entityId = null)
    {
        var result = await _aiPreferences.AiPreferencesSetReasoningLevelAsync(
            new AiPreferencesSetReasoningLevelRequest(level, entityId!),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    /// <summary>The single body argument of clear-deep-mode is the optional entityId; an empty string names no scope.</summary>
    protected async Task ClearAsync(string entityId = "")
    {
        var result = await _aiPreferences.AiPreferencesClearDeepModeAsync(entityId, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    /// <summary>
    /// Raw request on a preferences route — for the bodies and queries the typed SDK cannot produce. Also the way to
    /// read a refusal: the typed calls returning a bool or an enum throw NullReferenceException on any non-2xx (an SDK
    /// defect), not ApiException.
    /// </summary>
    protected Task<(HttpStatusCode Status, string Body)> RawAsync(HttpMethod method, string route, string? json = null) =>
        SendRawAsync(method, PreferencesRoute + route, json);

    protected Task<(HttpStatusCode Status, string Body)> RawReadAsync(string route, string? entityId = null) =>
        RawAsync(HttpMethod.Get, entityId is null ? route : $"{route}?entityId={Uri.EscapeDataString(entityId)}");

    /// <summary>Every route of the surface, deep mode and tool permission mode, as (route, verb, body of a valid call).</summary>
    public static TheoryData<string> AllRoutes => new()
    {
        "get-deep-mode",
        "is-deep-mode-set",
        "set-deep-mode",
        "clear-deep-mode",
        "get-reasoning-level",
        "set-reasoning-level",
        "get-tool-permission-mode",
        "set-tool-permission-mode"
    };

    /// <summary>The status and error body a typed SDK call ended with: 200 and an empty body when it succeeded.</summary>
    protected static async Task<(HttpStatusCode Status, string Body)> HttpOutcomeOfAsync(Func<Task> call)
    {
        var (status, error) = await OutcomeOfAsync(call);

        return ((HttpStatusCode)status, error);
    }

    /// <summary>
    /// A valid call of one route, for the cases that only care about the status it ends with. The writes go through
    /// the typed SDK (an ApiException carries the refusal); the GET routes return a bool or an enum, so they stay raw
    /// (SDK defect: NullReferenceException instead of ApiException on a non-2xx).
    /// </summary>
    protected Task<(HttpStatusCode Status, string Body)> CallRouteAsync(string route)
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        return route switch
        {
            "set-deep-mode" => HttpOutcomeOfAsync(() => _aiPreferences.AiPreferencesSetDeepModeAsync(new AiPreferencesSetDeepModeRequest(true, null!), cancellationToken)),
            "set-reasoning-level" => HttpOutcomeOfAsync(() => _aiPreferences.AiPreferencesSetReasoningLevelAsync(new AiPreferencesSetReasoningLevelRequest(AiReasoningLevel.High, null!), cancellationToken)),
            "set-tool-permission-mode" => HttpOutcomeOfAsync(() => _aiPreferences.AiPreferencesSetToolPermissionModeAsync(new AiPreferencesSetToolPermissionModeRequest(AiChatToolPermissionMode.Ask, null!), cancellationToken)),
            // The empty string is the SDK's way to name no scope: the bare body "".
            "clear-deep-mode" => HttpOutcomeOfAsync(() => _aiPreferences.AiPreferencesClearDeepModeAsync(string.Empty, cancellationToken)),
            _ => RawAsync(HttpMethod.Get, route)
        };
    }

    /// <summary>Invites a member of the type and signs the chat client in as them.</summary>
    protected async Task<User> ActAsMemberAsync(EmployeeType type)
    {
        var member = await InviteMemberAsync(type);
        await _aiChatClient.Authenticate(member);

        return member;
    }

    /// <summary>
    /// An id that is not the user's own scope: <c>room</c> and <c>folder</c> are real entities of the owner,
    /// <c>nonexistent</c> exists nowhere. Signs the files client in as the owner.
    /// </summary>
    protected async Task<string> CreateEntityAsync(string kind)
    {
        switch (kind)
        {
            case "room":
                return (await CreateCustomRoomAsync("Autotest Preference Room")).ToString();
            case "folder":
                var myDocuments = await GetMyDocumentsFolderIdAsync();
                var folder = await FoldersApi.CreateFolderAsync(myDocuments, new CreateFolderRequest("Autotest Preference Folder"), TestContext.Current.CancellationToken);
                return folder.Response.Id.ToString();
            default:
                return "999999999";
        }
    }
}
