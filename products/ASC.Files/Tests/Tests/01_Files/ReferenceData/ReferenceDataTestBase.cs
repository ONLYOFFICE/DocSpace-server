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

namespace ASC.Files.Tests.Tests._01_Files.ReferenceData;

/// <summary>
/// Shared setup for the <c>POST /files/file/referencedata</c> suites. Inherits
/// <c>RoomsPermissionsTestBase</c> (namespace <c>ASC.Files.Tests.Tests._03_Rooms</c>, already
/// brought in through the project's <c>GlobalUsings.cs</c>) purely to reuse its
/// <c>InviteMember</c> / <c>InviteToRoom</c> helpers, the same way <c>RecentTestBase</c> under
/// <c>01_Files/Recent</c> does.
/// </summary>
public abstract class ReferenceDataTestBase(AspireAppFixture fixture) : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Creates a custom room with one file inside it, as the currently authenticated caller.
    /// </summary>
    protected async Task<(FolderDtoInteger Room, FileDtoInteger File)> CreateRoomWithFile(string roomTitle, string fileTitle)
    {
        var room = await CreateCustomRoom(roomTitle);
        var file = await CreateFile(fileTitle, room.Id);

        return (room, file);
    }

    /// <summary>
    /// Opens the editor for the given file to mint the <c>fileKey</c>/<c>instanceId</c> pair that
    /// identifies it for <c>GetReferenceData</c> - the same round trip the document editor performs
    /// before ever calling the reference-data endpoint.
    /// </summary>
    protected async Task<(string FileKey, string InstanceId)> OpenEditAndGetReferenceKeys(int fileId)
    {
        var configuration = await _filesApi.OpenEditFileAsync(fileId, cancellationToken: TestContext.Current.CancellationToken);
        var referenceData = configuration.Response.Document.ReferenceData;

        return (referenceData.FileKey, referenceData.InstanceId);
    }
}
