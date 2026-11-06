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

namespace ASC.Files.Tests.Tests._01_Files.Update;

/// <summary>Renaming a file that another member has locked.</summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class FileRenameLockedTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private async Task<(User Member, int FileId)> LockedByOwner()
    {
        await _filesClient.Authenticate(Owner);

        var room = await CreateCustomRoom("Autotest Mutations Lock Enforcement " + Guid.NewGuid().ToString()[..8]);
        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        var file = (await _filesApi.CreateHtmlFileAsync(
            room.Id, new CreateTextOrHtmlFileRequest("Locked", "<p>x</p>"), TestContext.Current.CancellationToken)).Response;

        await _filesClient.Authenticate(Owner);
        await _filesApi.LockFileAsync(file.Id, new LockFileRequest(true), TestContext.Current.CancellationToken);

        return (member, file.Id);
    }

    private async Task<int> RenameAsMember(User member, int fileId)
    {
        await _filesClient.Authenticate(member);

        try
        {
            await _filesApi.UpdateFileAsync(fileId, new UpdateFileRequest { Title = "Renamed while locked" }, TestContext.Current.CancellationToken);

            return 200;
        }
        catch (ApiException e)
        {
            return e.ErrorCode;
        }
    }

    // Whatever status the refused rename answers, the file has to stay as it was.
    [Fact]
    public async Task UpdateFile_LockedByOwner_ContentCreatorRenameChangesNothing()
    {
        // Arrange
        var (member, fileId) = await LockedByOwner();

        // Act
        await RenameAsMember(member, fileId);

        // Assert
        await _filesClient.Authenticate(Owner);
        var file = await GetFile(fileId);
        file.Title.Should().Be("Locked.html");
        file.Locked.Should().BeTrue();
    }

    /// <summary>
    /// The rename is refused as documented, but through an unhandled exception (500) instead of a client error.
    /// </summary>
    [Fact]
    [Trait("Bug", "84842")]
    public async Task UpdateFile_LockedByOwner_ContentCreatorRenameRefusedWithClientError()
    {
        // Arrange
        var (member, fileId) = await LockedByOwner();

        // Act
        var status = await RenameAsMember(member, fileId);

        // Assert
        await _filesClient.Authenticate(Owner);
        (await GetFile(fileId)).Title.Should().Be("Locked.html");
        status.Should().BeInRange(400, 499);
    }
}
