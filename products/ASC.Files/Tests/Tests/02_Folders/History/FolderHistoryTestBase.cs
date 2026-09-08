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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// Shared setup for the folder/room history suites (<c>GET /files/folder/{folderId}/log</c>,
/// <c>POST /files/folder/{folderId}/log/report</c>): polling a room's history for a specific
/// action, since the write racing the request that triggered it is the one thing every "history
/// contains X after Y" test in this feature has in common.
/// </summary>
public abstract class FolderHistoryTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>The minimal valid 1x1 PNG used for the room-logo history tests.</summary>
    private const string TestImagePngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==";

    /// <summary>
    /// Polls a room's history until an entry for <paramref name="action"/> shows up, since history
    /// entries for background-triggered events are written after the request that caused them
    /// returns. Returns the last observed entry (<c>null</c> if none) so a timed-out assertion still
    /// shows what was actually there.
    /// </summary>
    protected async Task<HistoryDto?> PollHistoryEntryAsync(int roomId, MessageAction action, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        HistoryDto? entry;

        while (true)
        {
            var history = (await _foldersApi.GetFolderHistoryAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;
            entry = history.Find(e => e.Action?.Id == action);

            if (entry is not null || DateTime.UtcNow >= deadline)
            {
                return entry;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls a room's history until it holds at least <paramref name="count"/> entries. Paging tests
    /// need this: audit entries are written asynchronously, so reading two pages while an entry is
    /// still landing shifts the window between the calls and the second page returns the entry the
    /// first one just returned - which looks exactly like <c>startIndex</c> being ignored.
    /// </summary>
    protected async Task<List<HistoryDto>> PollHistoryCountAsync(int roomId, int count, int timeoutSeconds = 15)
    {
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (true)
        {
            var history = (await _foldersApi.GetFolderHistoryAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (history.Count >= count || DateTime.UtcNow >= deadline)
            {
                return history;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>
    /// Polls until <paramref name="action"/> appears in <paramref name="roomId"/>'s history and
    /// asserts that it does, optionally also asserting the initiator's display name. This is the
    /// shape of every "history contains X after Y" test in the suite.
    /// </summary>
    protected async Task<HistoryDto> AssertHistoryContainsAsync(int roomId, MessageAction action, string? initiatorDisplayName = null, int timeoutSeconds = 15)
    {
        var entry = await PollHistoryEntryAsync(roomId, action, TimeSpan.FromSeconds(timeoutSeconds));

        entry.Should().NotBeNull($"the room history should contain an entry for {action} within {timeoutSeconds}s");

        if (initiatorDisplayName is not null)
        {
            entry!.Initiator.DisplayName.Should().Be(initiatorDisplayName);
        }

        return entry!;
    }

    /// <summary>Reads a room's current history action ids, for "does not contain yet" assertions.</summary>
    protected async Task<List<MessageAction?>> GetHistoryActionIdsAsync(int roomId)
    {
        var history = (await _foldersApi.GetFolderHistoryAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return history.ConvertAll(e => e.Action?.Id);
    }

    /// <summary>
    /// The display name history entries are attributed to for <paramref name="user"/>, the owner by
    /// default. <c>_profilesApi</c> is bound to <c>_peopleClient</c>, which carries its own auth
    /// header and is not signed in by <c>BaseTest.InitializeAsync</c> - only <c>_filesClient</c> is -
    /// so the identity has to be named here rather than read off whoever <c>_filesClient</c> happens
    /// to be.
    /// </summary>
    protected async Task<string> GetDisplayNameAsync(User? user = null)
    {
        await _peopleClient.Authenticate(user ?? Owner);

        return (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response.DisplayName;
    }

    /// <summary>Uploads the suite's 1x1 test PNG and sets it as a room's logo.</summary>
    protected async Task SetRoomLogoAsync(int roomId)
    {
        await using var stream = new MemoryStream(Convert.FromBase64String(TestImagePngBase64));

        var uploaded = (await _roomsApi.UploadRoomLogoAsync(
            new FileParameter("logo.png", "image/png", stream),
            TestContext.Current.CancellationToken)).Response;

        await _roomsApi.CreateRoomLogoAsync(
            roomId,
            new LogoRequest(uploaded.Data?.ToString() ?? string.Empty, 0, 0, 1, 1),
            TestContext.Current.CancellationToken);
    }

    /// <summary>Restores an archived room and waits for the asynchronous operation to finish.</summary>
    protected async Task UnarchiveRoomAsync(int roomId)
    {
        await _roomsApi.UnarchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken);
        await WaitLongOperation();
    }

    /// <summary>
    /// Changes a member's employee type through the People <c>UserTypeApi</c>, which this suite's
    /// <see cref="ASC.Files.Tests.ApiFactories.PortalClients"/> does not wire up (only
    /// <c>ProfilesApi</c>/<c>GroupApi</c>/<c>UserStatusApi</c>/<c>PhotosApi</c> are, mirroring
    /// <see cref="BaseTest._peopleClient"/>'s other ad-hoc clients). Built against the shared
    /// people client, so the caller's current authentication applies.
    /// </summary>
    protected async Task UpdateUserTypeAsync(Guid userId, EmployeeType type)
    {
        var config = new Configuration { BasePath = _peopleClient.BaseAddress!.ToString().TrimEnd('/') };
        var userTypeApi = new DocSpace.API.SDK.Api.People.UserTypeApi(_peopleClient, config);

        await userTypeApi.UpdateUserTypeAsync(type, new UpdateMembersRequestDto([userId], resendAll: false), TestContext.Current.CancellationToken);
    }
}
