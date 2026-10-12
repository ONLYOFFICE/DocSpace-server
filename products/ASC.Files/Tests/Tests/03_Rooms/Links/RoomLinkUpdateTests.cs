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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// PUT /files/rooms/{id}/links — updating (by <c>linkId</c>) and deleting (<c>access: None</c>) an
/// existing link, plus the field behaviours (password, expiration, denyDownload, internal,
/// maxUseCount) and the side effects a link operation must NOT have on the room itself.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkUpdateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SetRoomLink_OwnerUpdatesExternalLinkByLinkId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Update External");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "External Before", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "External After", denyDownload: true, @internal: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.SharedLink.Id.Should().Be(created.SharedLink.Id);
        updated.SharedLink.Title.Should().Be("External After");
        updated.SharedLink.DenyDownload.Should().BeTrue();
        updated.SharedLink.Internal.Should().BeTrue();

        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        var link = links.Single(l => l.SharedLink.Id == created.SharedLink.Id);
        link.SharedLink.Title.Should().Be("External After");
        link.SharedLink.DenyDownload.Should().BeTrue();
        link.SharedLink.Internal.Should().BeTrue();
    }

    [Fact]
    public async Task SetRoomLink_OwnerDeletesExternalLinkViaAccessNone()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Delete External");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "External To Delete", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, title: "External To Delete", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Select(l => l.SharedLink.Id).Should().NotContain(created.SharedLink.Id);
    }

    [Fact]
    public async Task SetRoomLink_OwnerUpdatesInvitationLinkByLinkId()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Update Invitation");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "Invitation Before", denyDownload: false, maxUseCount: 3),
            TestContext.Current.CancellationToken)).Response;

        // Act
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.Invitation, title: "Invitation After", denyDownload: true, maxUseCount: 10),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.SharedLink.Id.Should().Be(created.SharedLink.Id);
        updated.SharedLink.Title.Should().Be("Invitation After");
        updated.SharedLink.MaxUseCount.Should().Be(10);

        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        var link = links.Single(l => l.SharedLink.Id == created.SharedLink.Id);
        link.SharedLink.Title.Should().Be("Invitation After");
        link.SharedLink.MaxUseCount.Should().Be(10);
    }

    [Fact]
    public async Task SetRoomLink_PasswordIsReflectedOnCreateAndUpdate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Password");

        // Act — create
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Password Link", password: "Secret123!", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert — create
        created.SharedLink.Password.Should().NotBeNullOrEmpty();

        // Act — update
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "Password Link", password: "Updated456!", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert — update
        updated.SharedLink.Password.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SetRoomLink_FutureExpirationDateIsReflected_PastIsSilentlyIgnored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Expiration");

        // Act — create with a future expiration date
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(
                access: FileShare.Read,
                linkType: LinkType.External,
                title: "Expiry Link",
                denyDownload: false,
                expirationDate: new ApiDateTime { UtcTime = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc) }),
            TestContext.Current.CancellationToken)).Response;

        // Assert — future date reflected
        created.SharedLink.IsExpired.Should().BeFalse();
        created.SharedLink.ExpirationDate.Should().NotBeNull();

        // Act — update with a past expiration date
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(
                linkId: created.SharedLink.Id,
                access: FileShare.Read,
                linkType: LinkType.External,
                title: "Expiry Link",
                denyDownload: false,
                expirationDate: new ApiDateTime { UtcTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc) }),
            TestContext.Current.CancellationToken)).Response;

        // Assert — past date silently ignored
        updated.SharedLink.ExpirationDate.Should().BeNull();
    }

    [Fact]
    public async Task SetRoomLink_DenyDownloadTogglesFromTrueToFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink DenyDownload");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "DenyDownload Link", denyDownload: true),
            TestContext.Current.CancellationToken)).Response;
        created.SharedLink.DenyDownload.Should().BeTrue();

        // Act
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "DenyDownload Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.SharedLink.DenyDownload.Should().BeFalse();
    }

    [Fact]
    public async Task SetRoomLink_InternalTogglesFromTrueToFalse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Internal");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Internal Link", @internal: true, denyDownload: false),
            TestContext.Current.CancellationToken)).Response;
        created.SharedLink.Internal.Should().BeTrue();

        // Act
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "Internal Link", @internal: false, denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.SharedLink.Internal.Should().BeFalse();
    }

    [Fact]
    public async Task SetRoomLink_MaxUseCountIsSaved_CurrentUseCountStaysZeroOnUpdate()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink maxUseCount");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: "MaxUse Link", denyDownload: false, maxUseCount: 5),
            TestContext.Current.CancellationToken)).Response;
        created.SharedLink.MaxUseCount.Should().Be(5);
        created.SharedLink.CurrentUseCount.Should().Be(0);

        // Act — a client-supplied currentUseCount must not be trusted
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.Invitation, title: "MaxUse Link", denyDownload: false, maxUseCount: 8, currentUseCount: 99),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        updated.SharedLink.MaxUseCount.Should().Be(8);
        updated.SharedLink.CurrentUseCount.Should().Be(0);
    }

    [Fact]
    public async Task SetRoomLink_DeletingAnAlreadyDeletedLink_IsIdempotent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Double Delete");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Double Delete Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, title: "Double Delete Link", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Act — deleting again
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, title: "Double Delete Link", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Select(l => l.SharedLink.Id).Should().NotContain(created.SharedLink.Id);
    }

    [Fact]
    public async Task SetRoomLink_ChangingLinkTypeOnUpdate_IsIgnored()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink LinkType Switch");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Switch Me", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act — try to switch it to an Invitation link
        var updated = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.Invitation, title: "Switch Me", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Assert — the link keeps its original type
        updated.SharedLink.LinkType.Should().Be(LinkType.External);
    }

    [Fact]
    public async Task SetRoomLink_LinkOperations_DoNotChangeRoomMembers()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Members Intact");

        var before = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Members Intact Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, title: "Members Intact Link", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var after = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Should().HaveCount(before.Count);
        after.Should().Contain(s => s.IsOwner);
    }

    [Fact]
    public async Task SetRoomLink_LinkCreation_DoesNotChangeRoomTitleOrType()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Metadata Intact");

        // Act
        await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Metadata Link", denyDownload: false),
            TestContext.Current.CancellationToken);

        // Assert
        var info = (await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken)).Response;
        info.Title.Should().Be("Autotest setLink Metadata Intact");
        info.RoomType.Should().Be(RoomType.CustomRoom);
    }

    [Fact]
    public async Task SetRoomLink_RepeatedIdenticalUpdate_KeepsASingleLink()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Idempotent Update");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Idempotent Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        var body = new RoomLinkRequest(
            linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "Idempotent Link Updated", denyDownload: true);

        // Act
        var first = (await _roomsApi.SetRoomLinkAsync(room.Id, body, TestContext.Current.CancellationToken)).Response;
        var second = (await _roomsApi.SetRoomLinkAsync(room.Id, body, TestContext.Current.CancellationToken)).Response;

        // Assert
        first.SharedLink.Id.Should().Be(created.SharedLink.Id);
        second.SharedLink.Id.Should().Be(created.SharedLink.Id);

        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Count(l => l.SharedLink.Id == created.SharedLink.Id).Should().Be(1);
    }

    [Fact]
    public async Task SetRoomLink_ParallelExternalCreation_YieldsUniqueLinks()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Parallel External");

        // Act
        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(i => _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: $"Parallel External {i}", denyDownload: false),
            TestContext.Current.CancellationToken)));

        // Assert
        results.Should().AllSatisfy(r => r.Response.SharedLink.Id.Should().NotBeEmpty());

        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        var ids = links.Select(l => l.SharedLink.Id).ToList();
        ids.Distinct().Should().HaveCount(ids.Count);
        ids.Should().HaveCount(3);
    }

    /// <summary>
    /// The Invitation-link singleton cap is enforced even under a race: every create past the first
    /// is rejected with 403 (same "maximum number of links" error as the sequential case in
    /// <c>RoomLinkCreateTests.SetRoomLink_SecondInvitationLinkWithoutLinkId_IsRejectedAsSingleton</c>),
    /// so concurrent creates must tolerate that failure rather than expect every call to succeed.
    /// </summary>
    [Fact]
    public async Task SetRoomLink_ParallelInvitationCreation_EnforcesSingleLinkCap()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Parallel Invitation");

        // Act — fire three concurrent creates; the product's singleton cap rejects all but one, even
        // under a race, rather than letting duplicates through.
        var results = await Task.WhenAll(Enumerable.Range(0, 3).Select(async i =>
        {
            try
            {
                await _roomsApi.SetRoomLinkAsync(
                    room.Id,
                    new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.Invitation, title: $"Parallel Invitation {i}", denyDownload: false),
                    TestContext.Current.CancellationToken);
                return true;
            }
            catch (ApiException)
            {
                return false;
            }
        }));

        // Assert — the cap is enforced, and exactly one Invitation link exists afterwards.
        results.Count(succeeded => succeeded).Should().Be(1);
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.Invitation, TestContext.Current.CancellationToken)).Response;
        links.Should().HaveCount(1);
    }

    [Fact]
    public async Task SetRoomLink_ConcurrentUpdateAndDeleteOfSameLink_LeavesConsistentState()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest setLink Concurrent");

        var created = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(access: FileShare.Read, linkType: LinkType.External, title: "Concurrent Link", denyDownload: false),
            TestContext.Current.CancellationToken)).Response;

        // Act
        await Task.WhenAll(
            _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.Read, linkType: LinkType.External, title: "Concurrent Updated", denyDownload: true),
                TestContext.Current.CancellationToken),
            _roomsApi.SetRoomLinkAsync(
                room.Id,
                new RoomLinkRequest(linkId: created.SharedLink.Id, access: FileShare.None, linkType: LinkType.External, title: "Concurrent Link", denyDownload: false),
                TestContext.Current.CancellationToken));

        // Assert — either the update won (one link) or the delete won (zero links), never a
        // duplicated or corrupted entry.
        var links = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Count(l => l.SharedLink.Id == created.SharedLink.Id).Should().BeLessThanOrEqualTo(1);
    }
}
