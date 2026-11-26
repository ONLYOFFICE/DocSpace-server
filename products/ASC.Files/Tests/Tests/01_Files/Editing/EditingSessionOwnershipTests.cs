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

namespace ASC.Files.Tests.Tests._01_Files.Editing;

/// <summary>
/// An editing session belongs to the account that opened it: a refused claim, the end of a tab and a save each touch
/// only the sessions of their caller, whatever tab id or file the request names.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class EditingSessionOwnershipTests(
    AspireAppFixture fixture)
    : EditingTestBase(fixture)
{
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    /// <summary>
    /// The holder of an exclusive session repeats the claim: the repeat is refused, and the refusal leaves the
    /// holder's own session in place, so the next claim of somebody else is refused too.
    /// </summary>
    [Fact]
    public async Task StartEditFile_HolderRepeatsTheClaim_KeepsTheirSession()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest Repeated Claim Room", "Autotest Repeated Claim File");
        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken);
        var repeat = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken));

        // Act
        await _filesClient.Authenticate(member);
        var other = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.StartEditFileAsync(file.Id, new StartEditRequest(editingAlone: true), TestContext.Current.CancellationToken));

        // Assert
        repeat.ErrorCode.Should().Be(403);
        other.ErrorCode.Should().Be(403);
        (await IsEditingAsync(file.Id)).Should().BeTrue();
    }

    /// <summary>
    /// <c>trackeditfile</c> with <c>isFinish</c> names a tab: a tab of another account stays, even when the caller holds
    /// the key of the document.
    /// </summary>
    [Fact]
    public async Task TrackEditFile_FinishOfAnotherAccountsTab_KeepsThatSession()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var (room, file) = await CreateRoomWithFile("Autotest Foreign Tab Room", "Autotest Foreign Tab File");
        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        var ownerTab = Guid.NewGuid();
        var docKey = await GetDocKey(file.Id);
        await _filesApi.TrackEditFileAsync(file.Id, tabId: ownerTab, docKeyForTrack: docKey, cancellationToken: TestContext.Current.CancellationToken);
        (await IsEditingAsync(file.Id)).Should().BeTrue();

        // Act
        await _filesClient.Authenticate(member);
        var answer = (await _filesApi.TrackEditFileAsync(file.Id, tabId: ownerTab, docKeyForTrack: docKey, isFinish: true, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        answer.Key.Should().BeTrue("the call succeeds although it removes nothing");
        (await IsEditingAsync(file.Id)).Should().BeTrue("the tab belongs to the owner");
    }

    /// <summary>
    /// Two accounts edit the file together, and one of them updates it through the hidden
    /// <c>PUT api/2.0/files/{fileId}/update</c>: the other one still has the file open, so the update is refused and
    /// closes no session, the caller's included. Hidden from the OpenAPI document, so it is called over raw HTTP.
    /// </summary>
    [Fact]
    public async Task UpdateFileStream_CoEditorWhileTheOtherStillEdits_ForbiddenAndTheOtherSessionStays()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Co-editing Update Room");
        var file = await CreateFile("Autotest Co-editing Update.docx", room.Id);
        var member = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, member, FileShare.Editing);

        var docKey = await GetDocKey(file.Id);
        var ownerTab = Guid.NewGuid();
        await _filesApi.TrackEditFileAsync(file.Id, tabId: ownerTab, docKeyForTrack: docKey, cancellationToken: TestContext.Current.CancellationToken);
        var versionBefore = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.Version;

        await _filesClient.Authenticate(member);
        await _filesApi.TrackEditFileAsync(file.Id, tabId: Guid.NewGuid(), docKeyForTrack: docKey, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        var status = await UpdateFileStreamAsync(file.Id, Encoding.UTF8.GetBytes("replaced"));

        // Assert
        status.Should().Be(HttpStatusCode.Forbidden);
        var after = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Version.Should().Be(versionBefore);
        (await IsEditingAsync(file.Id)).Should().BeTrue("the owner still has the file open");

        // the refused caller keeps their own session: once the owner closes theirs, the file is still being edited
        await _filesApi.TrackEditFileAsync(file.Id, tabId: ownerTab, docKeyForTrack: docKey, isFinish: true, cancellationToken: TestContext.Current.CancellationToken);
        (await IsEditingAsync(file.Id)).Should().BeTrue("the member's session was kept by the refused update");
    }

    /// <summary>Whether the file is open for editing, as the owner reads it; the client stays on the owner.</summary>
    private async Task<bool> IsEditingAsync(int fileId)
    {
        await _filesClient.Authenticate(Owner);
        var file = (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return file.FileStatus.HasValue && (file.FileStatus.Value & FileStatus.IsEditing) == FileStatus.IsEditing;
    }

    private async Task<HttpStatusCode> UpdateFileStreamAsync(int fileId, byte[] content)
    {
        using var form = new MultipartFormDataContent();
        var part = new ByteArrayContent(content);
        part.Headers.ContentType = new MediaTypeHeaderValue(DocxContentType);
        form.Add(part, "File", "Autotest Co-editing Update.docx");
        form.Add(new StringContent("false"), "Forcesave");

        using var response = await _filesClient.PutAsync($"api/2.0/files/{fileId}/update", form, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }
}
