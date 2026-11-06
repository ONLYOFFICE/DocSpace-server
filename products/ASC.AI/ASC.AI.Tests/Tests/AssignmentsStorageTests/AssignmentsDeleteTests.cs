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

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentsDeleteTests(AspireAppFixture fixture) : AssignmentsTestBase(fixture)
{
    [Fact]
    public async Task Delete_Existing_Removes()
    {
        var profile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.Chat, profile.Id);

        var result = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.Chat), TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_Existing_NotReturnedByReadAll()
    {
        var profile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.Chat, profile.Id);

        var result = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.Chat), TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        // Only the Default slot the first profile took on creation is left.
        (await GetAllAssignmentsAsync()).Keys.Should().BeEquivalentTo(new[] { DefaultSlot });
    }

    [Fact]
    public async Task Delete_Twice_KeepsUnassigned()
    {
        var profile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.Chat, profile.Id);

        var first = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.Chat), TestContext.Current.CancellationToken);
        var second = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.Chat), TestContext.Current.CancellationToken);

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_ThenAssignAgain_Persists()
    {
        var profile = await CreateAssignableProfileAsync();
        var anotherProfile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.Chat, profile.Id);

        var result = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.Chat), TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue();

        await AssignAsync(AiActionType.Chat, anotherProfile.Id);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(anotherProfile.Id);
    }

    [Fact]
    public async Task Delete_TypeWithDefault_NotReturnedByReadAll()
    {
        var profile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.ImageGeneration, profile.Id);

        var result = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.ImageGeneration), TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.ImageGeneration)).Should().BeNull();
        (await GetAllAssignmentsAsync()).Keys.Should().BeEquivalentTo(new[] { DefaultSlot });
    }

    [Fact]
    public async Task Delete_TypeWithDefault_ThenAssignAgain_Persists()
    {
        var profile = await CreateAssignableProfileAsync();
        var anotherProfile = await CreateAssignableProfileAsync();
        await AssignAsync(AiActionType.ImageGeneration, profile.Id);

        var result = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.ImageGeneration), TestContext.Current.CancellationToken);
        result.Success.Should().BeTrue();

        await AssignAsync(AiActionType.ImageGeneration, anotherProfile.Id);

        (await GetAssignmentAsync(AiActionType.ImageGeneration)).Should().Be(anotherProfile.Id);
    }

    [Fact]
    public async Task Delete_NonExisting_Succeeds()
    {
        var result = await _aiAssignments.AiAssignmentsUnassignAsync(nameof(AiActionType.Chat), TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    // The bulk delete (DELETE /assignments with a list of types) is reached publicly only through
    // cascade-profile-delete, which drops every slot bound to the given profile.
    [Fact]
    public async Task DeleteMany_RemovesAllListedTypes()
    {
        var defaultProfile = await CreateAssignableProfileAsync();
        var chatAndCodeProfile = await CreateAssignableProfileAsync();
        var summarizationProfile = await CreateAssignableProfileAsync();

        await AssignAsync(AiActionType.Chat, chatAndCodeProfile.Id);
        await AssignAsync(AiActionType.Code, chatAndCodeProfile.Id);
        await AssignAsync(AiActionType.Summarization, summarizationProfile.Id);

        var result = await _aiAssignments.AiAssignmentsCascadeProfileDeleteAsync(
            new AiAssignmentsCascadeProfileDeleteRequest(chatAndCodeProfile.Id),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        var all = await GetAllAssignmentsAsync();
        all.Keys.Should().BeEquivalentTo(new[] { DefaultSlot, nameof(AiActionType.Summarization) });
        all[DefaultSlot].Should().Be(defaultProfile.Id);
        all[nameof(AiActionType.Summarization)].Should().Be(summarizationProfile.Id);
    }

    [Fact]
    public async Task DeleteMany_ThenAssignAgain_Persists()
    {
        await CreateAssignableProfileAsync();
        var chatProfile = await CreateAssignableProfileAsync();
        var codeProfile = await CreateAssignableProfileAsync();

        await AssignAsync(AiActionType.Chat, chatProfile.Id);

        var result = await _aiAssignments.AiAssignmentsCascadeProfileDeleteAsync(
            new AiAssignmentsCascadeProfileDeleteRequest(chatProfile.Id),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull();

        await AssignAsync(AiActionType.Chat, codeProfile.Id);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(codeProfile.Id);
    }

    [Fact]
    public async Task DeleteMany_EmptyList_Succeeds()
    {
        // A profile bound to no slot gives the cascade an empty list to drop: a no-op that still succeeds.
        var defaultProfile = await CreateAssignableProfileAsync();
        var unboundProfile = await CreateAssignableProfileAsync();

        var result = await _aiAssignments.AiAssignmentsCascadeProfileDeleteAsync(
            new AiAssignmentsCascadeProfileDeleteRequest(unboundProfile.Id),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();

        var all = await GetAllAssignmentsAsync();
        all.Keys.Should().BeEquivalentTo(new[] { DefaultSlot });
        all[DefaultSlot].Should().Be(defaultProfile.Id);
    }

    // Internal contract: an arbitrary list of types for the bulk delete has no public route (only cascade-profile-delete reaches it, with the slots of one profile).
    [Fact]
    public async Task DeleteMany_ArbitraryList_Internal_RemovesAllListedTypes()
    {
        var chatProfile = await CreateProfileInternalAsync();
        var codeProfile = await CreateProfileInternalAsync();
        var summarizationProfile = await CreateProfileInternalAsync();

        await CreateAssignmentInternalAsync("Chat", chatProfile.Id);
        await CreateAssignmentInternalAsync("Code", codeProfile.Id);
        await CreateAssignmentInternalAsync("Summarization", summarizationProfile.Id);

        using var response = await _ai.DeleteAsync(
            AssignmentsPath,
            new { actionTypes = new[] { "Chat", "Code" } },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var all = await ReadAllAssignmentsInternalAsync();
        all.Should().HaveCount(1).And.ContainKey("Summarization");
    }

    // Internal contract: an empty list for the bulk delete has no public route (the Node adapter never sends one).
    [Fact]
    public async Task DeleteMany_EmptyList_Internal_Succeeds()
    {
        using var response = await _ai.DeleteAsync(
            AssignmentsPath,
            new { actionTypes = Array.Empty<string>() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
