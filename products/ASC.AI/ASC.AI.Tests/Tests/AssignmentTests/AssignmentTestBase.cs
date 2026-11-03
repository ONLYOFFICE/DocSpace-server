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

namespace ASC.AI.Tests.Tests.AssignmentTests;

/// <summary>
/// Shared plumbing of the assignment suites ported from assignments.spec.ts: profiles of a chosen capability class,
/// soft-failing writes that hand the result back, the two resolve routes, and a few raw requests for bodies the SDK
/// models cannot carry.
/// </summary>
/// <remarks>
/// The TypeScript suite took profiles out of the gateway's fixed catalogue by capability mask. The integration-test
/// host has no gateway, so a profile of each class is created on the stub provider instead; the masks are the
/// <c>Capabilities</c> flags (Chat 0x001, Image 0x002, Vision 0x080, Tools 0x100). The first profile of a portal takes
/// the <c>Default</c> slot on creation, so a test that cares which profile is the fallback creates that one first.
/// </remarks>
public abstract class AssignmentTestBase(AspireAppFixture fixture) : AssignmentsStorageTests.AssignmentsTestBase(fixture)
{
    /// <summary>Chat | Tools: a plain text model.</summary>
    protected const int TextCapabilities = 257;

    /// <summary>Chat | Vision | Tools: a text model that also reads images.</summary>
    protected const int TextVisionCapabilities = 385;

    /// <summary>Image: a drawing model that cannot chat.</summary>
    protected const int ImageCapabilities = 2;

    private const string AssignmentsRoute = "/api/2.0/ai/assignments";

    protected async Task<AiProfile> CreateProfileWithAsync(int capabilities)
    {
        var input = BuildProfileInput();
        input.Capabilities = capabilities;

        var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{ModelProvider.Describe()}");

        return result.Profile;
    }

    /// <summary>Several text profiles; the first one is the portal's <c>Default</c>.</summary>
    protected async Task<List<AiProfile>> CreateTextProfilesAsync(int count)
    {
        var profiles = new List<AiProfile>();

        for (var i = 0; i < count; i++)
        {
            profiles.Add(await CreateProfileWithAsync(TextCapabilities));
        }

        return profiles;
    }

    /// <summary>Binds without failing the test on a refusal: a refused assignment is a 200 with success=false.</summary>
    protected Task<AiAssignmentMutationResult> TryAssignAsync(AiActionType actionType, string profileId) =>
        _aiAssignments.AiAssignmentsAssignAsync(new AiAssignmentsAssignRequest(actionType, profileId), TestContext.Current.CancellationToken);

    protected Task<AiBulkAssignmentResult> BulkAssignAsync(Dictionary<string, string> map) =>
        _aiAssignments.AiAssignmentsBulkAssignAsync(map, TestContext.Current.CancellationToken);

    protected Task<AiSuccessResponse> UnassignAsync(AiActionType actionType) =>
        _aiAssignments.AiAssignmentsUnassignAsync(actionType.ToString(), TestContext.Current.CancellationToken);

    protected Task<AiSuccessResponse> CascadeProfileDeleteAsync(string profileId) =>
        _aiAssignments.AiAssignmentsCascadeProfileDeleteAsync(new AiAssignmentsCascadeProfileDeleteRequest(profileId), TestContext.Current.CancellationToken);

    protected Task<AiResolvedAssignment> ResolveAsync(string actionType, string? entityId = null) =>
        _aiAssignments.AiAssignmentsResolveForActionAsync(actionType, entityId, TestContext.Current.CancellationToken);

    protected Task<AiResolvedAssignment> TryResolveAsync(string actionType, string? entityId = null) =>
        _aiAssignments.AiAssignmentsTryResolveForActionAsync(actionType, entityId, TestContext.Current.CancellationToken);

    /// <summary>
    /// Both resolve routes name the expected profile, fully expanded. They are the same lookup and differ only when
    /// nothing resolves, so asking both lets a divergence show up in whichever test hits it first.
    /// </summary>
    protected async Task ExpectResolvedAsync(string profileId, AiActionType actionType, string? entityId = null)
    {
        var label = $"{actionType}{(entityId is null ? "" : $" @{entityId}")}";

        var resolved = await ResolveAsync(actionType.ToString(), entityId);
        var tried = await TryResolveAsync(actionType.ToString(), entityId);

        resolved.ProfileId.Should().Be(profileId, $"resolve {label}");
        resolved.Profile.Id.Should().Be(profileId, $"resolve {label} expanded");
        tried.Should().NotBeNull($"try-resolve {label} answers like resolve");
        tried.ProfileId.Should().Be(profileId, $"try-resolve {label}");
        tried.Profile.Id.Should().Be(profileId, $"try-resolve {label} expanded");
    }

    /// <summary>A body the SDK models cannot carry (a missing field, a bare value, an unknown action), sent to a PUT route.</summary>
    protected async Task<(int Status, string Body)> PutRawAsync(string route, string json)
    {
        using var response = await new RawApiClient(_aiChatClient).PutRawAsync($"{AssignmentsRoute}/{route}", json, TestContext.Current.CancellationToken);

        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    protected async Task<(int Status, string Body)> DeleteRawAsync(string route, object body)
    {
        using var response = await new RawApiClient(_aiChatClient).DeleteAsync($"{AssignmentsRoute}/{route}", body, TestContext.Current.CancellationToken);

        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
