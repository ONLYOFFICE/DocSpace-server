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

namespace ASC.AI.Tests.Tests.PreferencesStorageTests;

/// <summary>
/// Helpers for the reasoning-depth preference tests on the public surface (/api/2.0/ai/preferences).
/// </summary>
/// <remarks>
/// The internal preferences row (one <c>depth</c> per scope) is written by <c>set-reasoning-level</c>
/// (and <c>set-deep-mode</c>), read by <c>get-reasoning-level</c> / <c>get-deep-mode</c> /
/// <c>is-deep-mode-set</c> and removed by <c>clear-deep-mode</c>. Publicly it is one row per user: the
/// Node routes validate <c>entityId</c> as a room id and then drop it, so the entity-scoped rows exist only
/// on the internal contract. A missing row reads as <c>off</c>; <c>is-deep-mode-set</c> tells it apart
/// from a stored <c>off</c>.
/// </remarks>
public abstract class PreferencesTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string PublicSetReasoningLevelPath = "/api/2.0/ai/preferences/set-reasoning-level";

    protected async Task SetReasoningLevelAsync(AiReasoningLevel level, string? entityId = null)
    {
        var result = await _aiPreferences.AiPreferencesSetReasoningLevelAsync(
            // The generated constructor is not annotated for nullability; null leaves the field out of the body.
            new AiPreferencesSetReasoningLevelRequest(level, entityId!),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    protected Task<AiReasoningLevel> GetReasoningLevelAsync(string? entityId = null) =>
        _aiPreferences.AiPreferencesGetReasoningLevelAsync(entityId, TestContext.Current.CancellationToken);

    protected Task<bool> IsDepthStoredAsync() =>
        _aiPreferences.AiPreferencesIsDeepModeSetAsync(cancellationToken: TestContext.Current.CancellationToken);

    protected async Task ClearAsync()
    {
        // The single body argument is the optional entityId; an empty string names no scope.
        var result = await _aiPreferences.AiPreferencesClearDeepModeAsync(string.Empty, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }
}
