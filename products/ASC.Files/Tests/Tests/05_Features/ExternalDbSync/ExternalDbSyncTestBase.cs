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

namespace ASC.Files.Tests.Tests._05_Features.ExternalDbSync;

/// <summary>
/// Shared setup for the external DB sync suites (<c>POST/GET /files/rooms/{id}/externaldbsync</c>).
/// Test portals have no external database configured, so only the contract that does not need one
/// is covered: room-type gate, authorisation, unknown room, "no job yet" and "nothing queued after
/// a refusal". A running, completed or failed job cannot be reached without that database.
/// </summary>
public abstract class ExternalDbSyncTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Asserts that the room has no sync job: the endpoint answers 200 with an empty
    /// <c>response</c>.
    /// </summary>
    protected async Task AssertNoJob(int roomId)
    {
        var status = await _roomsApi.GetExternalDbSyncStatusAsync(roomId, TestContext.Current.CancellationToken);

        status?.Response.Should().BeNull("no external database sync job may exist for the room");
    }

    /// <summary>
    /// Invites an existing portal member into a form filling room and verifies the invitation
    /// landed, so a 403 asserted afterwards cannot come from a failed invite.
    /// </summary>
    protected async Task InviteAndConfirm(int roomId, User user, FileShare access)
    {
        await InviteToRoom(roomId, user, access);

        var members = (await _roomsApi.GetRoomSecurityInfoAsync(roomId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        members.Should().Contain(s => s.SharedToUser != null && s.SharedToUser.Id == user.Id && s.Access == access);
    }

    protected async Task<ApiException> ExpectGetRefused(int roomId)
    {
        return await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.GetExternalDbSyncStatusAsync(roomId, TestContext.Current.CancellationToken));
    }

    protected async Task<ApiException> ExpectStartRefused(int roomId)
    {
        return await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.StartExternalDbSyncAsync(roomId, TestContext.Current.CancellationToken));
    }
}
