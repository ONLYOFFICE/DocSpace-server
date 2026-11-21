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
/// A file put directly into an AI project is visible to its creator only, so the free title the server picks for it
/// takes only the caller's own files there into account: a member who uploads or copies "salary.txt" next to another
/// member's "salary.txt" keeps the name instead of learning about the other file from a "salary (1).txt".
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiProjectDraftTitleTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task Upload_SameTitleAsAnotherMembersFile_KeepsTheTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var first = await InviteContact(EmployeeType.User);
        var second = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, first.Id, FileShare.ContentCreator);
        await ShareRoomAsync(projectId, second.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(first);
        var firstOwn = await UploadThroughSessionAsync(projectId, "salary.txt");
        var firstRepeat = await UploadThroughSessionAsync(projectId, "salary.txt");

        await _filesClient.Authenticate(second);

        // Act
        var secondTitle = await UploadThroughSessionAsync(projectId, "salary.txt");

        // Assert
        firstOwn.Should().Be("salary.txt");
        firstRepeat.Should().Be("salary (1).txt");
        secondTitle.Should().Be("salary.txt");
    }

    [Fact]
    public async Task Copy_SameTitleAsAnotherMembersFile_KeepsTheTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projectId = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(projectId, member.Id, FileShare.ContentCreator);
        var title = $"Autotest report {Guid.NewGuid():N}.docx";
        var ownerDraft = await UploadThroughSessionAsync(projectId, title);
        var ownerFile = await CreateFileInMy(title, Owner);
        var ownerCopy = await CopyIntoAsync(projectId, ownerFile.Id);

        await _filesClient.Authenticate(member);
        var memberFile = await CreateFileInMy(title, member);

        // Act
        var memberCopy = await CopyIntoAsync(projectId, memberFile.Id);

        // Assert
        ownerDraft.Should().Be(title);
        ownerCopy.Should().Be(title.Replace(".docx", " (1).docx"));
        memberCopy.Should().Be(title);
    }

    private async Task<string> UploadThroughSessionAsync(int folderId, string name)
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

        return uploaded.Title;
    }

    private async Task<string?> CopyIntoAsync(int destFolderId, int fileId)
    {
        var request = new BatchRequestDto
        {
            DestFolderId = new(destFolderId),
            ConflictResolveType = FileConflictResolveType.Duplicate,
            FileIds = [new(fileId)],
            FolderIds = [],
            ReturnSingleOperation = true
        };

        var operationId = (await _filesOperationsApi.CopyBatchItemsAsync(request, TestContext.Current.CancellationToken)).Response.Single().Id;
        var status = (await WaitLongOperation(operationId))?.SingleOrDefault(s => s.Id == operationId);

        status.Should().NotBeNull();
        status!.Error.Should().BeNullOrEmpty();

        return status.Files?.OfType<FileDto>().SingleOrDefault()?.Title;
    }
}

