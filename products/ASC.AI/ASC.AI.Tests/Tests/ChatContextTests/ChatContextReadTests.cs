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

namespace ASC.AI.Tests.Tests.ChatContextTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/ChatContext")]
public class ChatContextReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    // Every section of the aggregate must be byte-equal to the individual
    // endpoint it replaces: the Node service serves either from the same
    // snapshot payload, so any drift here is a live semantic divergence.
    // Internal contract: the chat-context aggregate has no public route (only the Node chat service reads it, to prime a chat round).
    [Fact]
    public async Task Read_GlobalSections_MatchIndividualEndpoints()
    {
        var profile = await CreateProfileInternalAsync();
        await CreateAssignmentInternalAsync("Chat", profile.Id);
        await UpsertPreferencesInternalAsync(ReasoningDepth.High);
        await UpsertDisabledToolPrefsInternalAsync(new Dictionary<string, HashSet<string>>
        {
            [SystemToolsServerType] = ["tool-a"]
        });
        await CreateMcpServerInternalAsync("srv-global");

        var context = await ReadChatContextInternalAsync();

        context.Config.Should().NotBeNull();
        context.Profiles.Select(p => p.Id).Should().Contain(profile.Id);

        context.Global.EntityId.Should().BeNull();
        context.Global.Folder.Should().BeNull();
        context.Global.Assignments.Should().BeEquivalentTo(await ReadAllAssignmentsInternalAsync());
        context.Global.Preferences.Should().NotBeNull();
        context.Global.Preferences!.Depth.Should().Be(ReasoningDepth.High);
        context.Global.ToolPrefs.Should().BeEquivalentTo(await ReadToolPrefsInternalAsync());
        context.Global.McpServers.Should().BeEquivalentTo(await ReadAllMcpServersInternalAsync());

        context.Entity.Should().BeNull();
        context.ContextEntity.Should().BeNull();
        context.Thread.Should().BeNull();
        context.Messages.Should().BeNull();
    }

    // Internal contract: the chat-context aggregate has no public route (only the Node chat service reads it, to prime a chat round).
    [Fact]
    public async Task Read_WithEntityId_ReturnsScopedSectionsAndFolder()
    {
        var profile = await CreateProfileInternalAsync();
        var roomId = await CreateRoomAsync("agent-room");
        await CreateAssignmentInternalAsync("Chat", profile.Id, roomId.ToString());
        await CreateMcpServerInternalAsync("srv-scoped", entityId: roomId.ToString());

        var context = await ReadChatContextInternalAsync(entityId: roomId.ToString());

        var entity = context.Entity;
        entity.Should().NotBeNull();
        entity!.EntityId.Should().Be(roomId.ToString());
        entity.Folder.Should().NotBeNull();
        entity.Folder!.Id.Should().Be(roomId);
        entity.Folder.IsAgent.Should().BeTrue();
        entity.Folder.Title.Should().Be("agent-room");
        entity.Assignments.Should().BeEquivalentTo(await ReadAllAssignmentsInternalAsync(roomId.ToString()));
        entity.McpServers.Should().BeEquivalentTo(await ReadAllMcpServersInternalAsync(roomId.ToString()));
    }

    // Internal contract: the chat-context aggregate has no public route (only the Node chat service reads it, to prime a chat round).
    [Fact]
    public async Task Read_WithContextEntityId_ReturnsBothScopes()
    {
        var entityRoom = await CreateRoomAsync();
        var contextRoom = await CreateRoomAsync();

        var context = await ReadChatContextInternalAsync(
            entityId: entityRoom.ToString(),
            contextEntityId: contextRoom.ToString());

        context.Entity.Should().NotBeNull();
        context.Entity!.Folder!.Id.Should().Be(entityRoom);
        context.ContextEntity.Should().NotBeNull();
        context.ContextEntity!.Folder!.Id.Should().Be(contextRoom);
    }

    // Internal contract: the chat-context aggregate has no public route (only the Node chat service reads it, to prime a chat round).
    [Fact]
    public async Task Read_SameContextEntityId_OmitsContextEntity()
    {
        var roomId = await CreateRoomAsync();

        var context = await ReadChatContextInternalAsync(
            entityId: roomId.ToString(),
            contextEntityId: roomId.ToString());

        context.Entity.Should().NotBeNull();
        context.ContextEntity.Should().BeNull();
    }

    // Internal contract: the chat-context aggregate has no public route (only the Node chat service reads it, to prime a chat round).
    [Fact]
    public async Task Read_WithThreadId_ReturnsThreadAndMessages()
    {
        var thread = await CreateThreadInternalAsync();
        var first = await CreateMessageInternalAsync(thread.Id);
        var second = await CreateMessageInternalAsync(thread.Id);

        var context = await ReadChatContextInternalAsync(threadId: thread.Id);

        context.Thread.Should().NotBeNull();
        context.Thread!.Id.Should().Be(thread.Id);
        context.Messages.Should().NotBeNull();
        context.Messages!.Select(m => m.Id).Should().BeEquivalentTo([first.Id, second.Id]);
    }

    // Internal contract: the chat-context aggregate has no public route (only the Node chat service reads it, to prime a chat round).
    [Fact]
    public async Task Read_IncludeMessagesFalse_OmitsMessages()
    {
        var thread = await CreateThreadInternalAsync();
        await CreateMessageInternalAsync(thread.Id);

        var context = await ReadChatContextInternalAsync(threadId: thread.Id, includeMessages: false);

        context.Thread.Should().NotBeNull();
        context.Thread!.Id.Should().Be(thread.Id);
        context.Messages.Should().BeNull();
    }
}
