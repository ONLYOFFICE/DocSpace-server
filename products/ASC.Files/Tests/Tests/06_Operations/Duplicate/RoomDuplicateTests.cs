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

namespace ASC.Files.Tests.Tests._06_Operations.Duplicate;

/// <summary>
/// <c>duplicateBatchItems</c> over a room: the copy lands next to the original with a numeric suffix, carries the
/// room's settings and tags but none of its members, and a Public room copy gets a primary external link of its own.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class RoomDuplicateTests(
    AspireAppFixture fixture)
    : DuplicateTestBase(fixture)
{
    [Fact]
    public async Task DuplicateRoom_VirtualDataRoom_CopyCarriesSettingsTagsAndColourWithASuffix()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = "Autotest Dup VDR " + Guid.NewGuid().ToString()[..8];
        var room = await CreateRoom(new CreateRoomRequestDto(
            title,
            indexing: true,
            denyDownload: true,
            lifetime: new RoomDataLifetimeDto(deletePermanently: false, period: RoomDataLifetimePeriod.Month, value: 7, enabled: true),
            watermark: new WatermarkRequestDto(enabled: true, additions: WatermarkAdditions.UserName, text: "Autotest Mark"),
            tags: ["dup-tag-a", "dup-tag-b"],
            color: "4781D1",
            roomType: RoomType.VirtualDataRoom));

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(folderIds: [room.Id]));

        // Assert
        var copy = await FindCopyAsync(room.Id, title);
        copy.Title.Should().Be($"{title} (1)");
        copy.ParentId.Should().Be(room.ParentId, "the copy lands next to the original");
        copy.RoomType.Should().Be(RoomType.VirtualDataRoom);
        copy.Indexing.Should().BeTrue();
        copy.DenyDownload.Should().BeTrue();
        copy.Lifetime.Should().NotBeNull();
        copy.Lifetime.Value.Should().Be(7);
        copy.Lifetime.Period.Should().Be(RoomDataLifetimePeriod.Month);
        copy.Watermark.Should().NotBeNull();
        copy.Watermark.Text.Should().Be("Autotest Mark");
        copy.Tags.Should().BeEquivalentTo("dup-tag-a", "dup-tag-b");
        copy.Logo.Color.Should().BeEquivalentTo(room.Logo.Color);
    }

    [Fact]
    public async Task DuplicateRoom_CustomRoomWithMember_CopyHasNoMembersButTheCaller()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = "Autotest Dup Members " + Guid.NewGuid().ToString()[..8];
        var room = await CreateCustomRoom(title);
        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(folderIds: [room.Id]));

        // Assert
        var copy = await FindCopyAsync(room.Id, title);
        var shares = (await _roomsApi.GetRoomSecurityInfoAsync(copy.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        shares.Should().NotContain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id, "members of the original are not copied");

        var original = (await _roomsApi.GetRoomSecurityInfoAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        original.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == member.Id, "the original keeps its member");
    }

    [Fact]
    public async Task DuplicateRoom_PublicRoom_CopyGetsAPrimaryExternalLinkOfItsOwn()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var title = "Autotest Dup Public " + Guid.NewGuid().ToString()[..8];
        var room = await CreatePublicRoom(title);
        var originalLinks = (await _roomsApi.GetRoomLinksAsync(room.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;

        // Act
        await DuplicateAndWait(BuildDuplicateRequest(folderIds: [room.Id]));

        // Assert
        var copy = await FindCopyAsync(room.Id, title);
        var links = (await _roomsApi.GetRoomLinksAsync(copy.Id, LinkType.External, TestContext.Current.CancellationToken)).Response;
        links.Should().ContainSingle(l => l.SharedLink != null && l.SharedLink.Primary);
        var copyToken = links.Single(l => l.SharedLink.Primary).SharedLink.RequestToken;
        originalLinks.Select(l => l.SharedLink.RequestToken).Should().NotContain(copyToken, "the copy's link is a new one, not the original's");
    }

    /// <summary>
    /// Polls the Rooms section on a deadline for a room whose title starts with <paramref name="title"/> and whose id
    /// is not the original's, and reads it back through <c>getRoomInfo</c>.
    /// </summary>
    private async Task<FolderDto> FindCopyAsync(int originalId, string title)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var listed = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var copy = (listed.Folders ?? []).OfType<FolderDto>().FirstOrDefault(f => f.Id != originalId && f.Title.StartsWith(title, StringComparison.Ordinal));

            if (copy != null)
            {
                return (await _roomsApi.GetRoomInfoAsync(copy.Id, TestContext.Current.CancellationToken)).Response;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new InvalidOperationException($"No copy of room {originalId} ('{title}') appeared in the Rooms section.");
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }
}
