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
/// Which profile a task actually runs on: the portal binding of the action, then <c>Default</c> (assignments.spec.ts,
/// "resolution precedence"). resolve-for-action and try-resolve-for-action are the same lookup and differ only when
/// nothing resolves at all: resolve answers 404, try answers 200 with a null body.
/// </summary>
/// <remarks>
/// The agent-scoped precedence tests of the TypeScript suite (an agent's own model wins over the portal Chat; two agents
/// each resolve their own) have no public counterpart any more: the public routes drop <c>entityId</c> (see
/// <see cref="AssignmentEntityScopeTests"/>). <c>Code</c> falls back through <c>Chat</c> before <c>Default</c>.
/// </remarks>
[Trait("Category", "Read")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentResolutionTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Fact]
    public async Task Resolve_DirectChatBindingWinsOverDefault_AndUnassigningFallsBackToIt()
    {
        var profiles = await CreateTextProfilesAsync(2);
        var (a, b) = (profiles[0], profiles[1]);
        await AssignAsync(AiActionType.Chat, a.Id);
        await AssignAsync(AiActionType.Default, b.Id);

        // Chat=A, Default=B -> Chat resolves to A.
        await ExpectResolvedAsync(a.Id, AiActionType.Chat);

        // An action with no binding of its own still gets Default.
        await ExpectResolvedAsync(b.Id, AiActionType.Summarization);

        // Unassigning Chat -> Chat resolves to Default, which is itself untouched.
        (await UnassignAsync(AiActionType.Chat)).Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull("Chat is empty");
        await ExpectResolvedAsync(b.Id, AiActionType.Chat);
        (await GetAssignmentAsync(AiActionType.Default)).Should().Be(b.Id, "Default itself is untouched");
    }

    [Fact]
    public async Task Resolve_CascadingTheChatProfile_MakesChatResolveToDefault()
    {
        var profiles = await CreateTextProfilesAsync(2);
        var (a, b) = (profiles[0], profiles[1]);
        await AssignAsync(AiActionType.Chat, a.Id);
        await AssignAsync(AiActionType.Default, b.Id);
        await ExpectResolvedAsync(a.Id, AiActionType.Chat);

        (await CascadeProfileDeleteAsync(a.Id)).Success.Should().BeTrue();

        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull("Chat lost its binding");
        await ExpectResolvedAsync(b.Id, AiActionType.Chat);
    }

    [Fact]
    public async Task Resolve_FormAnalysis_IsAnIndependentSlotWithTheDefaultFallback()
    {
        var profiles = await CreateTextProfilesAsync(2);
        var (a, b) = (profiles[0], profiles[1]);

        // A different Default keeps "falls back to Default" distinguishable from "kept its own".
        await AssignAsync(AiActionType.Default, b.Id);

        // Assign -> every read names the new profile.
        await AssignAsync(AiActionType.FormAnalysis, a.Id);
        (await GetAssignmentAsync(AiActionType.FormAnalysis)).Should().Be(a.Id);
        await ExpectResolvedAsync(a.Id, AiActionType.FormAnalysis);
        (await GetAllAssignmentsAsync())[nameof(AiActionType.FormAnalysis)].Should().Be(a.Id);

        // Unassign -> the slot is empty and both resolves fall back to Default.
        (await UnassignAsync(AiActionType.FormAnalysis)).Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.FormAnalysis)).Should().BeNull("FormAnalysis is empty");
        await ExpectResolvedAsync(b.Id, AiActionType.FormAnalysis);
        (await GetAssignmentAsync(AiActionType.Default)).Should().Be(b.Id, "Default itself is untouched");
    }

    [Fact]
    public async Task Resolve_AChatBinding_DoesNotOverrideFormAnalysis()
    {
        var profiles = await CreateTextProfilesAsync(2);
        var (chat, form) = (profiles[0], profiles[1]);
        await AssignAsync(AiActionType.FormAnalysis, form.Id);
        await AssignAsync(AiActionType.Chat, chat.Id);

        await ExpectResolvedAsync(chat.Id, AiActionType.Chat);
        await ExpectResolvedAsync(form.Id, AiActionType.FormAnalysis);
    }

    [Fact]
    public async Task TryResolve_WithNothingToResolve_Answers200WithANullBody()
    {
        await CreateTextProfilesAsync(1);
        await EmptyChatAndDefaultAsync();

        (await TryResolveAsync(nameof(AiActionType.Chat))).Should().BeNull();
        (await TryResolveAsync(nameof(AiActionType.Default))).Should().BeNull();
    }

    /// <summary>"No model is bound" is a state of the portal, not a server fault: it used to be a bare 500, it is a 404 now.</summary>
    [Fact]
    [Trait("Bug", "84278")]
    public async Task Resolve_WithNoProfileToResolve_IsNotA500()
    {
        await CreateTextProfilesAsync(1);
        await EmptyChatAndDefaultAsync();

        var status = await StatusOfAsync(() => ResolveAsync(nameof(AiActionType.Chat)));

        status.Should().NotBe(500, "resolve with no profile to resolve");
        status.Should().Be(404);
    }

    /// <summary>An action whose only candidate is a model that cannot serve it is not a bare 500 either.</summary>
    [Fact]
    [Trait("Bug", "84450")]
    public async Task Resolve_DefaultHoldsAnImageModel_IsNotA500()
    {
        await CreateTextProfilesAsync(1);
        var image = await CreateProfileWithAsync(ImageCapabilities);
        (await UnassignAsync(AiActionType.Chat)).Success.Should().BeTrue();
        (await TryAssignAsync(AiActionType.Default, image.Id)).Success.Should().BeTrue("premise: Default holds the image model");

        // Positive control: the non-throwing sibling answers for the same state.
        (await StatusOfAsync(() => TryResolveAsync(nameof(AiActionType.Chat)))).Should().Be(200);

        var status = await StatusOfAsync(() => ResolveAsync(nameof(AiActionType.Chat)));

        status.Should().NotBe(500, "resolve-for-action answered a bare server error");
    }

    /// <summary>
    /// <c>Default</c> takes any profile, so an image-only model could become the fallback every unassigned text action
    /// resolves through. Either the assignment is refused or Chat must never be handed a profile that cannot chat; the
    /// fix keeps the assignment and answers nothing at resolve time, which satisfies the second reading.
    /// </summary>
    [Fact]
    [Trait("Bug", "82830")]
    public async Task Resolve_Chat_NeverResolvesToAProfileThatCannotChat()
    {
        await CreateTextProfilesAsync(1);
        var image = await CreateProfileWithAsync(ImageCapabilities);

        // Every named text action refuses the profile.
        (await TryAssignAsync(AiActionType.Summarization, image.Id)).Success.Should().BeFalse("Summarization refuses it");

        (await UnassignAsync(AiActionType.Chat)).Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull("Chat falls through to Default");

        var assigned = await TryAssignAsync(AiActionType.Default, image.Id);
        if (!assigned.Success)
        {
            // Refusing the profile at assign time is a valid fix too: then there is nothing left to resolve wrongly.
            return;
        }

        var resolved = await TryResolveAsync(nameof(AiActionType.Chat));

        if (resolved is not null)
        {
            ((int)resolved.Profile.Capabilities & 1).Should().Be(1, $"Chat resolved to {resolved.Profile.ModelId} (capabilities {resolved.Profile.Capabilities})");
        }
    }

    /// <summary>Chat and Default both empty: the only state where resolve and try differ.</summary>
    private async Task EmptyChatAndDefaultAsync()
    {
        foreach (var actionType in new[] { AiActionType.Chat, AiActionType.Default })
        {
            (await UnassignAsync(actionType)).Success.Should().BeTrue($"unassign {actionType}");
            (await GetAssignmentAsync(actionType)).Should().BeNull($"{actionType} is empty");
        }
    }
}
