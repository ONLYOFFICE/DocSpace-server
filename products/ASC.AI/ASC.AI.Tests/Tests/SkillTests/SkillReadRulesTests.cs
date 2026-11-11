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

namespace ASC.AI.Tests.Tests.SkillTests;

/// <summary>
/// GET /api/2.0/ai/context/get-room-skill — one skill file of an Instructions room (<c>contextController.getRoomSkill</c>,
/// <c>roomSkills.getRoomSkill</c>): both ids are required, and at most 256 KiB of the file is read.
/// </summary>
[Trait("Category", "Skills")]
[Trait("Feature", "AI/Skills")]
public class SkillReadRulesTests(AspireAppFixture fixture) : SkillTestBase(fixture)
{
    [Theory]
    [InlineData("", "1")]
    [InlineData("1", "")]
    public async Task GetRoomSkill_MissingRoomOrSkill_ThrowsBadRequest(string roomId, string skillId)
    {
        // Arrange
        await _aiChatClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiContext.AiContextGetRoomSkillAsync(roomId, skillId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetRoomSkill_FileLargerThan256KiB_IsCutAt256KiB()
    {
        // Arrange: an ASCII body, so one byte is one character
        var roomId = await CreateInstructionsRoomAsync();
        var skill = await UploadSkillAsync(roomId, "large-skill", "A skill with a very long body.", new string('x', 300 * 1024));
        await _aiChatClient.Authenticate(Owner);

        // Act
        var raw = await _aiContext.AiContextGetRoomSkillAsync(
            roomId.ToString(System.Globalization.CultureInfo.InvariantCulture), skill.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), cancellationToken: TestContext.Current.CancellationToken);

        // Assert: an SDK defect - a string answer comes back as its JSON literal, not parsed, so it is parsed here
        var text = JsonSerializer.Deserialize<string>(raw)!;
        text.StartsWith("---\nname: large-skill", StringComparison.Ordinal).Should().BeTrue();
        text.Length.Should().Be(256 * 1024);
    }
}
