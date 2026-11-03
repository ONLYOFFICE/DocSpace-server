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

namespace ASC.AI.Tests.Tests.McpTests;

/// <summary>
/// What an <c>entityId</c> does on the registry routes. The registry is one map per portal: a supplied entity must be
/// REACHABLE (a missing or deleted one is a 404 on every write, Bug 82975) but is otherwise dropped, so a server
/// "registered for a room" is the portal-wide one (Bug 82863 — the widget sends the current location here). The
/// reads take an entity for compatibility and ignore it. Registering needs a DocSpace administrator; the entity adds no
/// permission of its own.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpServerEntityScopeTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    [Fact]
    [Trait("Bug", "82863")]
    public async Task Add_ForARoom_IsListedAndReadableForIt()
    {
        var roomId = (await NewRoomIdAsync()).ToString();
        var config = UrlConfig();

        (await AddServerAsync("autotest-room-server", config, roomId)).Success.Should().BeTrue();

        ShouldBeConfig(await GetServerAsync("autotest-room-server", roomId), config, "the write really happened");
        ShouldHold(await ListServersAsync(roomId), new Dictionary<string, object> { ["autotest-room-server"] = config }, "it used to be reachable by name and absent from every listing");
    }

    /// <summary>
    /// The other half of what a room id does: it is not a partition key, so naming a room registers the server for the
    /// whole portal. That is the design (one map per portal), and the portal-wide listing says so.
    /// </summary>
    [Fact]
    [Trait("Bug", "82975")]
    public async Task Add_ForARoom_IsServedPortalWide()
    {
        var roomId = (await NewRoomIdAsync()).ToString();

        (await AddServerAsync("autotest-room-server", UrlConfig(), roomId)).Success.Should().BeTrue();

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["autotest-room-server"] = UrlConfig() });
        ShouldHold(await ListServersAsync(roomId), new Dictionary<string, object> { ["autotest-room-server"] = UrlConfig() });
    }

    [Fact]
    public async Task UpdateAndRemove_ByName_WorkWithTheRoomAsTheEntity()
    {
        var roomId = (await NewRoomIdAsync()).ToString();
        await RegisterAsync("autotest-room-server", UrlConfig(), roomId);
        var other = UrlConfig("other.example.invalid");

        (await UpdateServerAsync("autotest-room-server", other, roomId)).Success.Should().BeTrue();
        ShouldBeConfig(await GetServerAsync("autotest-room-server", roomId), other);

        (await RemoveServerAsync("autotest-room-server", roomId)).Success.Should().BeTrue();
        (await GetServerAsync("autotest-room-server", roomId)).Should().BeNull();
        (await ListServersAsync()).Should().BeEmpty();
    }

    /// <summary>
    /// A folder is a legitimate location for a chat, so an accessible one is accepted as the entity like a room (it was
    /// refused with 403 for its own owner). It does not partition the registry either.
    /// </summary>
    [Fact]
    [Trait("Bug", "83007")]
    public async Task Add_ForAFolderOfTheirOwn_IsAcceptedAndServedPortalWide()
    {
        var myDocuments = await GetMyDocumentsFolderIdAsync();
        var folder = (await FoldersApi.CreateFolderAsync(myDocuments, new CreateFolderRequest("Autotest MCP Folder"), TestContext.Current.CancellationToken)).Response;
        var folderId = folder.Id.ToString();

        (await AddServerAsync("autotest-folder-server", UrlConfig(), folderId)).Success.Should().BeTrue();

        ShouldBeConfig(await GetServerAsync("autotest-folder-server", folderId), UrlConfig());
        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["autotest-folder-server"] = UrlConfig() });
    }

    /// <summary>
    /// A room member who is not a DocSpace administrator reads what is registered but cannot register or remove
    /// anything; a user outside the room cannot name the room in a write at all.
    /// </summary>
    [Fact]
    [Trait("Bug", "83998")]
    public async Task RoomMember_ReadsTheRegistryButCannotRegisterAny_AndAnOutsiderCannotNameTheRoom()
    {
        var roomId = await NewRoomIdAsync();
        var member = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var outsider = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await ShareRoomAsync(roomId, member, FileShare.ContentCreator);
        await ActAsAsync(Owner);
        await RegisterAsync("autotest-owner-server", UrlConfig(), roomId.ToString());

        await ActAsAsync(member);
        await AssertForbiddenAsync(() => AddServerAsync("autotest-member-server", UrlConfig(), roomId.ToString()), "the member registers");
        await AssertForbiddenAsync(() => RemoveServerAsync("autotest-owner-server", roomId.ToString()), "the member removes");
        ShouldBeConfig(await GetServerAsync("autotest-owner-server", roomId.ToString()), UrlConfig(), "reading is not refused");

        // Like the member, a plain user may not register at all, so a bare 403 would pass whether or not the room was
        // checked: the refusal has to be the entity gate's (Entity "<id>" is not accessible), which names the room.
        await ActAsAsync(outsider);
        var outsiderRefusal = await AssertForbiddenAsync(() => AddServerAsync("autotest-outsider-server", UrlConfig(), roomId.ToString()), "the outsider names the room");
        using var refusal = JsonDocument.Parse(outsiderRefusal.ErrorContent?.ToString() ?? "{}");
        refusal.RootElement.TryGetProperty("error", out var error).Should().BeTrue(refusal.RootElement.ToString());
        error.GetString().Should().Be($"Entity \"{roomId}\" is not accessible", "the room is what the outsider is refused");

        await ActAsAsync(Owner);
        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["autotest-owner-server"] = UrlConfig() }, "the registration survived every refused write");
    }

    /// <summary>A read does not look at the entity: one that names nothing reads the portal registry.</summary>
    [Theory]
    [InlineData("abc")]
    [InlineData(MissingEntityId)]
    [InlineData("-1")]
    public async Task Read_EntityThatNamesNothing_ReadsThePortalRegistry(string entityId)
    {
        await RegisterAsync("portal-one", UrlConfig());

        ShouldHold(await ListServersAsync(entityId), new Dictionary<string, object> { ["portal-one"] = UrlConfig() });
        ShouldBeConfig(await GetServerAsync("portal-one", entityId), UrlConfig());
    }

    /// <summary>
    /// A write against an entity that resolves to nothing is refused, not folded into the portal registry. It used to
    /// let <c>add</c> and <c>replace-all</c> land portal-wide, <c>replace-all</c> replacing everything the portal had.
    /// </summary>
    [Fact]
    [Trait("Bug", "82975")]
    public async Task Write_UnknownEntity_IsA404ForAddReplaceAllAndRemove_AndThePortalRegistryIsUntouched()
    {
        await RegisterAsync("portal-server", UrlConfig());

        var unreachable = UrlConfig("x.example.invalid");
        var refusals = new (string Label, Func<Task> Call)[]
        {
            ("add", () => AddServerAsync("unknown-entity-server", unreachable, MissingEntityId)),
            ("replace-all", () => ReplaceAllAsync(new Dictionary<string, object> { ["unknown-entity-server"] = unreachable }, MissingEntityId)),
            ("remove", () => RemoveServerAsync("portal-server", MissingEntityId))
        };

        foreach (var (label, call) in refusals)
        {
            var exception = await Assert.ThrowsAsync<ApiException>(call);

            exception.ErrorCode.Should().Be(404, $"{label} against an unknown entity");
            // The body is JSON, so the quotes around the id are escaped in it.
            exception.ErrorContent?.ToString().Should().Contain("Entity").And.Contain(MissingEntityId).And.Contain("not found");
        }

        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["portal-server"] = UrlConfig() }, "neither written nor emptied");
    }

    /// <summary>
    /// A deleted room is an unknown entity: its reads fall back to the portal registry (they never looked at it) and a
    /// write against it is refused instead of landing portal-wide.
    /// </summary>
    [Fact]
    [Trait("Bug", "82975")]
    public async Task DeletedRoom_ReadsTheRegistry_AndAWriteAgainstItIsRefused()
    {
        var roomId = await NewRoomIdAsync();
        await RegisterAsync("autotest-portal-server", UrlConfig());

        await RoomsApi.DeleteRoomAsync(roomId, new DeleteRoomRequest(false), TestContext.Current.CancellationToken);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        ApiException? refusal = null;

        while (refusal is null && DateTime.UtcNow < deadline)
        {
            try
            {
                var result = await AddServerAsync("autotest-after-death", UrlConfig("x.example.invalid"), roomId.ToString());
                result.Success.Should().BeTrue();
                await RemoveServerAsync("autotest-after-death");
                await Task.Delay(500, TestContext.Current.CancellationToken);
            }
            catch (ApiException exception)
            {
                refusal = exception;
            }
        }

        refusal.Should().NotBeNull("the room is deleted in the background and a write against it is refused once it is gone");
        refusal!.ErrorCode.Should().Be(404);
        ShouldHold(await ListServersAsync(roomId.ToString()), new Dictionary<string, object> { ["autotest-portal-server"] = UrlConfig() });
        ShouldHold(await ListServersAsync(), new Dictionary<string, object> { ["autotest-portal-server"] = UrlConfig() });
    }

    private static async Task<ApiException> AssertForbiddenAsync(Func<Task> call, string because)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(call);

        exception.ErrorCode.Should().Be(403, because);

        return exception;
    }
}
