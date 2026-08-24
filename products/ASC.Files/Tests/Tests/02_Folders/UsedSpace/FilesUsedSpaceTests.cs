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

namespace ASC.Files.Tests.Tests._02_Folders.UsedSpace;

/// <summary>
/// <c>GET /api/2.0/files/filesusedspace</c> - the shape of the per-section usage statistics and how
/// each section reacts to the operation that is supposed to change it. <see cref="QuotaUsedSpaceTests"/>
/// covers the counters under load (deep trees, concurrent deletes); this class covers what a single
/// call reports and which single-file operations are, and are not, supposed to move the numbers.
/// </summary>
[Trait("Category", "Folders")]
[Trait("Feature", "UsedSpace")]
public class FilesUsedSpaceTests(
    AspireAppFixture fixture)
    : UsedSpaceTestBase(fixture)
{
    [Fact]
    public async Task GetUsedSpace_Owner_ReturnsResponse()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        sections.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUsedSpace_AllMandatorySections_HaveTitleAndUsedSpace()
    {
        // Arrange
        await InitializeAllSections();

        // Act
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        sections.MyDocumentsUsedSpace.Should().NotBeNull();
        sections.MyDocumentsUsedSpace.Title.Should().Be("Files");
        sections.TrashUsedSpace.Should().NotBeNull();
        sections.TrashUsedSpace.Title.Should().Be("Trash");
        sections.ArchiveUsedSpace.Should().NotBeNull();
        sections.ArchiveUsedSpace.Title.Should().NotBeNullOrEmpty();
        sections.RoomsUsedSpace.Should().NotBeNull();
        sections.RoomsUsedSpace.Title.Should().Be("Rooms");
    }

    [Fact]
    public async Task GetUsedSpace_AllValues_AreNonNegative()
    {
        // Arrange
        await InitializeAllSections();

        // Act
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        sections.MyDocumentsUsedSpace.UsedSpace.Should().BeGreaterThanOrEqualTo(0);
        sections.TrashUsedSpace.UsedSpace.Should().BeGreaterThanOrEqualTo(0);
        sections.ArchiveUsedSpace.UsedSpace.Should().BeGreaterThanOrEqualTo(0);
        sections.RoomsUsedSpace.UsedSpace.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task MyDocumentsUsedSpace_IncreasesAfterFileCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateFileInMy("used_space_my_init.docx", Owner);
        var before = await GetBaselineUsedSpaceAsync();

        // Act
        var file = await CreateFileInMy("used_space_my_check.docx", Owner);
        var fileSize = file.PureContentLength!.Value;

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.My > before.My);
        after.My.Should().Be(before.My + fileSize,
            "creating a file in My Documents must charge its section by the file's size");
    }

    [Fact]
    public async Task TrashUsedSpace_IncreasesAfterFileMovedToTrash()
    {
        // Arrange
        var file = await CreateFileInMy("used_space_trash.docx", Owner);
        var before = await GetBaselineUsedSpaceAsync();

        // Act
        await DeleteFileAndWait(file.Id, immediately: false);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Trash > before.Trash);
        after.Trash.Should().BeGreaterThan(before.Trash,
            "moving a file to the trash must be reflected in the Trash section");
    }

    [Fact]
    public async Task RoomsUsedSpace_IncreasesAfterFileCreatedInRoom()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("used_space_rooms_" + Guid.NewGuid().ToString()[..8]);
        var before = await GetBaselineUsedSpaceAsync();

        // Act
        await CreateFile("used_space_room_file.docx", room.Id);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Rooms > before.Rooms);
        after.Rooms.Should().BeGreaterThan(before.Rooms,
            "creating a file in a room must be reflected in the Rooms section");
    }

    [Fact]
    public async Task ArchiveUsedSpace_IncreasesAfterRoomWithFileArchived()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("used_space_archive_" + Guid.NewGuid().ToString()[..8]);
        await CreateFile("used_space_archive_file.docx", room.Id);

        var before = await GetBaselineUsedSpaceAsync();

        // Act
        await ArchiveRoomAndWait(room.Id);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.Archive > before.Archive);
        after.Archive.Should().BeGreaterThan(before.Archive,
            "archiving a room with content must be reflected in the Archive section");
    }

    [Fact]
    public async Task ArchiveUsedSpace_HasCorrectTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("used_space_archive_title_" + Guid.NewGuid().ToString()[..8]);

        // Act
        await ArchiveRoomAndWait(room.Id);

        // Assert
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;
        sections.ArchiveUsedSpace.Should().NotBeNull();
        sections.ArchiveUsedSpace.Title.Should().Be("Archive");
    }

    /// <summary>
    /// The AI Agents section only appears once the AI Agents feature is active (paid/configured);
    /// creating an AI room alone does not trigger it. This only checks the shape of the section
    /// when it happens to be present, it does not force it to appear.
    /// </summary>
    [Fact]
    public async Task AiAgentsUsedSpace_HasCorrectStructureWhenPresent()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        if (sections.AiAgentsUsedSpace is not null)
        {
            sections.AiAgentsUsedSpace.Title.Should().NotBeNullOrEmpty();
            sections.AiAgentsUsedSpace.UsedSpace.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    /// <summary>
    /// The product injects sample files as a side effect of some operations, which used to make
    /// <c>myDocumentsUsedSpace</c> stay the same or increase instead of decreasing after a hard
    /// delete.
    /// </summary>
    [Fact]
    [Trait("Bug", "81648")]
    public async Task MyDocumentsUsedSpace_DecreasesAfterHardDelete()
    {
        // Arrange
        await CreateFileInMy("used_space_hard_delete_warmup.docx", Owner);
        var file = await CreateFileInMy("used_space_hard_delete_target.docx", Owner);

        var before = await GetBaselineUsedSpaceAsync();

        // Act
        await DeleteFileAndWait(file.Id, immediately: true);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.My < before.My);
        after.My.Should().BeLessThan(before.My,
            "a hard delete must release the space of \"My documents\" immediately");
    }

    /// <summary>
    /// Reading a file's metadata must never mutate the used space counters.
    /// </summary>
    [Fact]
    [Trait("Bug", "81648")]
    public async Task UsedSpace_DoesNotChangeAfterReadingFileMetadata()
    {
        // Arrange
        var file = await CreateFileInMy("used_space_metadata_read.docx", Owner);
        var before = await GetBaselineUsedSpaceAsync();

        // Act
        await GetFile(file.Id);
        await GetFile(file.Id);
        await GetFile(file.Id);

        // Assert
        var after = await GetUsedSpaceAsync();
        after.My.Should().Be(before.My,
            "repeated metadata reads must not change the \"My documents\" used space");
    }

    [Fact]
    public async Task UsedSpace_IncreasesCumulativelyWithEachFileCreated()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateFileInMy("used_space_multi_warmup.docx", Owner);
        var s0 = (await GetBaselineUsedSpaceAsync()).My;

        // Act
        await CreateFileInMy("used_space_multi_1.docx", Owner);
        var s1 = (await WaitForUsedSpaceAsync(s => s.My > s0)).My;

        await CreateFileInMy("used_space_multi_2.docx", Owner);
        var s2 = (await WaitForUsedSpaceAsync(s => s.My > s1)).My;

        await CreateFileInMy("used_space_multi_3.docx", Owner);
        var s3 = (await WaitForUsedSpaceAsync(s => s.My > s2)).My;

        // Assert
        s1.Should().BeGreaterThan(s0);
        s2.Should().BeGreaterThan(s1);
        s3.Should().BeGreaterThan(s2);
        (s3 - s0).Should().Be(s1 - s0 + (s2 - s1) + (s3 - s2),
            "the total increase must equal the sum of the individual increments");
    }

    /// <summary>
    /// Renaming a file only changes its title, never its stored content, so it must not move the
    /// used space counter.
    /// </summary>
    [Fact]
    [Trait("Bug", "81648")]
    public async Task UsedSpace_DoesNotChangeAfterRenamingFile()
    {
        // Arrange
        var file = await CreateFileInMy("used_space_rename_before.docx", Owner);
        var before = await GetBaselineUsedSpaceAsync();

        // Act
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFile("used_space_rename_after.docx"), TestContext.Current.CancellationToken);

        // Assert
        var after = await GetUsedSpaceAsync();
        after.My.Should().Be(before.My,
            "renaming a file must not affect the used space it occupies");
    }

    [Fact]
    public async Task GetUsedSpace_AllSections_HaveCorrectDataTypes()
    {
        // Arrange
        await InitializeAllSections();

        // Act
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        foreach (var section in new[] { sections.MyDocumentsUsedSpace, sections.TrashUsedSpace, sections.ArchiveUsedSpace, sections.RoomsUsedSpace })
        {
            section.Should().NotBeNull();
            // FilesStatisticsFolder.UsedSpace is a `long`, so it can only ever carry a whole number of bytes.
            section!.UsedSpace.Should().BeGreaterThanOrEqualTo(0);
            section.Title.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>
    /// A soft delete must not lose or double-count space: what is removed from "My documents" must
    /// be exactly what is added to the Trash.
    /// </summary>
    [Fact]
    public async Task SoftDelete_ConservesTotalUsedSpace_MovingItFromMyDocumentsToTrash()
    {
        // Arrange - warm up so the injections both TS and here guard against happen before measuring
        await CreateFileInMy("used_space_conservation_warmup_create.docx", Owner);
        var warmupDelete = await CreateFileInMy("used_space_conservation_warmup_delete.docx", Owner);
        await DeleteFileAndWait(warmupDelete.Id, immediately: false);

        var file = await CreateFileInMy("used_space_conservation_target.docx", Owner);
        var before = await GetBaselineUsedSpaceAsync();

        // Act
        await DeleteFileAndWait(file.Id, immediately: false);

        // Assert
        var after = await WaitForUsedSpaceAsync(s => s.My < before.My);
        after.My.Should().BeLessThan(before.My,
            "soft deleting a file must release the space of \"My documents\"");
        after.Trash.Should().BeGreaterThan(before.Trash,
            "soft deleting a file must add the same amount to the Trash section");
        (after.Trash - before.Trash).Should().Be(before.My - after.My,
            "the space removed from \"My documents\" must equal the space added to the Trash");
    }

    /// <summary>
    /// Pre-existing files in My Documents must be counted even when <c>GetFilesUsedSpaceAsync</c>
    /// is the very first folder call of the session - the counter is not supposed to depend on some
    /// other endpoint having warmed anything up first.
    /// </summary>
    [Fact]
    [Trait("Bug", "81648")]
    public async Task MyDocumentsUsedSpace_ReportsPreexistingFiles()
    {
        // Arrange
        var file = await CreateFileInMy("used_space_preexisting.docx", Owner);
        file.PureContentLength.Should().BePositive();

        // Act
        var sections = (await _foldersApi.GetFilesUsedSpaceAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        sections.MyDocumentsUsedSpace.Should().NotBeNull();
        sections.MyDocumentsUsedSpace.UsedSpace.Should().BeGreaterThan(0,
            "files already present in My Documents must be counted even on the first statistics call");
    }

    /// <summary>
    /// Seeds every section the shape assertions look at: a file in My Documents and an archived
    /// room with a file in it (which also seeds the Rooms and Archive sections).
    /// </summary>
    private async Task InitializeAllSections()
    {
        await _filesClient.Authenticate(Owner);
        await CreateFileInMy("used_space_init.docx", Owner);

        var room = await CreateCustomRoom("used_space_init_room_" + Guid.NewGuid().ToString()[..8]);
        await ArchiveRoomAndWait(room.Id);
    }

    private async Task ArchiveRoomAndWait(int roomId)
    {
        var operation = (await _roomsApi.ArchiveRoomAsync(roomId, new ArchiveRoomRequest(false), TestContext.Current.CancellationToken)).Response;

        await WaitLongOperation(operation.Id);
    }
}
