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


namespace ASC.AI.Tests.Tests.AgentMigrationTests;

/// <summary>
/// Requirement "Chat outputs of a converted agent move to My documents": the Chat outputs folder of an agent goes,
/// with its content and the ids of its files, into the My documents of the agent's owner under the title
/// <c>&lt;agent title&gt; (&lt;title of the Chat outputs folder&gt;)</c>; an empty one is deleted.
/// </summary>
[Trait("Category", "Migration")]
[Trait("Feature", "AI/AgentMigration")]
public class AgentMigrationOutputsTests(AspireAppFixture fixture) : AgentMigrationTestBase(fixture)
{
    [Fact]
    public async Task Convert_ChatOutputsWithAFile_MovesTheFolderToMyDocumentsOfTheOwner()
    {
        // Arrange
        var title = NewTitle("Sales bot");
        var agent = await CreateAgentAsync(title);
        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);
        var file = await AddOutputsFileAsync(agent.Id, "autotest-output.txt");

        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await ShareRoomAsync(agent.Id, member.Id, FileShare.Read);

        // The member reads the file while it lies in the agent: the 403 below is the move, not a missing right.
        (await FileReadStatusAsync(file.Id, member)).Should().Be(200, "a member of the agent reads its Chat outputs");

        // Act
        await ConvertAgentsAsync();

        // Assert
        var expectedTitle = $"{title} ({outputs.Title})";
        var moved = (await MyDocumentsFoldersAsync(Owner)).Where(folder => folder.Title == expectedTitle).ToList();

        moved.Should().ContainSingle($"My documents of the owner lists \"{expectedTitle}\"");
        moved[0].Id.Should().Be(outputs.Id, "the folder is moved, not copied");

        await FilesHttpClient.Authenticate(Owner);
        var info = (await FilesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.Id.Should().Be(file.Id);
        info.FolderId.Should().Be(moved[0].Id);

        (await FileReadStatusAsync(file.Id, member)).Should().Be(403, "the folder is now a personal folder of the owner");
    }

    [Fact]
    public async Task Convert_EmptyChatOutputs_LeavesNoFolderInMyDocuments()
    {
        // Arrange
        var title = NewTitle("Empty outputs");
        var agent = await CreateAgentAsync(title);
        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);

        // Act
        var result = await ConvertAgentsAsync();

        // Assert - the agent was converted, so an empty list is not an agent the conversion never reached
        result.Migrated.Should().Be(1);
        (await MyDocumentsFoldersAsync(Owner)).Select(folder => folder.Title).Should().NotContain(folderTitle => folderTitle.StartsWith(title));

