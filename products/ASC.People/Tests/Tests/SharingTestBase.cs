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

namespace ASC.People.Tests.Tests;

/// <summary>
/// Shared setup for the suites that search who an entry can be shared with - the group search and the
/// share candidates: creating the groups, the entries in My documents and the rooms they share.
/// </summary>
public abstract class SharingTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// The system group of every account of the portal, the one group a caller that may not read the
    /// groups of the portal is still offered when sharing.
    /// </summary>
    protected static Guid EveryoneGroupId { get; } = new("c5cc67d1-c3e8-43c0-a3ad-3928ae3e5b5e");

    /// <summary>
    /// Always created by the owner - group membership/management is portal-wide, not scoped to
    /// whichever actor the test is currently impersonating.
    /// </summary>
    protected async Task<GroupDto> CreateGroupAsync(Guid? manager = null, List<Guid>? members = null, string? name = null)
    {
        await _peopleClient.Authenticate(Owner);

        var request = new GroupRequestDto(
            members: members ?? [],
            groupManager: manager ?? Owner.Id,
            groupName: name ?? "Autotest Group " + Guid.NewGuid().ToString()[..8]);

        return (await _groupApi.AddGroupAsync(request, TestContext.Current.CancellationToken)).Response;
    }

    protected async Task<int> CreateFileInMyDocumentsAsync(User? actor = null, string? title = null)
    {
        await _filesClient.Authenticate(actor ?? Owner);

        var file = await _filesApi.CreateFileInMyDocumentsAsync(
            new CreateFileRequest(title ?? "Autotest File " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);

        return file.Response.Id;
    }

    protected async Task<int> CreateFolderInMyDocumentsAsync(User? actor = null, string? title = null)
    {
        await _filesClient.Authenticate(actor ?? Owner);

        var myDocs = await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken);
        var folder = await _foldersApi.CreateFolderAsync(
            myDocs.Response.Current.Id,
            new CreateFolderRequest(title ?? "Autotest Folder " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);

        return folder.Response.Id;
    }

    protected async Task<int> CreateCustomRoomAsync(User? actor = null, string? title = null)
    {
        await _filesClient.Authenticate(actor ?? Owner);

        var room = await CreateCustomRoom(title ?? "Autotest Room " + Guid.NewGuid().ToString()[..8]);

        return room.Id;
    }
}
