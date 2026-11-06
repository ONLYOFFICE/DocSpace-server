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

namespace ASC.Files.Tests.Tests._01_Files.States;

/// <summary>
/// A file locked by one member and then touched by another through the co-editing endpoints.
/// The lock "blocks editing" for everybody but the account that set it (<c>lockFile</c>).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class LockedFileStateTests(
    AspireAppFixture fixture)
    : StatesTestBase(fixture)
{
    private async Task<(User Member, int FileId)> LockedByOwner()
    {
        var (roomId, fileId) = await CreateRoomWithHtml();
        var member = await AddMember(roomId, FileShare.ContentCreator);

        await _filesClient.Authenticate(Owner);
        await _filesApi.LockFileAsync(fileId, new LockFileRequest(true), TestContext.Current.CancellationToken);

        return (member, fileId);
    }

    [Fact]
    public async Task SaveEditing_LockedByOwner_ForbiddenAndNothingChanges()
    {
        // Arrange
        var (member, fileId) = await LockedByOwner();
        var before = await ReadState(fileId);
        await _filesClient.Authenticate(member);

        // Act
        var status = await StatusOf(async () => await _filesApi.SaveEditingFileFromFormAsync(
            fileId,
            file: new FileParameter("replacement.html", "text/html", new MemoryStream(Encoding.UTF8.GetBytes("<p>replaced</p>"))),
            forcesave: false,
            cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        status.Should().Be(403);
        (await ReadState(fileId)).Should().BeEquivalentTo(before);
    }

    // The refusal status is covered by the Bug test below; whatever it is, nothing may change.
    [Fact]
    public async Task StartEdit_LockedByOwner_RefusedClaimChangesNothing()
    {
        // Arrange
        var (member, fileId) = await LockedByOwner();
        var before = await ReadState(fileId);
        await _filesClient.Authenticate(member);

        // Act
        await StatusOf(async () => await _filesApi.StartEditFileAsync(
            fileId, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken));

        // Assert
        (await ReadState(fileId)).Should().BeEquivalentTo(before);
    }

    /// <summary>
    /// The claim of a locked file is refused, but through an unhandled exception (500) instead of a client error.
    /// </summary>
    [Fact]
    [Trait("Bug", "84768")]
    public async Task StartEdit_LockedByOwner_RefusedWithClientError()
    {
        // Arrange
        var (member, fileId) = await LockedByOwner();
        var before = await ReadState(fileId);
        await _filesClient.Authenticate(member);

        // Act
        var status = await StatusOf(async () => await _filesApi.StartEditFileAsync(
            fileId, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken));

        // Assert
        (await ReadState(fileId)).Should().BeEquivalentTo(before);
        status.Should().BeInRange(400, 499);
    }
}
