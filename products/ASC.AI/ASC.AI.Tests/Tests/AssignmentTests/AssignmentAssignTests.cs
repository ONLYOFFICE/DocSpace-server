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
/// Binding a profile to an action (assignments.spec.ts, "assigning a profile"). A refused assignment is a 200 with
/// success=false, so every write is checked for <c>success</c> and then read back.
/// </summary>
[Trait("Category", "Write")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentAssignTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    private const string MissingProfileId = "019fcc1d-2c4d-7557-b8d2-6b4f1be1b212";

    [Theory]
    [InlineData(AiActionType.Chat)]
    [InlineData(AiActionType.Summarization)]
    [InlineData(AiActionType.Translation)]
    [InlineData(AiActionType.TextAnalyze)]
    [InlineData(AiActionType.Code)]
    public async Task Assign_TextProfileToTextAction_ReadsBackAndResolves(AiActionType actionType)
    {
        var text = await CreateProfileWithAsync(TextCapabilities);

        var result = await TryAssignAsync(actionType, text.Id);

        result.Success.Should().BeTrue(result.Error?.Message);
        (await GetAssignmentAsync(actionType)).Should().Be(text.Id);
        (await ResolveAsync(actionType.ToString())).ProfileId.Should().Be(text.Id);
    }

    [Fact]
    public async Task Assign_OneProfileToSeveralActions_BacksThemAll()
    {
        var text = await CreateProfileWithAsync(TextCapabilities);

        foreach (var actionType in new[] { AiActionType.Chat, AiActionType.Summarization, AiActionType.Translation })
        {
            (await TryAssignAsync(actionType, text.Id)).Success.Should().BeTrue($"assign {actionType}");
        }

        var all = await GetAllAssignmentsAsync();
        all[nameof(AiActionType.Chat)].Should().Be(text.Id);
        all[nameof(AiActionType.Summarization)].Should().Be(text.Id);
        all[nameof(AiActionType.Translation)].Should().Be(text.Id);
    }

    [Fact]
    public async Task Assign_ChangingOneAction_LeavesTheOthersAlone()
    {
        var first = await CreateProfileWithAsync(TextCapabilities);
        var second = await CreateProfileWithAsync(TextVisionCapabilities);

        await AssignAsync(AiActionType.Chat, first.Id);
        await AssignAsync(AiActionType.Summarization, first.Id);

        await AssignAsync(AiActionType.Chat, second.Id);

        (await GetAssignmentAsync(AiActionType.Chat)).Should().Be(second.Id);
        (await GetAssignmentAsync(AiActionType.Summarization)).Should().Be(first.Id, "Summarization is untouched");
    }

    /// <summary>
    /// Every task named in the requirement gets a model of its own, all seven at once. Seven different profiles, so a
    /// read-back naming the right id cannot be a coincidence: one shared slot behind the scenes would collapse them
    /// all onto the last write.
    /// </summary>
    [Fact]
    public async Task Assign_EachOfTheSevenTasks_HoldsAModelOfItsOwn()
    {
        var drawing = await CreateProfileWithAsync(ImageCapabilities);
        var vision = await CreateProfileWithAsync(TextVisionCapabilities);
        var ocr = await CreateProfileWithAsync(TextVisionCapabilities);
        var chat = await CreateProfileWithAsync(TextCapabilities);
        var summarization = await CreateProfileWithAsync(TextCapabilities);
        var translation = await CreateProfileWithAsync(TextCapabilities);
        var textAnalyze = await CreateProfileWithAsync(TextCapabilities);

        var plan = new (AiActionType Action, AiProfile Profile)[]
        {
            (AiActionType.Chat, chat),
            (AiActionType.Summarization, summarization),
            (AiActionType.Translation, translation),
            (AiActionType.TextAnalyze, textAnalyze),
            (AiActionType.OCR, ocr),
            (AiActionType.Vision, vision),
            (AiActionType.ImageGeneration, drawing)
        };
        plan.Select(p => p.Profile.Id).Distinct().Should().HaveCount(7, "the premise: seven different models");

        foreach (var (action, profile) in plan)
        {
            var result = await TryAssignAsync(action, profile.Id);
            result.Success.Should().BeTrue($"assign {action}: {result.Error?.Message}");
        }

        // Read every binding back only after all seven writes, so a later write overwriting an earlier one is caught.
        var all = await GetAllAssignmentsAsync();
        foreach (var (action, profile) in plan)
        {
            (await GetAssignmentAsync(action)).Should().Be(profile.Id, $"{action} reads back its own model");
            all[action.ToString()].Should().Be(profile.Id, $"{action} in get-all-assignments");

            var resolved = await ResolveAsync(action.ToString());
            resolved.ProfileId.Should().Be(profile.Id, $"{action} resolves to its own model, not to Default");
            resolved.Profile.ModelId.Should().Be(profile.ModelId);
        }

        // Moving one task must leave the other six where they were.
        await AssignAsync(AiActionType.OCR, vision.Id);

        (await GetAssignmentAsync(AiActionType.OCR)).Should().Be(vision.Id);
        foreach (var (action, profile) in plan.Where(p => p.Action != AiActionType.OCR))
        {
            (await GetAssignmentAsync(action)).Should().Be(profile.Id, $"{action} is untouched by the OCR change");
        }
    }

    [Fact]
    public async Task Assign_NonExistentProfile_IsRefusedAndRecordsNothing()
    {
        await CreateProfileWithAsync(TextCapabilities);

        var result = await TryAssignAsync(AiActionType.Chat, MissingProfileId);

        result.Success.Should().BeFalse();
        result.Error.Message.Should().Be("Profile not found");
        (await GetAssignmentAsync(AiActionType.Chat)).Should().BeNull("no dangling binding is left behind");
    }

    // Raw: the typed request has both fields as required constructor parameters and an enum for the action, so a
    // missing field and an unknown action name cannot be built.
    [Theory]
    [InlineData("""{"profileId":"019fcc1d-2c4d-7557-b8d2-6b4f1be1b212"}""")]
    [InlineData("""{"actionType":"Chat"}""")]
    [InlineData("""{"actionType":"NotAnAction","profileId":"019fcc1d-2c4d-7557-b8d2-6b4f1be1b212"}""")]
    [InlineData("{}")]
    public async Task Assign_MissingOrUnknownField_IsRejectedWith400(string body)
    {
        var (status, response) = await PutRawAsync("assign", body);

        status.Should().Be(400, response);
    }
}
