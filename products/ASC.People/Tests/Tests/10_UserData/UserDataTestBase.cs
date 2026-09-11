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

namespace ASC.People.Tests.Tests._10_UserData;

/// <summary>
/// Shared setup and polling helpers for the reassign/remove data-lifecycle endpoints exposed by
/// <see cref="DocSpace.API.SDK.Api.People.UserDataApi"/>: giving an actor a room and a file whose
/// ownership later moves (or is dropped), and polling the asynchronous reassign/remove progress
/// endpoints on a deadline instead of reading them once right after the write.
/// </summary>
public abstract class UserDataTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    private static readonly TimeSpan _pollTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Only RoomAdmin and DocSpaceAdmin can invite through <see cref="BaseTest.InviteContact"/> and
    /// still be room owners themselves, so the theories below only ever parameterise this pair.
    /// </summary>
    public static TheoryData<EmployeeType> NonAdminRoles => new() { EmployeeType.RoomAdmin, EmployeeType.User, EmployeeType.Guest };

    /// <summary>
    /// Signs the given member in and has them create a room with one file - the data a later
    /// reassign/remove call moves or drops. Leaves <see cref="BaseTest._filesClient"/> authenticated
    /// as that member; callers that need the Owner back must re-authenticate explicitly.
    /// </summary>
    protected async Task<FolderDtoInteger> CreateOwnRoomWithFileAsync(User owner, string roomTitle, string fileTitle)
    {
        await _filesClient.Authenticate(owner);
        var room = await CreateCustomRoom(roomTitle);
        await _filesApi.CreateFileAsync(room.Id, new CreateFileJsonElement(fileTitle), TestContext.Current.CancellationToken);

        return room;
    }

    /// <summary>
    /// Invites a new member of <paramref name="type"/> and gives them a room and a file of their
    /// own, then restores <see cref="BaseTest._filesClient"/> to the Owner.
    /// </summary>
    protected async Task<(User Actor, FolderDtoInteger Room)> CreateActorWithRoomAndFileAsync(EmployeeType type, string roomTitle, string fileTitle)
    {
        var actor = await InviteContact(type);
        var room = await CreateOwnRoomWithFileAsync(actor, roomTitle, fileTitle);

        await _filesClient.Authenticate(Owner);

        return (actor, room);
    }

    /// <summary>
    /// The Id/DisplayName of a member, read as the Owner - used to assert room ownership
    /// (<c>FileEntryBaseDto.OwnedBy</c>, an <see cref="EmployeeDto"/>) against an invited actor.
    /// </summary>
    protected async Task<EmployeeFullDto> GetProfileAsync(Guid userId)
    {
        await _peopleClient.Authenticate(Owner);

        return (await _profilesApi.GetProfileByUserIdAsync(userId.ToString(), TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>
    /// The room whose title matches <paramref name="roomTitle"/>. Read through the folder listing
    /// rather than a by-id lookup because <c>OwnedBy</c> lives only on the listing's
    /// <c>FileEntryBaseDto</c> entries - see the "endpoints the SDK does not expose" note on
    /// <c>FolderContentDtoInteger.Folders</c>. Must be called as a caller allowed to see the room
    /// (the Owner, here).
    /// </summary>
    protected async Task<FileEntryBaseDto> FindRoomByTitleAsync(string roomTitle)
    {
        var content = (await _roomsApi.GetRoomsFolderAsync(filterValue: roomTitle, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return content.Folders.Single(f => f.Title == roomTitle);
    }

    /// <summary>
    /// Polls <paramref name="fetch"/> on a deadline until <paramref name="until"/> is satisfied,
    /// returning the last observed state either way so a failing assertion shows what was actually
    /// there instead of a bare timeout.
    /// </summary>
    protected static async Task<TaskProgressResponseDto> PollUntilAsync(
        Func<Task<TaskProgressResponseDto>> fetch,
        Func<TaskProgressResponseDto, bool> until)
    {
        var deadline = DateTime.UtcNow + _pollTimeout;
        TaskProgressResponseDto last;

        do
        {
            last = await fetch();

            if (until(last))
            {
                return last;
            }

            await Task.Delay(_pollInterval, TestContext.Current.CancellationToken);
        } while (DateTime.UtcNow < deadline);

        return last;
    }
}
