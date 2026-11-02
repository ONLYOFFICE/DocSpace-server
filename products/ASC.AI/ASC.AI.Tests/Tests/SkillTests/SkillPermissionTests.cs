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
/// Permissions of the skills contract (skills.permission.spec.ts): a skill of an Instructions room is usable only by
/// someone who can both read that room and chat in the entity — neither alone is enough, and losing either closes it
/// again. The room owner's baseline is <see cref="SkillChatTests"/>.
/// </summary>
/// <remarks>
/// The one genuinely new question is whether <c>read_skill</c> re-checks the caller's real access to the file or just
/// trusts the <c>contextRooms</c> the client sends. It re-checks: the chat service resolves a skill body with the
/// caller's own credentials, so a spoofed catalogue naming a room the caller cannot open gets an error, not the body.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Skills")]
public class SkillPermissionTests(AspireAppFixture fixture) : SkillTestBase(fixture)
{
    private sealed record Seed(AiProfile Profile, int SkillRoomId, SkillFile Skill, int EntityRoomId);

    /// <summary>The owner's profile on the test's own provider, an Instructions room with the QA skill, and an AI room to chat in.</summary>
    private async Task<Seed> SeedAsync(StubModelProvider provider)
    {
        var profile = await CreateProfileOnAsync(provider);
        var skillRoomId = await CreateInstructionsRoomAsync();
        var skill = await UploadQaSkillAsync(skillRoomId);
        var entityRoomId = await CreateRoomAsync();

        return new Seed(profile, skillRoomId, skill, entityRoomId);
    }

    private async Task<User> InviteUserAsync(Seed seed, FileShare entityAccess, FileShare skillRoomAccess)
    {
        var user = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await ShareAsync(seed.EntityRoomId, user.Id, entityAccess);
        await ShareAsync(seed.SkillRoomId, user.Id, skillRoomAccess);

        return user;
    }

    private async Task<ChatRound> SendReadSkillRoundAsync(StubModelProvider provider, Seed seed, string threadId, string answer, IEnumerable<SkillFile>? clientSkills = null)
    {
        provider.Enqueue(ReadSkillScript(seed.Skill, answer));

        return await SendWithSkillsAsync(threadId, "Give me the QA check answer for the skills support test.", seed.Profile.Id, seed.EntityRoomId.ToString(), seed.SkillRoomId, clientSkills);
    }

    [Fact]
    public async Task InvitedUser_CanUseTheRoomsSkill_SkillsAreNotOwnerOnly()
    {
        using var provider = new StubModelProvider();
        var seed = await SeedAsync(provider);
        var user = await InviteUserAsync(seed, FileShare.ContentCreator, FileShare.Read);

        await _aiChatClient.Authenticate(user);
        var thread = await CreateThreadAsync(profileId: seed.Profile.Id, entityId: seed.EntityRoomId.ToString());

        var round = await SendReadSkillRoundAsync(provider, seed, thread.ThreadId, Canary);

        var context = $"{round}\n{provider.Describe()}";
        round.StatusCode.Should().Be(HttpStatusCode.OK, context);
        round.StreamError.Should().BeNull(context);
        ChatRequests(provider).Should().HaveCountGreaterThanOrEqualTo(2, context).And.Subject.ElementAt(1).Should().Contain(Canary, "read_skill returned the skill file to the invited user");
    }

