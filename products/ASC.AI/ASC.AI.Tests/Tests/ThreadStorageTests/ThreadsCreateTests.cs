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

/// <summary>
/// <c>POST /api/2.0/ai/threads/create</c> beyond the storage-level cases of <see cref="ThreadCreateTests"/> and
/// <see cref="ThreadTitleLengthTests"/>: what a new thread looks like through every read path, titles stored
/// verbatim, the title and entityId refusals, and concurrent creates.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadsCreateTests(AspireAppFixture fixture) : ThreadsPortTestBase(fixture)
{
    private const string CreatePath = PublicThreadsPath + "/create";

    public static TheoryData<string> AwkwardTitles => new()
    {
        "Я",
        "Привет, мир — отчёт №1",
        "Plan 😀🚀 done",
        """He said "hi" <b>&amp;</b> 'x' % _ ; \ /""",
        new string('t', 255)
    };

    [Fact]
    public async Task Create_Owner_IsReadableListedEmptyAndKeepsWhatWasSent()
    {
        var profile = await GetProfileAsync();

        var first = await CreateOwnThreadAsync("Alpha");

        var stored = await GetThreadAsync(first.ThreadId);
        stored.ThreadId.Should().Be(first.ThreadId);
        stored.Title.Should().Be("Alpha");
        stored.ProfileId.Should().Be(profile.Id);
        stored.LastEditDate.Should().BeGreaterThan(0);

        var listed = await ListThreadsAsync();
        listed.Should().ContainSingle();
        listed[0].ToJson().Should().Be(stored.ToJson(), "the list carries the same data as get-by-id");

        (await ReadAllAsync(first.ThreadId)).Should().BeEmpty();
        (await OpenAsync(first.ThreadId)).PriorMessages.Should().BeEmpty("open-or-create replays nothing either");

        var firstSnapshot = await SnapshotAsync(first.ThreadId);

        // A second thread with the very same title is allowed, gets its own id and leaves the first alone.
        var second = await CreateOwnThreadAsync("Alpha");

        second.ThreadId.Should().NotBe(first.ThreadId);
        (await SnapshotAsync(first.ThreadId)).Should().Be(firstSnapshot);
        await ExpectOnlyThreadsAsync([first.ThreadId, second.ThreadId], "both same-titled threads are listed");
        (await ListThreadsAsync()).Select(t => t.Title).Should().Equal("Alpha", "Alpha");
    }

    [Theory]
    [MemberData(nameof(AwkwardTitles))]
    public async Task Create_AwkwardTitle_IsStoredVerbatim(string title)
    {
        var created = await CreateOwnThreadAsync(title);

        (await GetThreadAsync(created.ThreadId)).Title.Should().Be(title);
        (await ListThreadsAsync()).Select(t => t.Title).Should().Equal(title);
    }

    // A missing title and one over 255 characters are covered in ThreadCreateTests / ThreadTitleLengthTests. A
    // numeric or object title is not ported: rejecting a wrong-typed JSON value is the framework's model binding.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankTitle_Returns400AndCreatesNothing(string title)
    {
        var profile = await GetProfileAsync();
        var keeper = await CreateOwnThreadAsync("Keeper");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateThreadAsync(title, profile.Id));

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "a refused create left no thread behind");
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task Create_NullTitle_Returns400AndCreatesNothing()
    {
        var profile = await GetProfileAsync();
        var keeper = await CreateOwnThreadAsync("Keeper");

        // Raw on purpose: title is a required constructor parameter of AiThreadsCreateRequest, so the SDK refuses
        // to send an explicit null.
        var (status, body) = await SendRawAsync(
            HttpMethod.Post,
            CreatePath,
            $$"""{ "title": null, "profileId": "{{profile.Id}}" }""");

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "a refused create left no thread behind");
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task Create_NoBody_Returns400AndCreatesNothing()
    {
        var keeper = await CreateOwnThreadAsync("Keeper");

        // Raw on purpose: a request with no body at all cannot be produced by the typed client.
        var (status, body) = await SendRawAsync(HttpMethod.Post, CreatePath);

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "nothing was created");
        status.Should().Be(HttpStatusCode.BadRequest, body);
    }

    [Fact]
    public async Task Create_MissingOrNullEntityId_IsTheGlobalScope()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateRoomAsync();
        var inRoom = await CreateThreadAsync("In the room", profile.Id, roomId.ToString());

        var missing = await CreateThreadAsync("No entity", profile.Id);

        // Raw on purpose: AiThreadsCreateRequest drops a null entityId (EmitDefaultValue = false), so the typed
        // call cannot send an explicit "entityId": null.
        var (status, body) = await SendRawAsync(
            HttpMethod.Post,
            CreatePath,
            $$"""{ "title": "Null entity", "profileId": "{{profile.Id}}", "entityId": null }""");

        status.Should().Be(HttpStatusCode.OK, body);
        using var created = JsonDocument.Parse(body);
        var nullId = created.RootElement.GetProperty("threadId").GetString();
        IsUuid(nullId).Should().BeTrue(body);

        // One list per user (Bug 82719): the room thread and the two unscoped ones are all in it.
        await ExpectOnlyThreadsAsync([inRoom.ThreadId, missing.ThreadId, nullId!], "every thread lands in the one list");
    }

    // An unknown numeric id is in ThreadCreateTests. A numeric (non-string) entityId is not ported: rejecting a
    // wrong-typed JSON value is the framework's model binding.
    [Fact]
    public async Task Create_MalformedEntityId_Returns404AndCreatesNothing()
    {
        var profile = await GetProfileAsync();
        var keeper = await CreateOwnThreadAsync("Keeper");

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await CreateThreadAsync("Refused", profile.Id, "abc"));

        await ExpectOnlyThreadsAsync([keeper.ThreadId], "a refused create left no orphan thread");
        exception.ErrorCode.Should().Be(404);
    }

    // The "unknown extra fields are ignored" half of the TypeScript case is not ported: dropping an unknown JSON
    // field is the framework's model binding, not DocSpace.
    [Fact]
    public async Task Create_InARoom_IsStoredAndListed()
    {
        var profile = await GetProfileAsync();
        var roomId = await CreateRoomAsync("Autotest Threads Room");

        var created = await CreateThreadAsync("In a room", profile.Id, roomId.ToString());

        IsUuid(created.ThreadId).Should().BeTrue(created.ThreadId);
        var thread = await GetThreadAsync(created.ThreadId);
        thread.Title.Should().Be("In a room");
        thread.ProfileId.Should().Be(profile.Id);
        (await ListIdsAsync()).Should().Contain(created.ThreadId);
    }

    [Fact]
    public async Task Create_InParallel_AllSucceedWithDistinctIds()
    {
        // The profile first: GetProfileAsync caches lazily and would create one profile per racing call.
        var profile = await GetProfileAsync();

        var created = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(i => CreateThreadAsync($"Parallel {i}", profile.Id)));
        var ids = created.Select(t => t.ThreadId).ToList();

        ids.Distinct().Should().HaveCount(8, "every create got its own id");
        await ExpectOnlyThreadsAsync(ids, "all eight are listed, none lost");
    }
}
