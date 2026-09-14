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

namespace ASC.People.Tests.Tests._14_GroupSearch;

/// <summary>
/// Shared setup for <c>GET /api/2.0/group/{file|folder|room}/{id}</c>: creating the group and the
/// shared entity, and sharing one with the other. These three endpoints read straight from
/// <c>SecurityDao</c> (plain SQL, see <c>products/ASC.Files/Core/Core/Dao/TeamlabDao/SecurityDao.cs</c>),
/// not from a search index, so a read right after the write that is supposed to make it visible is
/// safe - no polling helper is needed here.
/// </summary>
public abstract class GroupSearchTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
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
            new CreateFileJsonElement(title ?? "Autotest File " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);

        return file.Response.Id;
    }

    protected async Task<int> CreateFolderInMyDocumentsAsync(User? actor = null, string? title = null)
    {
        await _filesClient.Authenticate(actor ?? Owner);

        var myDocs = await _foldersApi.GetMyFolderAsync(cancellationToken: TestContext.Current.CancellationToken);
        var folder = await _foldersApi.CreateFolderAsync(
            myDocs.Response.Current.Id,
            new CreateFolder(title ?? "Autotest Folder " + Guid.NewGuid().ToString()[..8]),
            TestContext.Current.CancellationToken);

        return folder.Response.Id;
    }

    protected async Task<int> CreateCustomRoomAsync(User? actor = null, string? title = null)
    {
        await _filesClient.Authenticate(actor ?? Owner);

        var room = await CreateCustomRoom(title ?? "Autotest Room " + Guid.NewGuid().ToString()[..8]);

        return room.Id;
    }

    /// <summary>
    /// Shares a file with a group. The caller decides who is doing the sharing by having already
    /// authenticated <c>_filesClient</c> as that actor (typically as a side effect of
    /// <see cref="CreateFileInMyDocumentsAsync"/>).
    /// </summary>
    protected Task ShareFileWithGroupAsync(int fileId, Guid groupId, FileShare access)
    {
        return _sharingApi.SetFileSecurityInfoAsync(
            fileId,
            new SecurityInfoSimpleRequestDto([new FileShareParams(groupId, access)], notify: false),
            TestContext.Current.CancellationToken);
    }

    protected Task ShareFolderWithGroupAsync(int folderId, Guid groupId, FileShare access)
    {
        return _sharingApi.SetFolderSecurityInfoAsync(
            folderId,
            new SecurityInfoSimpleRequestDto([new FileShareParams(groupId, access)], notify: false),
            TestContext.Current.CancellationToken);
    }
}
