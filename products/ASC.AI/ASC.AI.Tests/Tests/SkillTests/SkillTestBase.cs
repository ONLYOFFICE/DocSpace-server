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
/// Shared plumbing of the AI Skills suites ported from skills.spec.ts and skills.permission.spec.ts.
/// </summary>
/// <remarks>
/// The TypeScript suite put skill files in a <c>.ai</c> folder of a custom room and handed the catalogue to
/// <c>send-with-stream</c> as <c>actionArgs.contextRoom.skills</c>. The AI concept replaced that: a skill is a Markdown
/// file in the root of an Instructions room (<see cref="RoomType.InstructionsRoom"/>), the composer connects such rooms
/// to a chat as <c>actionArgs.contextRooms</c> (<c>{cloud, id}</c>), and the chat service reads each room's current
/// skills itself with the caller's credentials (<c>GET files/rooms/skills</c>). The model reads a body with the
/// built-in <c>read_skill</c> tool, whose result is the file text. The catalogue of the Files service itself is covered
/// by <c>RoomsSkillsTests</c> in ASC.Files.Tests.
/// <para>
/// The model is the stub provider, so "the model picks the matching skill" cannot be tested; what is tested is
/// everything the server owns: the skill index and the <c>read_skill</c> tool offered to the model, what the tool
/// returns when the model calls it with an id, and who may get a body out of it. A test that scripts the model's calls
/// owns a provider (and a profile on it), so no other test can take its queued answers.
/// </para>
/// </remarks>
public abstract class SkillTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>The line the QA skill's body tells the model to answer; it can only reach a test through a body read.</summary>
    protected const string Canary = "SKILL READ BLUE RACCOON 7429";

    protected const string WeatherCanary = "WEATHER SUNNY 21C";

    protected const string QaSkillName = "qa-happy-path-skill";

    private const string QaSkillDescription = "Use this skill when the user asks for the QA check answer of the skills support test. Read the main instruction of the skill and follow it.";

    private const string WeatherSkillName = "weather-briefing";

    private const string WeatherSkillDescription = "Use this skill when the user asks for a weather summary or a forecast for the day.";

    protected sealed record SkillFile(int Id, string Name, string Description);

    /// <summary>An Instructions room of the owner's: the room whose root Markdown files are skills.</summary>
    protected async Task<int> CreateInstructionsRoomAsync(string? title = null)
    {
        await FilesHttpClient.Authenticate(Owner);

        var room = await RoomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title ?? $"Autotest Skills {Guid.NewGuid():N}", roomType: RoomType.InstructionsRoom),
            TestContext.Current.CancellationToken);

        return room.Response.Id;
    }

    /// <summary>Uploads a Markdown file with the YAML front matter the Files service validates into the room's root.</summary>
    protected async Task<SkillFile> UploadSkillAsync(int roomId, string name, string description, string body)
    {
        await FilesHttpClient.Authenticate(Owner);

        var content = System.Text.Encoding.UTF8.GetBytes($"---\nname: {name}\ndescription: {description}\n---\n{body}\n");
        var file = new FileParameter($"{name}.md", "text/markdown", new MemoryStream(content));
        var uploaded = await FoldersApi.UploadFileAsync(roomId, file: file, cancellationToken: TestContext.Current.CancellationToken);

        return new SkillFile(uploaded.Response.Single().Id, name, description);
    }

    protected Task<SkillFile> UploadQaSkillAsync(int roomId) =>
        UploadSkillAsync(roomId, QaSkillName, QaSkillDescription, $"For the QA check answer write exactly one line: {Canary}. Add no explanations.");

    protected Task<SkillFile> UploadWeatherSkillAsync(int roomId) =>
        UploadSkillAsync(roomId, WeatherSkillName, WeatherSkillDescription, $"Always answer with exactly one line: {WeatherCanary}.");

    /// <summary>Sets the access of one member in a room as the owner.</summary>
    protected async Task ShareAsync(int roomId, Guid userId, FileShare access)
    {
        await FilesHttpClient.Authenticate(Owner);
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation(userId, access)], notify: false),
            TestContext.Current.CancellationToken);
    }

    /// <summary>The script of a model that reads a skill and then answers: the tool call, and the follow-up text.</summary>
    protected static ScriptedAnswer[] ReadSkillScript(SkillFile skill, string answer) =>
    [
        StubModelProvider.ToolCall("read_skill", $$"""{"id":"{{skill.Id}}"}"""),
        StubModelProvider.Text(answer)
    ];

    /// <summary>
    /// Sends one chat round with Instructions rooms connected as <c>actionArgs.contextRooms</c>, as whoever
    /// <see cref="BaseTest._aiChatClient"/> is signed in as. <paramref name="clientSkills"/> are the skills the widget
    /// would have read when the room was connected; the server replaces them with the room's current ones whenever it can
    /// read the room.
    /// </summary>
    protected Task<ChatRound> SendWithSkillsAsync(
        string threadId,
        string text,
        string profileId,
        string entityId,
        int skillRoomId,
        IEnumerable<SkillFile>? clientSkills = null)
    {
        var room = new AiContextRoomRef(
            cloud: "docspace",
            id: skillRoomId.ToString(),
            skills: clientSkills?.Select(skill => new AiRoomSkill(id: skill.Id.ToString(), name: skill.Name, description: skill.Description)).ToList()!);

        return SendWithStreamAsync(threadId, text, profileId, entityId, new AiActionArgs(isReasoning: false, contextRooms: [room]));
    }

    /// <summary>The chat requests the model saw, oldest first: the streamed completions, without the profile's tool probe.</summary>
    protected static List<string> ChatRequests(StubModelProvider provider) =>
        [.. provider.CompletionRequestBodies
            .Where(StubModelProvider.IsStreamed)
            .Where(b => !b.Contains("probe_echo", StringComparison.Ordinal))];

    /// <summary>An owner's AI room with a thread on the given profile: the entity a skills chat runs in.</summary>
    protected async Task<(int RoomId, string ThreadId)> CreateChatAsync(string profileId)
    {
        var roomId = await CreateRoomAsync();
        var thread = await CreateThreadAsync(profileId: profileId, entityId: roomId.ToString());

        return (roomId, thread.ThreadId);
    }
}
