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
/// The skills contract of a chat round (skills.spec.ts, "catalogue + read_skill contract"): the connected Instructions
/// rooms' skills are indexed in the prompt, the model gets a <c>read_skill</c> tool, and a call of it returns the whole
/// skill file. The room owner's baseline of skills.permission.spec.ts is the first test.
/// </summary>
[Trait("Category", "Chat")]
[Trait("Feature", "AI/Skills")]
public class SkillChatTests(AspireAppFixture fixture) : SkillTestBase(fixture)
{
    [Fact]
    public async Task ReadSkill_ModelCallsTheTool_ResultCarriesTheSkillBody()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var skillRoom = await CreateInstructionsRoomAsync();
        var qa = await UploadQaSkillAsync(skillRoom);
        var (roomId, threadId) = await CreateChatAsync(profile.Id);
        provider.Enqueue(ReadSkillScript(qa, Canary));

        var round = await SendWithSkillsAsync(threadId, "Give me the QA check answer for the skills support test.", profile.Id, roomId.ToString(), skillRoom);

        var context = $"{round}\n{provider.Describe()}";
        round.StatusCode.Should().Be(HttpStatusCode.OK, context);
        round.StreamError.Should().BeNull(context);
        var requests = ChatRequests(provider);
        requests.Should().HaveCountGreaterThanOrEqualTo(2, $"the tool result goes back to the model; {context}");

        // The first request offers the skill: the index names it and the read_skill tool is there. The body is not in it.
        requests[0].Should().Contain("read_skill", "the model is offered the skill tool").And.Contain(QaSkillName, "the index names the skill");
        requests[0].Should().NotContain(Canary, "only the name and description are indexed, never the body");

        // The follow-up request carries the tool result: the whole skill file, body included.
        requests[1].Should().Contain(Canary, "read_skill returns the skill file");
        round.AssistantText.Should().Be(Canary);
    }

    [Fact]
    public async Task SkillIndex_WhenTheModelDoesNotCallTheTool_NeverSendsTheBody()
    {
        // The TypeScript test asserted that an unrelated prompt does not read the skill. Whether a model reads a skill
        // is the model's choice; what the server owns is that the body leaves only through the tool.
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var skillRoom = await CreateInstructionsRoomAsync();
        await UploadQaSkillAsync(skillRoom);
        var (roomId, threadId) = await CreateChatAsync(profile.Id);
        provider.Enqueue(StubModelProvider.Text("It is 4."));

        var round = await SendWithSkillsAsync(threadId, "How much is 2 + 2?", profile.Id, roomId.ToString(), skillRoom);

        var context = $"{round}\n{provider.Describe()}";
        round.StreamError.Should().BeNull(context);
        round.AssistantText.Should().Be("It is 4.");
        ChatRequests(provider).Should().ContainSingle(context).Which.Should().NotContain(Canary, "the body is not part of the prompt");
    }

    [Fact]
    public async Task ReadSkill_AmongSeveralSkills_ReturnsOnlyTheRequestedOne()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var skillRoom = await CreateInstructionsRoomAsync();
        var qa = await UploadQaSkillAsync(skillRoom);
        var weather = await UploadWeatherSkillAsync(skillRoom);
        var (roomId, threadId) = await CreateChatAsync(profile.Id);
        provider.Enqueue(ReadSkillScript(qa, Canary));

        var round = await SendWithSkillsAsync(threadId, "Give me the QA check answer for the skills support test.", profile.Id, roomId.ToString(), skillRoom);

        var context = $"{round}\n{provider.Describe()}";
        round.StreamError.Should().BeNull(context);
        var requests = ChatRequests(provider);
        requests.Should().HaveCountGreaterThanOrEqualTo(2, context);

        // Both skills are indexed and both ids are offered to the tool...
        requests[0].Should().Contain(QaSkillName).And.Contain(weather.Name);
        requests[0].Should().Contain($"\"{qa.Id}\"").And.Contain($"\"{weather.Id}\"", "the tool takes either id");

        // ...and the call returns the one that was asked for.
        requests[1].Should().Contain(Canary);
        requests[1].Should().NotContain(WeatherCanary, "the other skill was not read");
    }
}
