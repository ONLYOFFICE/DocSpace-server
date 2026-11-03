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
/// The capability matrix of assign (assignments.spec.ts, "capability validation"): the profile has to be able to do the
/// job. Each case is (action, capability class, accepted?) and every one is checked against the persisted assignment,
/// not just against <c>success</c>. <c>FormAnalysis</c> is not seeded on this host, so a refusal there is checked
/// against an empty slot.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentCapabilityTests(AspireAppFixture fixture) : AssignmentTestBase(fixture)
{
    [Theory]
    [InlineData(AiActionType.Chat, ImageCapabilities, false)]
    [InlineData(AiActionType.Vision, TextVisionCapabilities, true)]
    [InlineData(AiActionType.Vision, TextCapabilities, false)]
    [InlineData(AiActionType.Vision, ImageCapabilities, false)]
    [InlineData(AiActionType.OCR, TextVisionCapabilities, true)]
    [InlineData(AiActionType.OCR, TextCapabilities, false)]
    [InlineData(AiActionType.OCR, ImageCapabilities, false)]
    [InlineData(AiActionType.ImageGeneration, ImageCapabilities, true)]
    [InlineData(AiActionType.ImageGeneration, TextVisionCapabilities, false)]
    [InlineData(AiActionType.ImageGeneration, TextCapabilities, false)]
    [InlineData(AiActionType.Summarization, ImageCapabilities, false)]
    [InlineData(AiActionType.Translation, ImageCapabilities, false)]
    [InlineData(AiActionType.Code, ImageCapabilities, false)]
    [InlineData(AiActionType.TextAnalyze, TextCapabilities, true)]
    [InlineData(AiActionType.FormAnalysis, TextCapabilities, true)]
    [InlineData(AiActionType.FormAnalysis, TextVisionCapabilities, true)]
    [InlineData(AiActionType.FormAnalysis, ImageCapabilities, false)]
    public async Task Assign_ByCapabilityClass_IsAcceptedOrRefusedAndPersistedAccordingly(AiActionType actionType, int capabilities, bool accepted)
    {
        var profile = await CreateProfileWithAsync(capabilities);
        var before = await GetAssignmentAsync(actionType);

        var result = await TryAssignAsync(actionType, profile.Id);

        if (accepted)
        {
            result.Success.Should().BeTrue(result.Error?.Message);
            (await GetAssignmentAsync(actionType)).Should().Be(profile.Id);

            return;
        }

        result.Success.Should().BeFalse();
        result.Error.Message.Should().Be($"Profile lacks capabilities required for ActionType.{actionType}");

        // A refusal must not have written anything either.
        (await GetAssignmentAsync(actionType)).Should().Be(before, $"{actionType} is unchanged");
    }
}
