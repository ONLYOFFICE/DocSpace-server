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
/// <c>PUT /files/file/:fileId/history</c> on a file that someone else holds: locked, or open in an
/// editing session. Both are documented refusals; the lock binds others, not the account that set it.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class ChangeVersionHistoryHeldFileTests(
    AspireAppFixture fixture)
    : StatesTestBase(fixture)
{
    // A docx, because the editing flag is only reliable on it.
    private async Task<(User Member, int FileId, int Versions)> HeldByOwner(bool locked)
    {
        var (roomId, _) = await CreateRoomWithHtml();
        var file = await CreateFile("Autotest History State.docx", roomId);
        var member = await AddMember(roomId, FileShare.ContentCreator);

        await _filesClient.Authenticate(Owner);

        if (locked)
        {
            await _filesApi.LockFileAsync(file.Id, new LockFileRequest(true), TestContext.Current.CancellationToken);
        }
        else
        {
            await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken);
        }

        return (member, file.Id, await VersionCount(file.Id));
    }

    private async Task<int> ChangeAs(User member, int fileId)
    {
        await _filesClient.Authenticate(member);

        return await StatusOf(async () => await _filesApi.ChangeVersionHistoryAsync(
            fileId, new ChangeHistoryRequest(0, false), TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeVersionHistory_HeldByOwner_LeavesHistoryUnchanged(bool locked)
    {
        // Arrange
        var (member, fileId, before) = await HeldByOwner(locked);

        // Act
        await ChangeAs(member, fileId);

        // Assert
        (await VersionCount(fileId)).Should().Be(before);
    }

    /// <summary>
    /// The refusal of a locked file ("The file is currently locked by another user") was raised as a plain
    /// exception, a 500, instead of a client error.
    /// </summary>
    [Fact]
    [Trait("Bug", "84771")]
    public async Task ChangeVersionHistory_LockedByOwner_RefusedWithClientError()
    {
        // Arrange
        var (member, fileId, _) = await HeldByOwner(locked: true);

        // Act
        var status = await ChangeAs(member, fileId);

        // Assert
        status.Should().BeInRange(400, 499);
    }

    /// <summary>
    /// A file open in someone else's editing session is "refused" by the SDK description, yet the call
    /// answered 200 although nothing changed.
    /// </summary>
    [Fact]
    [Trait("Bug", "84772")]
    public async Task ChangeVersionHistory_EditedByOwner_RefusedNotReportedAsSuccess()
    {
        // Arrange
        var (member, fileId, before) = await HeldByOwner(locked: false);

        // Act
        var status = await ChangeAs(member, fileId);

        // Assert
        (await VersionCount(fileId)).Should().Be(before);
        status.Should().NotBe(200);
    }
}