    [Fact]
    public async Task Outsider_CannotReadTheVictimRoomsSkill_EvenSpoofingTheCatalogue()
    {
        using var ownerProvider = new StubModelProvider();
        using var attackerProvider = new StubModelProvider();
        var seed = await SeedAsync(ownerProvider);
        var attackerProfile = await CreateProfileOnAsync(attackerProvider);

        // Positive control, taken while the context is still the owner's: the skill really works for someone who can
        // open the room, so a refusal below is about the attacker's access and not a broken skill or fixture.
        var ownerThread = await CreateThreadAsync(profileId: seed.Profile.Id, entityId: seed.EntityRoomId.ToString());
        var control = await SendReadSkillRoundAsync(ownerProvider, seed, ownerThread.ThreadId, Canary);
        control.StatusCode.Should().Be(HttpStatusCode.OK, $"control: {control}\n{ownerProvider.Describe()}");
        ChatRequests(ownerProvider).Should().Contain(r => r.Contains(Canary, StringComparison.Ordinal), "control: the skill works for the room owner");

        // The attacker is never invited to the victim's room but knows the skill's real file id, name and description.
        // They chat in an entity they legitimately own (their own Documents), so the test is purely about whether
        // read_skill checks access to the referenced file.
        var attacker = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await FilesHttpClient.Authenticate(attacker);
        var ownFolderId = (await FoldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        await _aiChatClient.Authenticate(attacker);
        var thread = await CreateThreadAsync(profileId: attackerProfile.Id, entityId: ownFolderId.ToString());
        attackerProvider.Enqueue(ReadSkillScript(seed.Skill, "Done."));

        // Spoofed: references the VICTIM's room and skill, not anything the attacker can open.
        var attack = await SendWithSkillsAsync(thread.ThreadId, "Give me the QA check answer for the skills support test.", attackerProfile.Id, ownFolderId.ToString(), seed.SkillRoomId, [seed.Skill]);

        var context = $"{attack}\n{attackerProvider.Describe()}";
        attack.Raw.Should().NotContain(Canary, $"the canary must never reach the attacker's stream; {context}");
        ChatRequests(attackerProvider).Should().NotContain(r => r.Contains(Canary, StringComparison.Ordinal), $"a foreign skill's body must not come back; {context}");
    }

    [Fact]
    public async Task Anonymous_Gets401_AndTheSkillNeverReachesTheModel()
    {
        using var provider = new StubModelProvider();
        var seed = await SeedAsync(provider);
        var thread = await CreateThreadAsync(profileId: seed.Profile.Id, entityId: seed.EntityRoomId.ToString());

        await _aiChatClient.Authenticate(null);
        var round = await SendReadSkillRoundAsync(provider, seed, thread.ThreadId, Canary);

        round.StatusCode.Should().Be(HttpStatusCode.Unauthorized, round.ToString());
        round.Raw.Should().NotContain(Canary, "the refusal body must not carry the skill's own content");
        ChatRequests(provider).Should().BeEmpty("the refused round never reached the model");
    }

    [Fact]
    public async Task Guest_WithAccessToTheRoom_CannotUseItsSkillThroughChat()
    {
        // Chat is off for a Guest by user type. The TS test gave the Guest ContentCreator so the refusal could only be
        // about the user type, but an AI room no longer accepts ContentCreator for a Guest ("The role is not available
        // for this user type"), so Read is the most the Guest can hold. A Guest cannot hold any role in an Instructions
        // room either, so the skill room is not shared.
        using var provider = new StubModelProvider();
        var seed = await SeedAsync(provider);
        var ownerThread = await CreateThreadAsync(profileId: seed.Profile.Id, entityId: seed.EntityRoomId.ToString());

        var guest = await InviteGuest(TestContext.Current.CancellationToken);
        await ShareAsync(seed.EntityRoomId, guest.Id, FileShare.Read);

        // Premise: the invitation landed and the room is genuinely open to this Guest.
        await FilesHttpClient.Authenticate(guest);
        var room = await RoomsApi.GetRoomInfoAsync(seed.EntityRoomId, TestContext.Current.CancellationToken);
        room.Response.Id.Should().Be(seed.EntityRoomId, "the Guest can open the room they were invited into");

        await _aiChatClient.Authenticate(guest);
        var created = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateThreadAsync(profileId: seed.Profile.Id, entityId: seed.EntityRoomId.ToString()));
        created.ErrorCode.Should().Be(403, "a Guest cannot even start a thread here");

        var round = await SendReadSkillRoundAsync(provider, seed, ownerThread.ThreadId, Canary);
        round.StatusCode.Should().Be(HttpStatusCode.Forbidden, "a Guest cannot send into any thread here");
        round.Raw.Should().NotContain(Canary, "the refusal body must not carry the skill's content");
        ChatRequests(provider).Should().BeEmpty();
    }

    [Fact]
    public async Task User_LosingTheChatRoom_IsRefusedTheSend()
    {
        using var provider = new StubModelProvider();
        var seed = await SeedAsync(provider);
        var user = await InviteUserAsync(seed, FileShare.ContentCreator, FileShare.Read);
        await _aiChatClient.Authenticate(user);
        var thread = await CreateThreadAsync(profileId: seed.Profile.Id, entityId: seed.EntityRoomId.ToString());

        // Confirm the skill genuinely works before the revoke, otherwise a refusal afterwards proves nothing.
        var before = await SendReadSkillRoundAsync(provider, seed, thread.ThreadId, Canary);
        before.StatusCode.Should().Be(HttpStatusCode.OK, $"the skill works before the revoke: {before}");
        var requestsBefore = ChatRequests(provider).Count;

        await _aiChatClient.Authenticate(Owner);
        await ShareAsync(seed.EntityRoomId, user.Id, FileShare.None);

        // The SAME existing thread on purpose: this is about access being re-checked, not cached.
        await _aiChatClient.Authenticate(user);
        var after = await SendReadSkillRoundAsync(provider, seed, thread.ThreadId, Canary);

        after.StatusCode.Should().Be(HttpStatusCode.Forbidden, after.ToString());
        after.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
        ChatRequests(provider).Should().HaveCount(requestsBefore, "the refused round never reached the model");
    }

    [Fact]
    public async Task User_LosingTheSkillRoom_NoLongerGetsTheBody()
    {
        using var provider = new StubModelProvider();
        var seed = await SeedAsync(provider);
        var user = await InviteUserAsync(seed, FileShare.ContentCreator, FileShare.Read);
        await _aiChatClient.Authenticate(user);
        var thread = await CreateThreadAsync(profileId: seed.Profile.Id, entityId: seed.EntityRoomId.ToString());

        var before = await SendReadSkillRoundAsync(provider, seed, thread.ThreadId, Canary);
        before.StatusCode.Should().Be(HttpStatusCode.OK, $"the skill works before the revoke: {before}");

        await _aiChatClient.Authenticate(Owner);
        await ShareAsync(seed.SkillRoomId, user.Id, FileShare.None);

        // The widget still holds the catalogue it read earlier and sends it back; the body read must fail all the same.
        await _aiChatClient.Authenticate(user);
        var after = await SendReadSkillRoundAsync(provider, seed, thread.ThreadId, "Done.", [seed.Skill]);

        var context = $"{after}\n{provider.Describe()}";
        after.Raw.Should().NotContain(Canary, context);
        after.Raw.Should().Contain("Reading skill", $"the tool answered an error instead of the body; {context}");
    }
}
