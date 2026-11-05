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

namespace ASC.AI.Tests.Tests.ChatTests;

/// <summary>
/// Refusals of the chat surface that used to be wrong and are now a plain status with a readable error: an unknown
/// thread, an entity that names nothing, an empty message, a send into a thread that does not exist.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Chat")]
public class ChatPermissionValidationTests(AspireAppFixture fixture) : ChatPermissionTestBase(fixture)
{
    /// <summary>An unknown thread used to answer a bare <c>null</c> under a 200.</summary>
    [Fact]
    [Trait("Bug", "82718")]
    public async Task GetById_UnknownThread_Returns404ThreadNotFound()
    {
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await GetThreadAsync(UnknownId));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent?.ToString().Should().Contain("thread not found");

        // read-messages, on the same unknown id, agrees.
        (await StatusOfAsync(() => ReadAllAsync(UnknownId))).Should().Be(404);
    }

    /// <summary>
    /// An entity id that names nothing used to be accepted and persisted. Any accessible folder is a valid entity by
    /// design (the reopen decision on this bug), so what is pinned is the refusal of one that does not exist.
    /// </summary>
    [Fact]
    [Trait("Bug", "82719")]
    public async Task Create_NonExistentEntity_Returns404EntityNotFound()
    {
        var profile = await GetProfileAsync();
        const string fakeEntityId = "999999999";

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateThreadAsync("Orphan thread", profile.Id, fakeEntityId));

        exception.ErrorCode.Should().Be(404);
        // The body is {"error":"Entity \"999999999\" not found"}; the JSON escapes the quotes around the id.
        exception.ErrorContent?.ToString().Should().Contain("Entity").And.Contain(fakeEntityId).And.Contain("not found");
        (await ListThreadsAsync()).Should().BeEmpty("no thread came back and none was stored");
    }

    /// <summary>An empty message used to be accepted, stored as a real user message and handed to the provider.</summary>
    [Fact]
    [Trait("Bug", "82720")]
    public async Task SendWithStream_EmptyMessage_Returns400AndStoresNothing()
    {
        var seed = await SeedEmptyThreadAsync();

        var round = await SendWithStreamAsync(seed.ThreadId, string.Empty, seed.ProfileId, seed.RoomId);

        round.StatusCode.Should().Be(HttpStatusCode.BadRequest, round.ToString());
        round.Raw.Should().Contain("userMessage must contain non-empty text content or an attachment");
        (await ReadAllAsync(seed.ThreadId)).Should().BeEmpty("the thread is still empty on both sides");
    }

    /// <summary>
    /// The refusal used to arrive as HTTP 200 carrying <c>{"type":"error","message":"stream error"}</c> in the
    /// streamed body; the stream now never opens.
    /// </summary>
    [Fact]
    [Trait("Bug", "82723")]
    public async Task SendWithStream_UnknownThread_Returns404()
    {
        var seed = await SeedEmptyThreadAsync();

        var round = await SendWithStreamAsync(UnknownId, "Hello", seed.ProfileId, seed.RoomId);

        round.StatusCode.Should().Be(HttpStatusCode.NotFound, round.ToString());
        round.Raw.Should().Contain("Not Found");
        round.StreamError.Should().BeNull("the refusal is the status, not a stream frame");
    }

    private async Task<(string ThreadId, string ProfileId, string RoomId)> SeedEmptyThreadAsync()
    {
        var profile = await GetProfileAsync();
        var roomId = (await CreateRoomAsync("Autotest Chat AI Room")).ToString();
        var thread = await CreateThreadAsync("Autotest thread", profile.Id, roomId);

        return (thread.ThreadId, profile.Id, roomId);
    }
}
