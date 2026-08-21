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

using QuotaSettingsRequestsDto = DocSpace.API.SDK.Model.QuotaSettingsRequestsDto;

namespace ASC.Files.Tests.Tests._03_Rooms.Create;

/// <summary>
/// The optional fields of <c>POST /files/rooms</c>: quota, indexing, deny-download, lifetime,
/// watermark, tags, color and cover. Each is verified either straight from the create response or,
/// where the endpoint deliberately omits it (quota), through a follow-up <c>getRoomInfo</c>.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "RoomCreate")]
public class RoomCreateOptionalFieldsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task CreateRoom_CustomQuota_VerifiedViaGetRoomInfo()
    {
        // Arrange
        // Per-room quota must be enabled portal-wide first, otherwise the quota in createRoom is ignored.
        // SaveRoomQuotaSettings is served by Web.Api, which carries its own auth header - authenticating
        // _filesClient alone is not enough.
        const long myQuota = 10 * 1024 * 1024;
        await _webApiClient.Authenticate(Owner);
        await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(100 * 1024 * 1024)),
            TestContext.Current.CancellationToken);

        // Act
        // createRoom's own response omits quotaLimit even when quota is set - verify via getRoomInfo.
        var created = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Quota", roomType: RoomType.CustomRoom, quota: myQuota),
            TestContext.Current.CancellationToken)).Response;

        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.QuotaLimit.Should().Be(myQuota);
    }

    [Fact]
    public async Task CreateRoom_Vdr_IndexingEnabled()
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Indexing", roomType: RoomType.VirtualDataRoom, indexing: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Indexing.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoom_Vdr_DenyDownload()
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest DenyDownload", roomType: RoomType.VirtualDataRoom, denyDownload: true),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.DenyDownload.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRoom_Vdr_LifetimeSettings_VerifiedViaGetRoomInfo()
    {
        // Arrange
        // `enabled` is not part of the working request shape here; period+value+deletePermanently is
        // enough to turn lifetime on.
        var lifetime = new RoomDataLifetimeDto(period: RoomDataLifetimePeriod.Day, value: 30, deletePermanently: false);

        // Act
        var created = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Lifetime", roomType: RoomType.VirtualDataRoom, lifetime: lifetime),
            TestContext.Current.CancellationToken)).Response;

        var info = (await _roomsApi.GetRoomInfoAsync(created.Id, TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Lifetime.Period.Should().Be(RoomDataLifetimePeriod.Day);
        info.Lifetime.Value.Should().Be(30);
        info.Lifetime.DeletePermanently.Should().BeFalse();
    }

    [Fact]
    public async Task CreateRoom_Vdr_Watermark()
    {
        // Arrange
        var watermark = new WatermarkRequestDto(enabled: true, text: "Confidential", rotate: -45);

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Watermark", roomType: RoomType.VirtualDataRoom, watermark: watermark),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Watermark.Text.Should().Be("Confidential");
    }

    [Fact]
    public async Task CreateRoom_WithTags_AttachedToRoom()
    {
        // Arrange
        List<string> tags = ["Autotest Alpha", "Autotest Beta"];

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Tags", roomType: RoomType.CustomRoom, tags: tags),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Tags.Should().Contain(tags);
    }

    [Fact]
    public async Task CreateRoom_TagsCreatedDuringCreation_AppearInTagList()
    {
        // Arrange
        var stamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        List<string> tags = [$"autotest-create-{stamp}-a", $"autotest-create-{stamp}-b"];

        // Act
        await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest TagsAutoCreate", roomType: RoomType.CustomRoom, tags: tags),
            TestContext.Current.CancellationToken);

        var allTags = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken))
            .Response.ConvertAll(t => t?.ToString());

        // Assert
        allTags.Should().Contain(tags);
    }

    [Fact]
    public async Task CreateRoom_DuplicateTagsInRequest_Deduplicated()
    {
        // Arrange
        List<string> tags = ["autotest-dup", "autotest-dup", "autotest-dup"];

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest DupTags", roomType: RoomType.CustomRoom, tags: tags),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Tags.Should().ContainSingle(t => t == "autotest-dup");
    }

    [Fact]
    public async Task CreateRoom_WithColor_AppliedToLogo()
    {
        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Color", roomType: RoomType.CustomRoom, color: "FF5733"),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Logo.Color.Should().Be("FF5733");
    }

    [Fact]
    public async Task CreateRoom_WithCover_AppliedToLogo()
    {
        // Arrange
        var coverId = await GetFirstCoverId();

        // Act
        var room = (await _roomsApi.CreateRoomAsync(
            new CreateRoomRequestDto("Autotest Cover", roomType: RoomType.CustomRoom, cover: coverId),
            TestContext.Current.CancellationToken)).Response;

        // Assert
        room.Logo.Cover.Id.Should().Be(coverId);
    }

    /// <summary>
    /// The `share` parameter is exposed by the generated SDK/OpenAPI document but has no backing
    /// implementation on createRoom. Today the server accepts it and silently drops it (200); it
    /// should instead reject a field it does not support.
    /// </summary>
    [Fact]
    [Trait("Bug", "81582")]
    public async Task CreateRoom_UndocumentedShareParameter_Rejected()
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.CreateRoomAsync(
                new CreateRoomRequestDto(
                    "Autotest ShareOnCreate",
                    roomType: RoomType.CustomRoom,
                    share: [new FileShareParams(member.Id, FileShare.Editing)]),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
