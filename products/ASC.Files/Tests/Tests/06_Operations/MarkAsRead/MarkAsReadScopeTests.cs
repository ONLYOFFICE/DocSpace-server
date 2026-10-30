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

namespace ASC.Files.Tests.Tests._06_Operations.MarkAsRead;

/// <summary>
/// <c>PUT /api/2.0/files/fileops/markasread</c> — whose badges are cleared. Only the caller's "new"
/// marks are cleared; items the caller cannot read, or that do not exist, are passed over in silence;
/// nothing is modified and other members keep their badges.
/// </summary>
[Trait("Category", "Operations")]
[Trait("Feature", "Files")]
public class MarkAsReadScopeTests(
    AspireAppFixture fixture)
    : MarkAsReadTestBase(fixture)
{
    private async Task MarkAndWait(params int[] fileIds)
    {
        var results = (await _filesOperationsApi.MarkAsReadAsync(
            MarkAsReadFiles(fileIds), TestContext.Current.CancellationToken)).Response;
        await WaitLongOperation(results.FirstOrDefault()?.Id);
    }

    [Fact]
    public async Task MarkAsRead_File_ClearsOnlyTheCallersBadgeAndAnotherMemberKeepsTheirs()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest MarkRead Isolation Room");
        var reader = await InviteMember(EmployeeType.User);
        var other = await InviteMember(EmployeeType.User);
        await InviteToRoom(room.Id, reader, FileShare.Read);
        await InviteToRoom(room.Id, other, FileShare.Read);

        foreach (var member in new[] { reader, other })
        {
            await _filesClient.Authenticate(member);
            await _roomsApi.GetRoomInfoAsync(room.Id, TestContext.Current.CancellationToken);
        }

        await _filesClient.Authenticate(Owner);
        var first = await CreateFile("Autotest MarkRead One.docx", room.Id);
        await CreateFile("Autotest MarkRead Two.docx", room.Id);
        string[] both = ["Autotest MarkRead One.docx", "Autotest MarkRead Two.docx"];

        foreach (var member in new[] { reader, other })
        {
            await _filesClient.Authenticate(member);
            (await PollRoomNewsTitles(room.Id, t => both.All(b => t.Contains(b)))).Should().Contain(both);
        }

        // Act
        await _filesClient.Authenticate(reader);
        await MarkAndWait(first.Id);

        // Assert
        (await PollRoomNewsTitles(room.Id, t => !t.Contains(both[0]))).Should().Equal("Autotest MarkRead Two.docx");
        await _filesClient.Authenticate(other);
        (await PollRoomNewsTitles(room.Id, _ => true)).Should().BeEquivalentTo(both);

        // Repeating on an item that is already read changes nothing.
        await _filesClient.Authenticate(reader);
        await MarkAndWait(first.Id);
        (await PollRoomNewsTitles(room.Id, t => t.Count == 1)).Should().Equal("Autotest MarkRead Two.docx");
        await _filesClient.Authenticate(other);
        (await PollRoomNewsTitles(room.Id, _ => true)).Should().BeEquivalentTo(both);

        // Nothing was opened, moved or modified.
        await _filesClient.Authenticate(Owner);
        var info = await GetFile(first.Id);
        info.Version.Should().Be(1);
        info.FolderId.Should().Be(room.Id);
    }

    [Fact]
    public async Task MarkAsRead_SeveralFilesWithARepeatedId_AreAllClearedForTheCaller()
    {
        // Arrange
        var (room, member) = await CreateRoomWithReadVisitor("Autotest MarkRead Several Room");
        var a = await CreateFile("Autotest MarkRead SevA.docx", room.Id);
        var b = await CreateFile("Autotest MarkRead SevB.docx", room.Id);
        await CreateFile("Autotest MarkRead SevC.docx", room.Id);

        await _filesClient.Authenticate(member);
        await PollRoomNewsTitles(room.Id, t => t.Count >= 3);

        // Act
        await MarkAndWait(a.Id, b.Id, a.Id);

        // Assert
        var titles = await PollRoomNewsTitles(room.Id, t => t.Count == 1);
        titles.Should().Equal("Autotest MarkRead SevC.docx");
    }

    [Fact]
    public async Task MarkAsRead_FileTheCallerCannotRead_IsPassedOverInSilenceAndTheReadableOneIsStillCleared()
    {
        // Arrange
        var (room, member) = await CreateRoomWithReadVisitor("Autotest MarkRead Private Room");
        var myDocsFolderId = await GetUserFolderIdAsync(Owner);
        var readable = await CreateFile("Autotest MarkRead Readable.docx", room.Id);
        var secret = OperationFileBytes.Make(40, 31);
        var privateFile = (await OperationFileBytes.UploadAsync(
            _filesOperationsApi, myDocsFolderId, "Autotest MarkRead Secret.bin", secret, true, TestContext.Current.CancellationToken)).File;

        await _filesClient.Authenticate(member);
        await PollRoomNewsTitles(room.Id, t => t.Contains("Autotest MarkRead Readable.docx"));

        // Premise: the member cannot read the private file.
        var denied = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(privateFile.Id, cancellationToken: TestContext.Current.CancellationToken));
        denied.ErrorCode.Should().Be(403);

        // Act
        await MarkAndWait(readable.Id, privateFile.Id);

        // Assert
        (await PollRoomNewsTitles(room.Id, t => t.Count == 0)).Should().BeEmpty();

        // Even a selection that is entirely inaccessible succeeds and changes nothing.
        await MarkAndWait(privateFile.Id);
        await _filesClient.Authenticate(Owner);
        (await OperationFileBytes.DownloadAsync(_filesClient, privateFile.Id, null, TestContext.Current.CancellationToken)).Should().Equal(secret);
    }

    [Fact]
    public async Task MarkAsRead_IdThatDoesNotExist_IsPassedOverAndARealFileNextToItIsCleared()
    {
        // Arrange
        var (room, member) = await CreateRoomWithReadVisitor("Autotest MarkRead Missing Room");
        var real = await CreateFile("Autotest MarkRead Real.docx", room.Id);

        await _filesClient.Authenticate(member);
        await PollRoomNewsTitles(room.Id, t => t.Contains("Autotest MarkRead Real.docx"));

        // Act
        await MarkAndWait(real.Id, 99999999);

        // Assert
        (await PollRoomNewsTitles(room.Id, t => t.Count == 0)).Should().BeEmpty();
    }
}
