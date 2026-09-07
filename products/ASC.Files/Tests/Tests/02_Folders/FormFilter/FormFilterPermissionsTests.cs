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

namespace ASC.Files.Tests.Tests._02_Folders.FormFilter;

/// <summary>
/// <c>GET /files/{folderId}/formfilter</c> - access control: any room member who can read the
/// folder at all (down to <see cref="FileShare.Read"/>) can also read its form filter, and the
/// room owner can read the form filter of a folder created by another member.
/// </summary>
public class FormFilterPermissionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetFolder_UserWithReadAccess_ReturnsOk()
    {
        var room = await CreateCustomRoom("Autotest Room For Filter Read Access");
        var folder = await CreateFolder("Autotest Folder Filter Read", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);

        await _filesClient.Authenticate(user);

        var act = async () => await _foldersApi.GetFolderAsync(folder.Id, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetFolder_OwnerReadsAnotherUsersFolder_ReturnsOk()
    {
        var room = await CreateCustomRoom("Autotest Room For Owner Filter Other User");

        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.ContentCreator);

        await _filesClient.Authenticate(user);
        var folder = await CreateFolder("Autotest Folder By User For Owner Filter", room.Id);

        await _filesClient.Authenticate(Owner);

        var act = async () => await _foldersApi.GetFolderAsync(folder.Id, TestContext.Current.CancellationToken);

        await act.Should().NotThrowAsync();
    }

}
