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

namespace ASC.AI.Tests.Tests.AssignmentsStorageTests;

/// <summary>
/// Helpers for the assignment tests on the public surface (/api/2.0/ai/assignments, the Node chat service).
/// </summary>
/// <remarks>
/// The public routes keep ONE portal-wide action -> profile map: the Node storage adapter drops every
/// <c>entityId</c> before it calls ASC.AI, so the entity-scoped rows exist only on the internal contract.
/// The first profile created on a portal is bound to the <c>Default</c> slot by the profiles engine, so every
/// map read after a profile create carries <c>Default</c> as well.
/// </remarks>
public abstract class AssignmentsTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const string PublicAssignPath = "/api/2.0/ai/assignments/assign";

    protected const string DefaultSlot = "Default";

    // Chat | Image | Tools (0x001 | 0x002 | 0x100). The engine refuses to bind a profile that lacks the
    // capability a slot needs: Chat, Code and Summarization need Chat, ImageGeneration needs Image.
    private const decimal AssignableCapabilities = 259m;

    /// <summary>Creates a profile on the stub provider that every slot used by these tests accepts.</summary>
    protected async Task<AiProfile> CreateAssignableProfileAsync()
    {
        var input = BuildProfileInput();
        input.Capabilities = AssignableCapabilities;

        var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{ModelProvider.Describe()}");

        return result.Profile;
    }

    /// <summary>Binds a profile to a slot through the public assign; fails the test on a soft rejection.</summary>
    protected async Task AssignAsync(AiActionType actionType, string profileId)
    {
        var result = await _aiAssignments.AiAssignmentsAssignAsync(
            new AiAssignmentsAssignRequest(actionType, profileId),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue($"assign rejected {actionType}: {result.Error?.Message}");
    }

    /// <summary>The profile id bound to one slot, or null when the slot is empty.</summary>
    protected async Task<string?> GetAssignmentAsync(AiActionType actionType)
    {
        var raw = await _aiAssignments.AiAssignmentsGetAssignmentAsync(actionType.ToString(), TestContext.Current.CancellationToken);

        return DecodeJsonString(raw);
    }

    protected Task<Dictionary<string, string>> GetAllAssignmentsAsync(string? entityId = null) =>
        _aiAssignments.AiAssignmentsGetAllAssignmentsAsync(entityId, TestContext.Current.CancellationToken);

    // SDK defect: an operation typed `string` hands back the raw response body, so the JSON string "<id>" keeps
    // its quotes and a JSON null comes back as the text "null". Decode it here until the client is fixed.
    private static string? DecodeJsonString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw == "null")
        {
            return null;
        }

        return raw.StartsWith('"') ? JsonSerializer.Deserialize<string>(raw) : raw;
    }
}
