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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Notifications;

/// <summary>
/// Whose notification settings a call reads and changes: every kind is on for an account that never touched it,
/// switching a kind off is the caller's own choice only, and the muted-room list adds and removes an id as given.
/// </summary>
[Trait("Category", "Settings")]
public class NotificationSettingsScopeTests(
    AspireAppFixture fixture)
    : NotificationsTestBase(fixture)
{
    [Theory]
    [InlineData(NotificationType.Badges)]
    [InlineData(NotificationType.RoomsActivity)]
    [InlineData(NotificationType.DailyFeed)]
    [InlineData(NotificationType.UsefullTips)]
    public async Task GetNotificationSettings_FreshMember_ReadsEnabled(NotificationType type)
    {
        await AuthenticateAsAsync(NotificationActor.User);

        var settings = await _notificationsApi.GetNotificationSettingsAsync(type, TestContext.Current.CancellationToken);

        settings.Response.IsEnabled.Should().BeTrue("no kind of notification starts switched off");
    }

    [Theory]
    [InlineData(NotificationType.Badges)]
    [InlineData(NotificationType.RoomsActivity)]
    [InlineData(NotificationType.DailyFeed)]
    [InlineData(NotificationType.UsefullTips)]
    public async Task SetNotificationSettings_Off_LeavesOtherMembersOn(NotificationType type)
    {
        await AuthenticateAsAsync(NotificationActor.RoomAdmin);

        await _notificationsApi.SetNotificationSettingsAsync(
            new NotificationSettingsRequestDto(type, false), TestContext.Current.CancellationToken);

        var own = await _notificationsApi.GetNotificationSettingsAsync(type, TestContext.Current.CancellationToken);

        await AuthenticateAsAsync(NotificationActor.User);

        var other = await _notificationsApi.GetNotificationSettingsAsync(type, TestContext.Current.CancellationToken);

        own.Response.IsEnabled.Should().BeFalse();
        other.Response.IsEnabled.Should().BeTrue("the setting belongs to the member who switched it off");
    }

    [Fact]
    public async Task SetRoomsNotificationStatus_MuteTwiceThenUnmute_ListsTheIdOnceThenNotAtAll()
    {
        await AuthenticateAsAsync(NotificationActor.User);
        var roomId = Random.Shared.Next(1, int.MaxValue);

        await _notificationsApi.SetRoomsNotificationStatusAsync(
            new RoomsNotificationsSettingsRequestDto(roomId, true), TestContext.Current.CancellationToken);

        var mutedTwice = await _notificationsApi.SetRoomsNotificationStatusAsync(
            new RoomsNotificationsSettingsRequestDto(roomId, true), TestContext.Current.CancellationToken);

        var unmuted = await _notificationsApi.SetRoomsNotificationStatusAsync(
            new RoomsNotificationsSettingsRequestDto(roomId, false), TestContext.Current.CancellationToken);

        var listed = await _notificationsApi.GetRoomsNotificationSettingsAsync(TestContext.Current.CancellationToken);

        mutedTwice.Response.DisabledRooms.Where(r => r.ToString() == roomId.ToString()).Should().ContainSingle();
        unmuted.Response.DisabledRooms.Should().NotContain(r => r.ToString() == roomId.ToString());
        listed.Response.DisabledRooms.Should().NotContain(r => r.ToString() == roomId.ToString());
    }

    [Fact]
    public async Task SetRoomsNotificationStatus_Mute_IsTheCallersOwnList()
    {
        await AuthenticateAsAsync(NotificationActor.RoomAdmin);
        var roomId = Random.Shared.Next(1, int.MaxValue);

        await _notificationsApi.SetRoomsNotificationStatusAsync(
            new RoomsNotificationsSettingsRequestDto(roomId, true), TestContext.Current.CancellationToken);

        await AuthenticateAsAsync(NotificationActor.User);

        var other = await _notificationsApi.GetRoomsNotificationSettingsAsync(TestContext.Current.CancellationToken);

        other.Response.DisabledRooms.Should().NotContain(r => r.ToString() == roomId.ToString());
    }
}
