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

namespace ASC.AI.Tests.Tests.ThreadStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadCreateTests(AspireAppFixture fixture) : ThreadTestBase(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var created = await CreateOwnThreadAsync("my-thread");

        Guid.TryParse(created.ThreadId, out _).Should().BeTrue();
        created.Title.Should().Be("my-thread");

        var stored = await GetThreadAsync(created.ThreadId);
        stored.ThreadId.Should().Be(created.ThreadId);
        stored.Title.Should().Be("my-thread");
    }

    [Fact]
    public async Task Create_WithProfileId_PersistsProfile()
    {
        var profile = await GetProfileAsync();

        var created = await CreateThreadAsync(profileId: profile.Id);

        created.ProfileId.Should().Be(profile.Id);
        (await GetThreadAsync(created.ThreadId)).ProfileId.Should().Be(profile.Id);
    }

    [Fact]
    public async Task Create_WithEntityId_LandsInTheOneThreadListOfTheUser()
    {
        var profile = await GetProfileAsync();
        var folderId = (await GetMyDocumentsFolderIdAsync()).ToString();

        var created = await CreateThreadAsync(profileId: profile.Id, entityId: folderId);

        // By design (Bug 82719): threads are one list per user whatever folder the chat is opened in. The chat
        // service checks that the entity is accessible, then stores the thread unscoped and ignores the entityId
        // of the list, so the thread shows up both with and without it.
        (await ListThreadsAsync()).Should().ContainSingle(t => t.ThreadId == created.ThreadId);
        (await ListThreadsAsync(entityId: folderId)).Should().ContainSingle(t => t.ThreadId == created.ThreadId);
    }

    // Internal contract: entityId scoping of the stored thread has no public route (the chat service stores every
    // thread under entityId null and ignores the entityId of the list, by design - Bug 82719).
    [Fact]
    public async Task Create_WithEntityId_InternalStorageScopesToFolder()
    {
        var roomId = await CreateRoomAsync();

        var created = await CreateThreadInternalAsync(entityId: roomId.ToString());

        created.EntityId.Should().Be(roomId.ToString());

        var scoped = await ReadAllThreadsInternalAsync(roomId.ToString());
        scoped.Should().ContainSingle(t => t.Id == created.Id);

        var global = await ReadAllThreadsInternalAsync();
        global.Should().NotContain(t => t.Id == created.Id);
    }

    [Fact]
    public async Task Create_NonExistentProfileId_Returns404()
    {
        // A profile exists, so whichever way the chat service resolves the model the unknown profileId is what fails.
        await GetProfileAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateThreadAsync("x", Guid.NewGuid().ToString()));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_NonExistentEntityId_Returns404()
    {
        var profile = await GetProfileAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await CreateThreadAsync("x", profile.Id, "999999999"));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task Create_MissingTitle_Returns400()
    {
        var profile = await GetProfileAsync();

        // Raw on purpose: title is a required constructor parameter of AiThreadsCreateRequest, so the SDK refuses to
        // send a body without one.
        using var response = await new RawApiClient(_aiChatClient).PostRawAsync(
            $"{PublicThreadsPath}/create",
            $$"""{ "profileId": "{{profile.Id}}" }""",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