        await FilesHttpClient.Authenticate(Owner);
        var status = await StatusOfAsync(() => FoldersApi.GetFolderInfoAsync(outputs.Id, TestContext.Current.CancellationToken));
        status.Should().Be(404, "an empty Chat outputs folder is deleted");
    }

    [Fact]
    public async Task Convert_OwnerOverTheQuota_MovesTheFolderAndChargesTheOwner()
    {
        // Arrange - the agent belongs to a Room admin, the lowest account that creates one, with a quota of one byte
        var author = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var title = NewTitle("Quota bot");
        var agent = await CreateAgentAsync(title, author);
        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);
        var file = await AddOutputsFileAsync(agent.Id, "autotest-quota-output.txt", author);

        await EnableUserQuotaAsync(100 * 1024 * 1024);
        await PeopleHttpClient.Authenticate(Owner);
        await PeopleQuotaApi.UpdateUserQuotaAsync(
            new UpdateMembersQuotaRequestDto([author.Id], new UpdateMembersQuotaRequestDtoQuota(1)),
            TestContext.Current.CancellationToken);

        var before = await ProfileOfAsync(author);
        before.QuotaLimit.Should().Be(1, "the quota of the agent's owner is one byte");
        (before.UsedSpace ?? 0).Should().Be(0, "the files of an agent are not charged to its owner");

        // Act
        var result = await ConvertAgentsAsync();

        // Assert
        result.Migrated.Should().Be(1);

        var expectedTitle = $"{title} ({outputs.Title})";
        (await MyDocumentsFoldersAsync(author)).Select(folder => folder.Title).Should().Contain(expectedTitle);

        var after = await WaitForProfileAsync(author, profile => (profile.UsedSpace ?? 0) >= file.Size);
        (after.UsedSpace ?? 0).Should().Be(file.Size, "the files of Chat outputs are charged to the owner with no check of the quota");
        (after.UsedSpace ?? 0).Should().BeGreaterThan(after.QuotaLimit ?? 0);
    }

    [Fact]
    public async Task Convert_OwnerDisabled_GivesTheFolderToThePortalOwner()
    {
        // Arrange
        var author = await InviteContact(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        var title = NewTitle("Orphan bot");
        var agent = await CreateAgentAsync(title, author);
        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);
        var file = await AddOutputsFileAsync(agent.Id, "autotest-orphan-output.txt", author);

        await PeopleHttpClient.Authenticate(Owner);
        await UserStatusApi.UpdateUserStatusAsync(
            DocSpace.API.SDK.Model.EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([author.Id]),
            TestContext.Current.CancellationToken);

        // Act
        await ConvertAgentsAsync();

        // Assert
        var expectedTitle = $"{title} ({outputs.Title})";
        var moved = (await MyDocumentsFoldersAsync(Owner)).Where(folder => folder.Title == expectedTitle).ToList();

        moved.Should().ContainSingle("the portal owner takes the folder of a disabled account");
        (await FileReadStatusAsync(file.Id, Owner)).Should().Be(200);
    }

    [Fact]
    public async Task Convert_SecondCall_MovesNothingAgain()
    {
        // Arrange
        var title = NewTitle("Twice bot");
        var agent = await CreateAgentAsync(title);
        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);
        var file = await AddOutputsFileAsync(agent.Id, "autotest-twice-output.txt");

        await ConvertAgentsAsync();

        // Act
        var second = await ConvertAgentsAsync();

        // Assert
        second.Migrated.Should().Be(0);

        var expectedTitle = $"{title} ({outputs.Title})";
        var moved = (await MyDocumentsFoldersAsync(Owner)).Where(folder => folder.Title.StartsWith(title)).ToList();

        moved.Should().ContainSingle().Which.Title.Should().Be(expectedTitle, "a second call neither moves nor retitles the folder again");

        await FilesHttpClient.Authenticate(Owner);
        var info = (await FilesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;
        info.FolderId.Should().Be(moved[0].Id);
    }

    [Fact]
    public async Task Convert_ChatOutputsFiles_MovesTheirModifiedOn()
    {
        // Arrange
        var agent = await CreateAgentAsync(NewTitle("Outputs index bot"));
        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);

        var direct = await UploadAsync(outputs.Id, "autotest-direct-output.txt");
        var folder = (await FoldersApi.CreateFolderAsync(outputs.Id, new CreateFolderRequest("Nested"), TestContext.Current.CancellationToken)).Response;
        var nested = await UploadAsync(folder.Id, "autotest-nested-output.txt");
        int[] fileIds = [direct.Id, nested.Id];
        var past = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await using (var db = await CreateDbContextAsync<FilesDbContext>())
        {
            (await db.Files
                .Where(f => fileIds.Contains(f.Id))
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.ModifiedOn, past), TestContext.Current.CancellationToken))
                .Should().BeGreaterThanOrEqualTo(2);

            (await db.Folders
                .Where(f => f.Id == folder.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.ModifiedOn, past), TestContext.Current.CancellationToken))
                .Should().Be(1);
        }

        await AddOlderVersionAsync(direct.Id, past);
        await AddOlderVersionAsync(direct.Id, past);

        // Act
        await ConvertAgentsAsync();

        // Assert - the periodic pass of the full-text index reads the files by modified_on and stores their new ancestor chain
        Dictionary<int, DateTime> modified;

        await using (var db = await CreateDbContextAsync<FilesDbContext>())
        {
            modified = await db.Files
                .Where(f => fileIds.Contains(f.Id) && f.CurrentVersion)
                .ToDictionaryAsync(f => f.Id, f => f.ModifiedOn, TestContext.Current.CancellationToken);
        }

        modified.Keys.Should().BeEquivalentTo(fileIds);
        modified.Values.Should().AllSatisfy(value => value.Should().BeAfter(past, "the file lies in the moved Chat outputs folder"));
        var versions = await ModifiedOnByVersionAsync(direct.Id);
        versions[1].Should().BeAfter(past, "the periodic pass of the index finds a file by the modified_on of its first version");
        versions[2].Should().Be(past, "a version between the first and the current one keeps its date in the version history");

        await using (var db = await CreateDbContextAsync<FilesDbContext>())
        {
            var folderModifiedOn = await db.Folders
                .Where(f => f.Id == folder.Id)
                .Select(f => f.ModifiedOn)
                .SingleAsync(TestContext.Current.CancellationToken);
            folderModifiedOn.Should().BeAfter(past, "the periodic pass of the folder index finds a subfolder of the moved Chat outputs folder by its modified_on");
        }
    }

    [Fact]
    public async Task Convert_ChatOutputs_PublishesIndexEventsForItsSubtree()
    {
        // Arrange
        var agent = await CreateAgentAsync(NewTitle("Outputs events bot"));
        var outputs = await AgentFolderAsync(agent.Id, FolderType.ChatOutputs);

        var direct = await UploadAsync(outputs.Id, "autotest-direct-output.txt");
        var folder = (await FoldersApi.CreateFolderAsync(outputs.Id, new CreateFolderRequest("Nested"), TestContext.Current.CancellationToken)).Response;
        var nested = await UploadAsync(folder.Id, "autotest-nested-output.txt");
        int[] fileIds = [direct.Id, nested.Id];

        await using var fileProbe = await ListenToEventBusAsync(nameof(FileIndexIntegrationEvent));
        await using var folderProbe = await ListenToEventBusAsync(nameof(FolderIndexIntegrationEvent));

        // Act
        await ConvertAgentsAsync();

        // Assert - the worker stores the new ancestor chain of each file and subfolder it gets an event for
        var fileEvents = await fileProbe.ReadAsync<FileIndexIntegrationEvent>(
            e => e.Action == FileIndexAction.UpdateFolders && fileIds.Contains(e.FileId),
            TestContext.Current.CancellationToken);

        fileEvents.Select(e => e.FileId).Should().BeEquivalentTo(fileIds, "every file of the moved Chat outputs folder gets its event");

        var folderEvents = await folderProbe.ReadAsync<FolderIndexIntegrationEvent>(
            e => e.Action == FolderIndexAction.UpdateFolders && e.FolderId == folder.Id,
            TestContext.Current.CancellationToken);

        folderEvents.Should().ContainSingle("the subfolder of the moved Chat outputs folder gets its event");
    }
}
