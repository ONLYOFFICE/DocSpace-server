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
/// Access to the tools surface beyond the plain role matrix (<see cref="McpToolsManagePermissionsTests"/>): what a
/// refusal leaks, who may name a room, what survives an off period of the portal AI switch, and the order of the
/// gate and the serverType check.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpToolsAccessTests(AspireAppFixture fixture) : McpToolsPermissionTestBase(fixture)
{
    private const string Secret = "AUTOTEST-SECRET-TOKEN-4711";
    private const string SecretHost = "secret-host-4711.example.invalid";

    private static Dictionary<string, object> SecretConfig() =>
        new()
        {
            ["url"] = $"https://{SecretHost}/sse",
            ["headers"] = new Dictionary<string, string> { ["Authorization"] = $"Bearer {Secret}" }
        };

    private static Dictionary<string, object?> Body(string? entityId, params (string Key, object? Value)[] fields)
    {
        var body = fields.ToDictionary(f => f.Key, f => f.Value);

        if (entityId is not null)
        {
            body["entityId"] = entityId;
        }

        return body;
    }

    /// <summary>The twelve routes other than the catalogue, started together; an entity is named on each when given.</summary>
    /// <param name="only">When given, only these routes are started; the others are not sent at all.</param>
    private (string Label, Task<ToolsReply> Call)[] StartEveryRoute(string? entityId, params string[] only)
    {
        var q = entityId is null ? string.Empty : $"?entityId={entityId}";
        var and = entityId is null ? string.Empty : $"&entityId={entityId}";

        (string Label, Func<Task<ToolsReply>> Start)[] routes =
        [
            ("list-custom-servers", () => GetToolsAsync("list-custom-servers", q)),
            ("get-custom-server", () => GetToolsAsync("get-custom-server", $"?name=secret-srv{and}")),
            ("add-custom-server", () => PostToolsAsync("add-custom-server", Body(entityId, ("name", "outsider"), ("config", UrlConfig())))),
            ("update-custom-server", () => PutToolsAsync("update-custom-server", Body(entityId, ("name", "secret-srv"), ("config", UrlConfig())))),
            ("remove-custom-server", () => DeleteToolsAsync("remove-custom-server", Body(entityId, ("name", "secret-srv")))),
            ("replace-all-custom-servers", () => PutToolsAsync("replace-all-custom-servers", Body(entityId, ("map", new Dictionary<string, object> { ["outsider"] = UrlConfig() })))),
            ("get-disabled", () => GetToolsAsync("get-disabled", $"?scope={UserScope}{and}")),
            ("set-disabled", () => PutToolsAsync("set-disabled", Body(entityId, ("serverType", WebSearch), ("toolNames", new[] { "x" })))),
            ("is-tool-disabled", () => GetToolsAsync("is-tool-disabled", $"?serverType=secret-srv&toolName=owner-disabled{and}")),
            ("get-allow-always", () => GetToolsAsync("get-allow-always", q)),
            ("set-allow-always", () => PutToolsAsync("set-allow-always", Body(entityId, ("serverType", "secret-srv"), ("toolName", "x"), ("value", true)))),
            ("is-allow-always", () => GetToolsAsync("is-allow-always", $"?serverType=secret-srv&toolName=owner-allowed{and}"))
        ];

        return [.. routes.Where(r => only.Length == 0 || only.Contains(r.Label)).Select(r => (r.Label, r.Start()))];
    }

    private async Task StageOwnersStateAsync()
    {
        await RegisterAsync("secret-srv", SecretConfig());
        await SetDisabledAsync("secret-srv", "owner-disabled");
        await SetAllowAlwaysAsync("secret-srv", "owner-allowed");
    }

    /// <summary>
    /// An administrator can put a credential into a server config (an Authorization header), and the read routes hand
    /// the config back whole to whoever reads. A plain User must not be handed it.
    /// </summary>
    [Theory]
    [Trait("Bug", "84399")]
    [InlineData(McpRole.User)]
    [InlineData(McpRole.RoomAdmin)]
    public async Task Read_ANonAdministrator_IsNotHandedTheAuthorizationHeaderAnAdminStored(McpRole role)
    {
        await RegisterAsync("secret-srv", SecretConfig());
        var member = await InviteRoleAsync(role);
        await ActAsAsync(member);

        var exposed = new List<string>();
        var get = await GetToolsAsync("get-custom-server", "?name=secret-srv");
        var list = await GetToolsAsync("list-custom-servers");

        get.Code.Should().Be(200, get.ToString());
        list.Code.Should().Be(200, list.ToString());
        if (get.Text.Contains(Secret, StringComparison.Ordinal))
        {
            exposed.Add("get-custom-server");
        }

        if (list.Text.Contains(Secret, StringComparison.Ordinal))
        {
            exposed.Add("list-custom-servers");
        }

        exposed.Should().BeEmpty("reads that returned the stored credential");
    }

    /// <summary>
    /// A Guest is refused on twelve routes and answered on the catalogue, which asks for no membership. Nothing the
    /// owner stored moves, and no refusal carries the config.
    /// </summary>
    [Fact]
    public async Task Guest_IsRefusedOnEveryRouteButTheCatalogue_AndTheOwnersStateIsUntouched()
    {
        await StageOwnersStateAsync();
        var before = await SnapshotAsync();
        var guest = await InviteRoleAsync(McpRole.Guest);
        await ActAsAsync(guest);

        var calls = StartEveryRoute(null);
        var settled = await Task.WhenAll(calls.Select(c => c.Call));
        var catalogue = await GetToolsAsync("list-system-tools");
        await ActAsAsync(Owner);

        (await SnapshotAsync()).Should().Be(before);
        for (var i = 0; i < calls.Length; i++)
        {
            settled[i].Code.Should().Be(403, calls[i].Label + ": " + settled[i]);
            settled[i].Text.Should().NotContain(Secret, calls[i].Label);
        }

        catalogue.Code.Should().Be(200);
    }

    /// <summary>A User and a RoomAdmin keep their own tool settings but cannot touch the registry; the owner's state is left alone.</summary>
    [Theory]
    [InlineData(McpRole.User)]
    [InlineData(McpRole.RoomAdmin)]
    public async Task Member_KeepsTheirOwnToolSettings_ButCannotTouchTheRegistry(McpRole role)
    {
        await RegisterAsync("portal-secret", SecretConfig());
        var before = await SnapshotAsync();
        var member = await InviteRoleAsync(role);
        await ActAsAsync(member);

        await SetDisabledAsync("portal-secret", "t");
        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["portal-secret"] = ["t"] });

        var add = await PostToolsAsync("add-custom-server", new { name = "user-registered", config = UrlConfig() });
        var update = await PutToolsAsync("update-custom-server", new { name = "portal-secret", config = UrlConfig() });
        var remove = await DeleteToolsAsync("remove-custom-server", new { name = "portal-secret" });
        var replace = await PutToolsAsync("replace-all-custom-servers", new { map = new Dictionary<string, object> { ["x"] = UrlConfig() } });
        await ActAsAsync(Owner);

        (await SnapshotAsync()).Should().Be(before, "refused registry writes and the member's own settings left the owner's state alone");
        foreach (var (label, reply) in new[] { ("add", add), ("update", update), ("remove", remove), ("replace-all", replace) })
        {
            reply.Code.Should().Be(403, label + ": " + reply);
            reply.Error.Should().Be("Forbidden", label);
        }
    }

    /// <summary>
    /// A tool-settings write that names a room the caller cannot reach is refused, the refusal carries no
    /// configuration, no endpoint and no server name, and nothing the owner stored moves. A User may write their tool
    /// settings without a room (the role matrix), so the room is what refuses here.
    /// </summary>
    [Fact]
    public async Task Write_ToolSettingsNamingARoomTheCallerCannotReach_IsA403_AndLeaksNothing()
    {
        var roomId = (await NewRoomIdAsync()).ToString();
        var outsider = await InviteRoleAsync(McpRole.User);

        await ExpectWritesRefusedAsync(outsider, roomId, "set-disabled", "set-allow-always");
    }

    /// <summary>
    /// The same for the registry writes. Those are administrator-only, so a User naming the room would be refused by
    /// the role and never test the entity: the caller is a DocSpace administrator, who passes the role but cannot open
    /// the Owner's My documents — an administrator reads every room, not another user's personal folder.
    /// </summary>
    [Fact]
    public async Task Write_RegistryNamingAFolderTheAdministratorCannotReach_IsA403_AndLeaksNothing()
    {
        var folderId = await GetMyDocumentsFolderIdAsync();
        var admin = await InviteRoleAsync(McpRole.DocSpaceAdmin);

        // Premise: the folder really is closed to this administrator.
        await FilesHttpClient.Authenticate(admin);
        var premise = await Assert.ThrowsAsync<ApiException>(
            async () => await FoldersApi.GetFolderInfoAsync(folderId, TestContext.Current.CancellationToken));
        premise.ErrorCode.Should().Be(403, "the Owner's My documents is not the administrator's to open");

        await ExpectWritesRefusedAsync(admin, folderId.ToString(), "add-custom-server", "update-custom-server", "remove-custom-server", "replace-all-custom-servers");
    }

    /// <summary>
    /// Stages the Owner's state, sends the given writes naming <paramref name="entityId"/> as <paramref name="caller"/>,
    /// and checks every one is a 403 that leaks nothing and moved nothing.
    /// </summary>
    private async Task ExpectWritesRefusedAsync(User caller, string entityId, params string[] routes)
    {
        await StageOwnersStateAsync();
        var before = await SnapshotAsync();
        await ActAsAsync(caller);

        var writes = StartEveryRoute(entityId, routes);
        var settled = await Task.WhenAll(writes.Select(c => c.Call));
        await ActAsAsync(Owner);

        writes.Select(c => c.Label).Should().BeEquivalentTo(routes);
        (await SnapshotAsync()).Should().Be(before, "the refused writes changed nothing");
        for (var i = 0; i < writes.Length; i++)
        {
            settled[i].Code.Should().Be(403, writes[i].Label + ": " + settled[i]);
            settled[i].Text.Should().NotContain(Secret, writes[i].Label).And.NotContain(SecretHost, writes[i].Label).And.NotContain("secret-srv", writes[i].Label);
        }
    }

    /// <summary>
    /// Tool settings that name a room follow the caller's access to it: accepted while a member, refused once removed
    /// (the refusal writes nothing), accepted again after a re-invite — and the same write without a room is never
    /// affected.
    /// </summary>
    [Fact]
    public async Task ToolSettingsNamingARoom_FollowTheCallersAccessToIt()
    {
        var roomId = await NewRoomIdAsync();
        await RegisterAsync("secret-srv", SecretConfig());
        var member = await InviteRoleAsync(McpRole.RoomAdmin);
        await ShareRoomAsync(roomId, member, FileShare.ContentCreator);
        await ActAsAsync(member);

        var whileMember = await PutToolsAsync("set-disabled", Body(roomId.ToString(), ("serverType", "secret-srv"), ("toolNames", new[] { "mine" })));
        whileMember.Code.Should().Be(200, whileMember.ToString());

        await ShareRoomAsync(roomId, member, FileShare.None);
        await ActAsAsync(member);

        var afterRemoval = await PutToolsAsync("set-disabled", Body(roomId.ToString(), ("serverType", "secret-srv"), ("toolNames", new[] { "after-removal" })));
        var allowAfterRemoval = await PutToolsAsync("set-allow-always", Body(roomId.ToString(), ("serverType", "secret-srv"), ("toolName", "after-removal"), ("value", true)));
        afterRemoval.Code.Should().Be(403, afterRemoval.ToString());
        allowAfterRemoval.Code.Should().Be(403, allowAfterRemoval.ToString());
        afterRemoval.Text.Should().NotContain(Secret);

        (await DisabledAsync()).Should().BeEquivalentTo(
            new Dictionary<string, List<string>> { ["secret-srv"] = ["mine"] },
            "only the write made while a member survives");
        (await AllowedAsync()).Should().BeEmpty();

        await ActAsAsync(Owner);
        await ShareRoomAsync(roomId, member, FileShare.ContentCreator);
        await ActAsAsync(member);

        var afterReinvite = await PutToolsAsync("set-disabled", Body(roomId.ToString(), ("serverType", "secret-srv"), ("toolNames", new[] { "mine", "again" })));
        afterReinvite.Code.Should().Be(200, afterReinvite.ToString());
    }

    /// <summary>With AI access off every route but the catalogue is refused; nothing stored is lost to, or changed by, the off period.</summary>
    [Fact]
    public async Task AiSwitchOffAndOn_EveryRouteIsRefusedWhileOff_AndNothingStoredChanges()
    {
        await StageOwnersStateAsync();
        var before = await SnapshotAsync();

        await SetPortalAiAccessAsync(false);
        await ActAsAsync(Owner);
        var calls = StartEveryRoute(null);
        var settled = await Task.WhenAll(calls.Select(c => c.Call));
        var catalogue = await GetToolsAsync("list-system-tools");

        for (var i = 0; i < calls.Length; i++)
        {
            settled[i].Code.Should().Be(403, calls[i].Label + ": " + settled[i]);
            settled[i].Error.Should().Be("Forbidden", calls[i].Label);
            settled[i].Text.Should().NotContain(Secret, calls[i].Label);
        }

        catalogue.Code.Should().Be(200, "the catalogue is the one route AI-off does not gate");

        await SetPortalAiAccessAsync(true);
        (await SnapshotAsync()).Should().Be(before, "servers, disabled lists and approvals all survived, and no refused write landed");
    }

    private static string ValidValues(string text) => text.Split("valid values:").ElementAtOrDefault(1) ?? string.Empty;

    /// <summary>
    /// With AI off the gate comes first: a registered custom server as serverType is a 403 like every other tools
    /// write, and the refusal is not a 400 that lists (and so leaks) the registered server names.
    /// </summary>
    [Fact]
    [Trait("Bug", "84400")]
    public async Task SetDisabled_WithAiOff_NamingARegisteredServerIsA403LikeAnyOtherWrite()
    {
        await RegisterAsync("secret-srv", SecretConfig());
        await SetDisabledAsync("secret-srv", "control");

        await SetPortalAiAccessAsync(false);
        await ActAsAsync(Owner);
        var refused = await PutToolsAsync("set-disabled", new { serverType = "secret-srv", toolNames = new[] { "while-off" } });
        var unknown = await PutToolsAsync("set-disabled", new { serverType = "no-such-server", toolNames = new[] { "while-off" } });
        await SetPortalAiAccessAsync(true);

        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["secret-srv"] = ["control"] });
        ValidValues(refused.Text).Should().NotContain("secret-srv");
        refused.Text.Replace("secret-srv", "X").Should().Be(unknown.Text.Replace("no-such-server", "X"), "a registered and an unknown type are answered alike");
        refused.Code.Should().Be(403, refused.ToString());
    }

    /// <summary>A Guest naming a registered server must get the same refusal as one naming an unknown server.</summary>
    [Fact]
    [Trait("Bug", "84401")]
    public async Task SetDisabled_AGuestNamingARegisteredServer_IsA403NotA400ListingTheValidValues()
    {
        await RegisterAsync("portal-secret", SecretConfig());
        var guest = await InviteRoleAsync(McpRole.Guest);
        await ActAsAsync(guest);

        var refused = await PutToolsAsync("set-disabled", new { serverType = "portal-secret", toolNames = new[] { "g" } });
        var unknown = await PutToolsAsync("set-disabled", new { serverType = "no-such-server", toolNames = new[] { "g" } });

        ValidValues(refused.Text).Should().NotContain("portal-secret");
        refused.Text.Replace("portal-secret", "X").Should().Be(unknown.Text.Replace("no-such-server", "X"));
        refused.Code.Should().Be(403, refused.ToString());
    }
}
