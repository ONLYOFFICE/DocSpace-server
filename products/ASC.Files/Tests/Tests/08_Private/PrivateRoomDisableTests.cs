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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// Disabling an account deletes its wrapped file keys, so a returning account gets keys only when a member issues them
/// again, and the owner of a private room cannot be disabled until the room has another owner.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "PrivacyRoom")]
public class PrivateRoomDisableTests(AspireAppFixture fixture) : PrivacyRoomTestBase(fixture)
{
    private async Task<(FolderDto Room, int FileId)> CreateRoomWithEncryptedFile()
    {
        await _filesClient.Authenticate(Owner);
        await SetFakeKeys(publicKeyPrefix: "owner");
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);

        return (room, await UploadEncryptedFile(room.Id, "Encrypted.docx"));
    }

    private const string ThroughStatus = "updateUserStatus";
    private const string ThroughUpdateMember = "updateMember";
    private const string ThroughDeleteProfile = "deleteProfile";

    // The confirmation query of each account's removal link: a second request for the letter within the repeat period
    // sends none, so a test that closes the profile twice signs both calls with the first link.
    private readonly Dictionary<Guid, string> _removalLinks = [];

    private async Task Disable(User user, string route)
    {
        if (route == ThroughDeleteProfile)
        {
            await CloseOwnProfile(user);

            return;
        }

        await _peopleClient.Authenticate(Owner);

        if (route == ThroughUpdateMember)
        {
            await _profilesApi.UpdateMemberAsync(user.Id.ToString(), new UpdateMemberRequestDto(disable: true), TestContext.Current.CancellationToken);
        }
        else
        {
            await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Terminated, new UpdateMembersRequestDto([user.Id], resendAll: false), TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// The two steps of self-removal as the client takes them: the letter is asked for, and the profile is closed with
    /// the query of its link in the <c>confirm</c> header.
    /// </summary>
    private async Task CloseOwnProfile(User user)
    {
        if (!_removalLinks.TryGetValue(user.Id, out var query))
        {
            query = await RequestRemovalLink(user);
            _removalLinks[user.Id] = query;
        }

        await _peopleClient.Authenticate(null);
        _peopleClient.DefaultRequestHeaders.Add("confirm", query);

        try
        {
            await _profilesApi.DeleteProfileAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            _peopleClient.DefaultRequestHeaders.Remove("confirm");
        }
    }

    /// <summary>
    /// Asks for the removal letter and takes the link out of its button. The letter is handed to the notify service
    /// inside the request, so the bus shows it; the button holds a short link, which Web.Api redirects to the long one
    /// whose query carries the key.
    /// </summary>
    private async Task<string> RequestRemovalLink(User user)
    {
        await using var bus = await Fixture.ListenToEventBusAsync(nameof(NotifyItemIntegrationEvent), TestContext.Current.CancellationToken);
        await _peopleClient.Authenticate(user);
        await _userDataApi.SendInstructionsToDeleteAsync(TestContext.Current.CancellationToken);

        var all = await bus.ReadAsync<NotifyItemIntegrationEvent>(l => l.TenantId == TenantId, TestContext.Current.CancellationToken);
        var letters = all.Where(l => l.Action.NotifyActionType == "ASC.Web.Studio.Core.Notify.ProfileDeleteNotifyAction").ToList();

        var button = letters.Should().ContainSingle("the letters were: {0}", string.Join("; ", all.Select(l => $"{l.Action.NotifyActionType} to {string.Join(",", l.Recipients.Select(r => r.Id))}"))).Subject.Tags.Select(t => t.Value).First(v => v != null && v.Contains("href=", StringComparison.Ordinal));
        var link = new Uri(WebUtility.HtmlDecode(Regex.Match(button, "href=\"([^\"]+)\"").Groups[1].Value));

        using var response = await _webApiClient.GetAsync(link.PathAndQuery, TestContext.Current.CancellationToken);
        var target = response.Headers.Location is { } location
            ? new Uri(link, location)
            : response.RequestMessage!.RequestUri!;

        target.Query.Should().Contain("key=", "the removal link carries its confirmation key in the query: {0}", target);

        return target.Query.TrimStart('?');
    }

    private async Task<EmployeeStatus> StatusOf(User user)
    {
        await _peopleClient.Authenticate(Owner);

        return (await _profilesApi.GetProfileByUserIdAsync(user.Id.ToString(), TestContext.Current.CancellationToken)).Response.Status!.Value;
    }

    private async Task<long> RevocationRecords(Guid userId)
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);
        await using var command = connection.CreateCommand();

        command.CommandText = "SELECT COUNT(*) FROM audit_events WHERE action = 5165 AND description LIKE @user;";
        command.Parameters.AddWithValue("@user", $"%{userId}%");

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(ThroughStatus)]
    [InlineData(ThroughUpdateMember)]
    [InlineData(ThroughDeleteProfile)]
    public async Task DisablingAMember_DeletesTheirFileKeys_AndTheyStayMissingAfterReenabling(string route)
    {
        var member = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.Read);
        await WriteKeys(fileId, member.Id);
        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();

        await Disable(member, route);
        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(EmployeeStatus.Active, new UpdateMembersRequestDto([member.Id], resendAll: false), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        (await GetMissing(room.Id, member.Id)).Select(m => m.UserId).Should().BeEquivalentTo([member.Id]);
        (await PollUntil(() => RevocationRecords(member.Id), count => count > 0)).Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(ThroughStatus)]
    [InlineData(ThroughUpdateMember)]
    [InlineData(ThroughDeleteProfile)]
    public async Task OwnerOfAPrivateRoom_IsNotDisabled_UntilTheRoomHasAnotherOwner(string route)
    {
        var successor = await InviteWithKeys(EmployeeType.RoomAdmin);
        var roomOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        var room = await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        await InviteToRoom(room.Id, successor, FileShare.RoomManager);

        var exception = await Assert.ThrowsAsync<ApiException>(() => Disable(roomOwner, route));

        exception.ErrorCode.Should().Be(403);
        (await StatusOf(roomOwner)).Should().Be(EmployeeStatus.Active);

        await _filesClient.Authenticate(roomOwner);
        await _sharingApi.ChangeFileOwnerAsync(new ChangeOwnerRequestDto(folderIds: [new(room.Id)], userId: successor.Id), TestContext.Current.CancellationToken);

        await Disable(roomOwner, route);

        (await StatusOf(roomOwner)).Should().Be(EmployeeStatus.Terminated);
    }

    [Fact]
    public async Task DisablingAnAccount_IsAnnouncedOnTheEventBus()
    {
        // The first half of the path an LDAP synchronization takes: every disabling is saved by the same user store,
        // which announces it. The stack has no directory, so an API call disables the account here.
        var member = await InviteContact(EmployeeType.User);
        await using var bus = await Fixture.ListenToEventBusAsync(nameof(UserDisabledIntegrationEvent), TestContext.Current.CancellationToken);

        await Disable(member, ThroughStatus);

        var announcements = await bus.ReadAsync<UserDisabledIntegrationEvent>(e => e.TenantId == TenantId, TestContext.Current.CancellationToken);
        announcements.Should().ContainSingle().Which.UserId.Should().Be(member.Id);
    }

    [Fact]
    public async Task TheAnnouncement_DeletesTheFileKeys_InTheFilesWorker()
    {
        // The second half: the Files worker deletes the keys of the account it is told about, with no People action
        // involved, as for an account a directory synchronization disabled. The member stays active here; the keys go
        // all the same, as they would for an account re-enabled before the worker got to it.
        var member = await InviteWithKeys(EmployeeType.User);
        var (room, fileId) = await CreateRoomWithEncryptedFile();
        await InviteToRoom(room.Id, member, FileShare.Read);
        await WriteKeys(fileId, member.Id);
        (await GetMissing(room.Id, member.Id)).Should().BeEmpty();

        await Fixture.PublishToEventBusAsync(new UserDisabledIntegrationEvent(Owner.Id, TenantId) { UserId = member.Id }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var missing = await PollUntil(() => GetMissing(room.Id, member.Id), m => m.Count > 0, TimeSpan.FromSeconds(30));
        missing.Select(m => m.UserId).Should().BeEquivalentTo([member.Id]);
        (await PollUntil(() => RevocationRecords(member.Id), count => count > 0)).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ABatchWithTheOwnerOfAPrivateRoom_DisablesNobody()
    {
        var roomOwner = await InviteWithKeys(EmployeeType.RoomAdmin);
        await CreatePrivateRoom("Autotest Privacy Room", RoomType.CustomRoom);
        var other = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(Owner);
        var exception = await Assert.ThrowsAsync<ApiException>(() => _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([other.Id, roomOwner.Id], resendAll: false),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        (await StatusOf(other)).Should().Be(EmployeeStatus.Active);
        (await StatusOf(roomOwner)).Should().Be(EmployeeStatus.Active);
    }
}
