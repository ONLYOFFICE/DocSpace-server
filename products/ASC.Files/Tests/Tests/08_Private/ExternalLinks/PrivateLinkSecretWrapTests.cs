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

namespace ASC.Files.Tests.Tests._08_Privacy.ExternalLinks;

/// <summary>
/// The secret of a link of a private item is wrapped by the browser for the room owner and each room manager, so that
/// any of them can copy the full address later. The link answers each reader the wrap made for them and names the
/// managers without one; a wrap for anybody else is refused, and a user who stops managing the room's links loses their
/// wraps while the links keep working.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateLinkSecretWrapTests(AspireAppFixture fixture) : PrivateLinkTestBase(fixture)
{
    private async Task<(FolderDto Room, User Manager)> RoomWithAManager()
    {
        var manager = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreateRoomAsOwner();
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        return (room, manager);
    }

    private async Task<(Guid Id, string Token)> SetRoomLink(int roomId, LinkBody body)
    {
        using var response = await PutRoomLink(roomId, body);
        ((int)response.StatusCode).Should().Be(200, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var link = await SharedLinkOf(response);

        return (Guid.Parse(link.GetProperty("id").GetString()!), link.GetProperty("requestToken").GetString()!);
    }

    private async Task<JsonElement> RoomLinkAs(User user, int roomId, Guid linkId)
    {
        await _filesClient.Authenticate(user);

        return (await RoomLinksJson(roomId)).Single(l => l.GetProperty("id").GetString() == linkId.ToString());
    }

    private static string? WrapOf(JsonElement link) =>
        link.TryGetProperty("linkSecretWrap", out var wrap) && wrap.ValueKind == JsonValueKind.Object ? wrap.GetProperty("wrap").GetString() : null;

    private static List<Guid> WithoutWrap(JsonElement link) =>
        [.. link.GetProperty("managersWithoutLinkSecretWrap").EnumerateArray().Select(e => e.GetGuid())];

    private async Task<long> StoredWraps(Guid userId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM files_share_link_secret_wraps WHERE tenant_id = @tenant AND user_id = @user;";
        command.Parameters.AddWithValue("@tenant", TenantId);
        command.Parameters.AddWithValue("@user", userId.ToString());

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), CultureInfo.InvariantCulture);
    }

    [Fact]
    public async Task EachManager_ReadsTheWrapMadeForThem_AndNoOtherOne()
    {
        var (room, manager) = await RoomWithAManager();
        var ownerWrap = SecretWrap.For(Owner.Id);
        var managerWrap = SecretWrap.For(manager.Id);

        await _filesClient.Authenticate(Owner);
        var (linkId, _) = await SetRoomLink(room.Id, new LinkBody { LinkSecretWraps = [ownerWrap, managerWrap] });

        var asOwner = await RoomLinkAs(Owner, room.Id, linkId);
        var asManager = await RoomLinkAs(manager, room.Id, linkId);

        WrapOf(asOwner).Should().Be(ownerWrap.Wrap);
        asOwner.GetProperty("linkSecretWrap").GetProperty("publicKeyId").GetGuid().Should().Be(ownerWrap.PublicKeyId);
        WrapOf(asManager).Should().Be(managerWrap.Wrap);
        asManager.GetRawText().Should().NotContain(ownerWrap.Wrap);
        WithoutWrap(asOwner).Should().BeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AWrapForAnEditorOrANonMember_Returns400_AndStoresNothing(bool member)
    {
        var room = await CreateRoomAsOwner();
        var other = await InviteWithKeys(EmployeeType.RoomAdmin);
        if (member)
        {
            await _filesClient.Authenticate(Owner);
            await InviteToRoom(room.Id, other, FileShare.Editing);
        }

        await _filesClient.Authenticate(Owner);
        using var response = await PutRoomLink(room.Id, new LinkBody { LinkSecretWraps = [SecretWrap.For(Owner.Id), SecretWrap.For(other.Id)] });

        ((int)response.StatusCode).Should().Be(400);
        (await RoomLinkCount(room.Id)).Should().Be(0);
        (await StoredWraps(Owner.Id)).Should().Be(0);
    }

    [Theory]
    [InlineData(FileShare.Editing)]
    [InlineData(FileShare.None)]
    public async Task AManagerWhoStopsManaging_LosesTheirWraps_AndTheLinkStillOpens(FileShare newAccess)
    {
        var (room, manager) = await RoomWithAManager();
        var proof = Proof.New();

        await _filesClient.Authenticate(Owner);
        var (linkId, token) = await SetRoomLink(room.Id, new LinkBody { LinkProofHash = proof.Hash, LinkSecretWraps = [SecretWrap.For(Owner.Id), SecretWrap.For(manager.Id)] });
        (await StoredWraps(manager.Id)).Should().Be(1);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, manager, newAccess);

        (await StoredWraps(manager.Id)).Should().Be(0);
        (await StoredWraps(Owner.Id)).Should().Be(1);

        await _filesClient.Authenticate(null);
        (await OpenLinkSession(token, proof)).Should().NotBeEmpty();
        (await RoomLinkAs(Owner, room.Id, linkId)).GetProperty("id").GetString().Should().Be(linkId.ToString());
    }

    [Fact]
    public async Task ANewManager_IsNamedWithoutAWrap_UntilAManagerAddsOne()
    {
        var room = await CreateRoomAsOwner();

        await _filesClient.Authenticate(Owner);
        var (linkId, _) = await SetRoomLink(room.Id, new LinkBody { LinkSecretWraps = [SecretWrap.For(Owner.Id)] });

        var manager = await InviteWithKeys(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, manager, FileShare.RoomManager);

        var before = await RoomLinkAs(Owner, room.Id, linkId);
        WithoutWrap(before).Should().Equal(manager.Id);
        WrapOf(await RoomLinkAs(manager, room.Id, linkId)).Should().BeNull();

        // The wrap alone, without the key fields, as a manager's browser adds it for a new manager.
        var managerWrap = SecretWrap.For(manager.Id);
        await _filesClient.Authenticate(Owner);
        await SetRoomLink(room.Id, new LinkBody { LinkId = linkId, LinkSecretWraps = [managerWrap] }.WithoutKeys());

        WithoutWrap(await RoomLinkAs(Owner, room.Id, linkId)).Should().BeEmpty();
        WrapOf(await RoomLinkAs(manager, room.Id, linkId)).Should().Be(managerWrap.Wrap);
    }

    [Fact]
    public async Task AManagerMadeAGuest_LosesTheirWraps_AlsoWithoutAFileKeyInTheRoom()
    {
        var (room, manager) = await RoomWithAManager();

        await _filesClient.Authenticate(Owner);
        await SetRoomLink(room.Id, new LinkBody { LinkSecretWraps = [SecretWrap.For(Owner.Id), SecretWrap.For(manager.Id)] });
        (await StoredWraps(manager.Id)).Should().Be(1);

        await _peopleClient.Authenticate(Owner);
        await _profilesApi.UpdateMemberAsync(manager.Id.ToString(), new UpdateMemberRequestDto(isUser: true), TestContext.Current.CancellationToken);

        (await StoredWraps(manager.Id)).Should().Be(0);
        (await StoredWraps(Owner.Id)).Should().Be(1);
    }

    [Fact]
    public async Task ARoomOwnerMadeAGuest_HandsTheRoomOver_AndLosesTheirWraps()
    {
        var roomOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreatePrivateRoom($"Autotest Private {Guid.NewGuid():N}", RoomType.CustomRoom);
        await SetRoomLink(room.Id, new LinkBody { LinkSecretWraps = [SecretWrap.For(roomOwner.Id)] });
        (await StoredWraps(roomOwner.Id)).Should().Be(1);

        // Only a change to a guest hands a private room over: the previous owner gets a lower role for the moment before
        // leaving. A change to a user keeps the room theirs, so they keep managing its links. The recipient needs a key.
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        await _peopleClient.Authenticate(Owner);
        await _userTypeApi.StartUserTypeUpdateAsync(new StartUpdateUserTypeDto(EmployeeType.Guest, roomOwner.Id, Owner.Id), TestContext.Current.CancellationToken);
        var progress = await PollUntil(
            async () => (await _userTypeApi.GetUserTypeUpdateProgressAsync(roomOwner.Id, TestContext.Current.CancellationToken)).Response,
            progress => progress is { IsCompleted: true },
            TimeSpan.FromSeconds(60));

        progress.Error.Should().BeNullOrEmpty();
        (await StoredWraps(roomOwner.Id)).Should().Be(0);
        await _filesClient.Authenticate(Owner);
        (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response.CreatedBy.Id.Should().Be(Owner.Id);
    }
}
