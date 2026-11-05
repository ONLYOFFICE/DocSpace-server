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

namespace ASC.Web.Api.Tests.Tests._05_Security.AuditTrail;

/// <summary>
/// The audit trail is read by administrators who may not be in a private room: it keeps the record of what happened
/// there and names the room neutrally. Its members still read the real titles in the history of the room.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "PrivacyRoom")]
public class AuditPrivateRoomTitleTests(
    AspireAppFixture fixture)
    : AuditTrailTestBase(fixture)
{
    private const string NeutralTitle = "Private room";

    protected override bool NeedsLazyResources => true;

    private async Task<(User Creator, int RoomId, string Title)> CreatePrivateRoomAsync()
    {
        var creator = await InviteContact(EmployeeType.RoomAdmin);
        var title = $"secret-room-{Guid.NewGuid():N}";

        await _filesClient.Authenticate(creator);
        await _privacyRoomApi.SetKeysAsync(
            new EncryptionKeyRequestDto(Guid.Empty, $"pk-{Guid.NewGuid():N}", $"prv-{Guid.NewGuid():N}"),
            TestContext.Current.CancellationToken);

        var room = await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto(title: title, roomType: RoomType.CustomRoom, @private: true),
            TestContext.Current.CancellationToken);

        return (creator, room.Response.Id, title);
    }

    private static bool Concerns(AuditEventDto auditEvent, int id)
    {
        return auditEvent.Target?.Contains(id.ToString()) == true;
    }

    private static void ShouldNotName(IEnumerable<AuditEventDto> events, params string[] titles)
    {
        foreach (var auditEvent in events)
        {
            foreach (var title in titles)
            {
                auditEvent.Action.Should().NotContain(title);
                auditEvent.Context.Should().NotContain(title);
            }
        }
    }

    [Fact]
    public async Task RoomCreated_KeepsTheRecordAndNamesTheRoomNeutrally()
    {
        var (creator, roomId, title) = await CreatePrivateRoomAsync();

        await _webApiClient.Authenticate(Owner);
        var events = await PollAuditEventsByFilterAsync(all => all.Exists(e => Concerns(e, roomId)));

        var created = events.Find(e => Concerns(e, roomId));
        created.Should().NotBeNull("the record stays, only the title is hidden");
        created!.UserId.Should().Be(creator.Id);
        created.Context.Should().Contain(NeutralTitle);
        ShouldNotName(events, title);
    }

    [Fact]
    public async Task RoomRenamed_HidesTheOldTitleAndTheNewOne()
    {
        var (_, roomId, title) = await CreatePrivateRoomAsync();
        var newTitle = $"secret-renamed-{Guid.NewGuid():N}";

        await _roomsApi.UpdateRoomAsync(roomId, new UpdateRoomRequest(title: newTitle), TestContext.Current.CancellationToken);

        await _webApiClient.Authenticate(Owner);
        var events = await PollAuditEventsByFilterAsync(all => all.Count(e => Concerns(e, roomId)) >= 2);

        events.Count(e => Concerns(e, roomId)).Should().BeGreaterThanOrEqualTo(2);
        ShouldNotName(events, title, newTitle);
    }

    [Fact]
    public async Task FolderCreatedAndMoved_HideTheFoldersAndTheRoom()
    {
        var (_, roomId, title) = await CreatePrivateRoomAsync();
        var folderTitle = $"secret-folder-{Guid.NewGuid():N}";
        var targetTitle = $"secret-target-{Guid.NewGuid():N}";
        var cancellationToken = TestContext.Current.CancellationToken;

        var folder = (await _foldersApi.CreateFolderAsync(roomId, new CreateFolderRequest(folderTitle), cancellationToken)).Response;
        var target = (await _foldersApi.CreateFolderAsync(roomId, new CreateFolderRequest(targetTitle), cancellationToken)).Response;

        await _operationsApi.MoveBatchItemsAsync(
            new BatchRequestDto { FolderIds = [new(folder.Id)], DestFolderId = new(target.Id) },
            cancellationToken);

        await _webApiClient.Authenticate(Owner);
        var events = await PollAuditEventsByFilterAsync(all => all.Exists(e => e.Target?.Contains(target.Id.ToString()) == true && e.Target.Contains(folder.Id.ToString())));

        events.Should().Contain(e => e.Target != null && e.Target.Contains(folder.Id.ToString()) && e.Target.Contains(target.Id.ToString()), "the move is on record");
        ShouldNotName(events, title, folderTitle, targetTitle);
    }

    [Fact]
    public async Task MemberInvited_KeepsTheNameOfTheMemberAndHidesTheRoom()
    {
        var (creator, roomId, title) = await CreatePrivateRoomAsync();
        var member = await InviteContact(EmployeeType.User);
        var cancellationToken = TestContext.Current.CancellationToken;

        await _filesClient.Authenticate(member);
        await _privacyRoomApi.SetKeysAsync(
            new EncryptionKeyRequestDto(Guid.Empty, $"pk-{Guid.NewGuid():N}", $"prv-{Guid.NewGuid():N}"),
            cancellationToken);

        await _filesClient.Authenticate(creator);
        await _roomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest(invitations: [new RoomInvitation(member.Id, DocSpace.API.SDK.Model.FileShare.Read)], notify: false),
            cancellationToken);

        await _webApiClient.Authenticate(Owner);
        var events = await PollAuditEventsByFilterAsync(all => all.Exists(IsMemberAdded));

        var added = events.Find(IsMemberAdded);
        added.Should().NotBeNull("the invitation is on record");
        added!.Action.Should().NotContain(NeutralTitle, "only the titles are hidden, the name of the member stays");
        added.Action.Should().MatchRegex("^Added user: .+\\. Role: .+$");
        ShouldNotName(events, title);

        bool IsMemberAdded(AuditEventDto e) => Concerns(e, roomId) && e.Action?.StartsWith("Added user") == true;
    }

    [Fact]
    public async Task RoomQuotaChanged_HidesTheRoomInTheList()
    {
        var (_, roomId, title) = await CreatePrivateRoomAsync();
        var cancellationToken = TestContext.Current.CancellationToken;

        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestDto(true, new QuotaSettingsRequestDtoDefaultQuota(512 * 1024 * 1024)), cancellationToken);

        var filesQuotaApi = new DocSpace.API.SDK.Api.Files.QuotaApi(_filesClient, new Configuration { BasePath = _filesClient.BaseAddress!.ToString().TrimEnd('/') });

        await filesQuotaApi.UpdateRoomsQuotaAsync(
            new UpdateRoomsQuotaRequestDto(roomIds: [new(roomId)], quota: 1024L * 1024 * 1024),
            cancellationToken);

        var events = await PollAuditEventsByFilterAsync(all => all.Exists(e => e.Action?.Contains(NeutralTitle) == true));

        events.Should().Contain(e => e.Action != null && e.Action.Contains(NeutralTitle), "the change of the quota is on record");
        ShouldNotName(events, title);
    }

    [Fact]
    public async Task OrdinaryRoom_KeepsItsTitle()
    {
        var title = $"ordinary-room-{Guid.NewGuid():N}";

        await _filesClient.Authenticate(Owner);
        var room = (await _roomsApi.CreateRoomAsync(new CreateRoomRequestDto(title: title, roomType: RoomType.CustomRoom), TestContext.Current.CancellationToken)).Response;

        await _webApiClient.Authenticate(Owner);
        var events = await PollAuditEventsByFilterAsync(all => all.Exists(e => Concerns(e, room.Id)));

        events.Find(e => Concerns(e, room.Id))!.Context.Should().Contain(title);
    }

    [Fact]
    public async Task HistoryOfTheRoom_StillShowsItsMembersTheTitle()
    {
        var (_, roomId, title) = await CreatePrivateRoomAsync();
        var folderTitle = $"secret-folder-{Guid.NewGuid():N}";

        await _foldersApi.CreateFolderAsync(roomId, new CreateFolderRequest(folderTitle), TestContext.Current.CancellationToken);

        // raw: what a history entry says is in an untyped data object of the model
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var history = string.Empty;

        while (!history.Contains(folderTitle) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500, TestContext.Current.CancellationToken);

            using var response = await new RawApiClient(_filesClient).GetAsync($"api/2.0/files/folder/{roomId}/log", TestContext.Current.CancellationToken);
            history = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        }

        history.Should().Contain(folderTitle);
    }
}
