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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// An AI project and the entries inside it have no history: the log and the report of the history answer 403 to
/// everyone, and a copy between a project and another folder still shows in the history of that folder.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiProjectHistoryTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetFolderHistory_OfAProject_Owner_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var roomId = await CreateInstructionsRoomAsync();

        // Act
        var roomHistory = await _foldersApi.GetFolderHistoryAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(projectId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        roomHistory.Response.Should().NotBeNull();
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFolderHistory_OfAProject_ContentCreatorMember_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);
        var folder = await CreateFolderInMy("Autotest member folder", member);

        // Act
        var folderHistory = await _foldersApi.GetFolderHistoryAsync(folder.Id, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(projectId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        folderHistory.Response.Should().NotBeNull();
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFolderHistory_OfAProject_DocSpaceAdminNotAMember_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var roomId = await CreateInstructionsRoomAsync();
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);

        // Act
        var roomHistory = await _foldersApi.GetFolderHistoryAsync(roomId, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetFolderHistoryAsync(projectId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        roomHistory.Response.Should().NotBeNull();
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetFileHistory_OfOwnFileInAProject_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);
        var myFile = await CreateFileInMy("Autotest my file.docx", member);

        await _filesClient.Authenticate(member);
        var projectFileId = await UploadThroughSessionAsync(projectId, "notes.txt");

        // Act
        var myFileHistory = await _filesApi.GetFileHistoryAsync(myFile.Id, cancellationToken: TestContext.Current.CancellationToken);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileHistoryAsync(projectFileId, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        myFileHistory.Response.Should().NotBeNull();
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task FolderHistoryReport_OfAProject_Owner_Returns403()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var roomId = await CreateInstructionsRoomAsync();

        // Act
        var roomReport = await _foldersApi.GetReportFolderHistoryAsync(roomId, TestContext.Current.CancellationToken);
        var start = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.CreateReportFolderHistoryAsync(projectId, cancellationToken: TestContext.Current.CancellationToken));
        var status = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.GetReportFolderHistoryAsync(projectId, TestContext.Current.CancellationToken));
        var stop = await Assert.ThrowsAsync<ApiException>(
            async () => await _foldersApi.TerminateReportFolderHistoryAsync(projectId, TestContext.Current.CancellationToken));

        // Assert
        roomReport.Should().NotBeNull();
        start.ErrorCode.Should().Be(403);
        status.ErrorCode.Should().Be(403);
        stop.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task GetRoomInfo_OfAProject_ReportsNoHistoryExport()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var roomId = await CreateInstructionsRoomAsync();

        // Act
        var project = await GetRoomInfoAsync(projectId);
        var room = await GetRoomInfoAsync(roomId);

        // Assert
        room.Security.HistoryExport.Should().BeTrue();
        project.Security.HistoryExport.Should().BeFalse();
    }

    [Fact]
    public async Task Copy_BetweenMyDocumentsFolderAndAProject_IsInTheHistoryOfTheFolder()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var folder = await CreateFolderInMy("Autotest copy folder", Owner);
        var myFile = await CreateFile("Autotest to project.docx", folder.Id);
        var projectFileId = await UploadThroughSessionAsync(projectId, "from-project.txt");

        // Act
        await CopyFileAsync(projectId, myFile.Id);
        await CopyFileAsync(folder.Id, projectFileId);

        // Assert
        var copies = await PollActionCountAsync(folder.Id, MessageAction.FileCopied, 2);
        copies.Should().Be(2);
    }

    private async Task<int> UploadThroughSessionAsync(int folderId, string name)
    {
        var bytes = Encoding.UTF8.GetBytes("Autotest project file.");

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            folderId,
            new SessionRequest(name, bytes.Length, createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        var uploaded = (await _filesOperationsApi.UploadSessionAsync(
            folderId,
            session.Id,
            new FileParameter(name, "application/octet-stream", new MemoryStream(bytes)),
            TestContext.Current.CancellationToken)).Response.File;

        return uploaded.Id;
    }

    private async Task<int> PollActionCountAsync(int folderId, MessageAction action, int expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            var history = (await _foldersApi.GetFolderHistoryAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;
            var count = history.Concat(history.SelectMany(e => e.Related ?? [])).Count(e => e.Action?.Id == action);

            if (count >= expected || DateTime.UtcNow >= deadline)
            {
                return count;
            }

            await Task.Delay(1_000, TestContext.Current.CancellationToken);
        }
    }
}
