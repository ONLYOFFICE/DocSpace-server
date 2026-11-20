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

namespace ASC.Files.Tests.Tests._03_Rooms.AiHub;

/// <summary>
/// The AI rooms section of the root folders (`GET api/2.0/files/@root`) lists the AI projects the caller pinned and
/// may see, whatever paging and filters the request carries, sorted as the listing of the section sorts pinned rooms.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiHubRootPinnedProjectsTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetRootFolders_PinnedProjects_OnlyTheyAreListed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var first = await CreateProjectAsync();
        var second = await CreateProjectAsync();
        var unpinned = await CreateProjectAsync();
        var instructions = await CreateInstructionsRoomAsync();
        await PinRoomAsync(first);
        await PinRoomAsync(second);
        await PinRoomAsync(instructions);

        // Act
        var section = await GetRootAiHubSectionAsync(filterType: FilterType.FoldersOnly, count: 1);

        // Assert
        IdsOf(section).Should().BeEquivalentTo([first, second]);
        section.Total.Should().Be(2);
        section.Count.Should().Be(2);
        section.StartIndex.Should().Be(0);
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain([unpinned, instructions]);
    }

    [Fact]
    public async Task GetRootFolders_NothingPinned_SectionIsListedEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var project = await CreateProjectAsync();

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain(project);
        IdsOf(section).Should().BeEmpty();
        section.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetRootFolders_OnlyInstructionsRoomPinned_SectionIsListedEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateProjectAsync();
        var instructions = await CreateInstructionsRoomAsync();
        await PinRoomAsync(instructions);

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        PinnedOf(await GetAiHubFoldersAsync()).Should().Contain(instructions);
        IdsOf(section).Should().BeEmpty();
        section.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetRootFolders_TenPinnedProjects_PagingAndFiltersDoNotCutTheListSortedByTitle()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projects = new List<int>();
        for (var i = 0; i < 10; i++)
        {
            projects.Add(await CreateProjectAsync($"Autotest Pinned Project {i}"));
        }

        foreach (var project in projects)
        {
            await PinRoomAsync(project);
        }

        // Act
        var section = await GetRootAiHubSectionAsync(count: 1, startIndex: 5, sortBy: "AZ", filterValue: "NoProjectHasThisTitle");

        // Assert
        IdsOf(section).Should().Equal(projects);
        section.Total.Should().Be(10);
        section.Count.Should().Be(10);
        section.StartIndex.Should().Be(0);
    }

    [Fact]
    public async Task GetRootFolders_UnpinnedProject_LeavesTheListAndStaysInTheSection()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var project = await CreateProjectAsync();
        await PinRoomAsync(project);
        IdsOf(await GetRootAiHubSectionAsync()).Should().Contain(project);

        // Act
        await UnpinRoomAsync(project);
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().NotContain(project);
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain(project);
    }

    [Fact]
    public async Task GetRootFolders_CountOne_RoomsSectionKeepsItsPaging()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest Pinned Projects Room 1");
        await CreateCustomRoom("Autotest Pinned Projects Room 2");
        var project = await CreateProjectAsync();
        var otherProject = await CreateProjectAsync();
        await PinRoomAsync(project);
        await PinRoomAsync(otherProject);
        var roomsRootId = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        // Act
        var sections = await GetRootSectionsAsync(count: 1);

        // Assert
        var rooms = sections.Single(s => s.Current.Id == roomsRootId);
        IdsOf(rooms).Should().HaveCount(1);
        rooms.Total.Should().Be(2);
        IdsOf(AiHubSectionOf(sections)).Should().BeEquivalentTo([project, otherProject]);
    }

    [Fact]
    public async Task GetRootFolders_MemberPinsProject_ListedForTheMemberNotForTheOwner()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var project = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(project, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        await PinRoomAsync(project);

        // Act
        var memberSection = await GetRootAiHubSectionAsync();
        await _filesClient.Authenticate(Owner);
        var ownerSection = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(memberSection).Should().Equal(project);
        IdsOf(ownerSection).Should().NotContain(project);
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain(project, "the owner sees the project in the section, only the pin is missing");
    }

    [Fact]
    public async Task GetRootFolders_MemberLosesAccessAndRegainsIt_ListedAgainWithoutANewPin()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var project = await CreateProjectAsync();
        var unpinnedProject = await CreateProjectAsync();
        var member = await InviteContact(EmployeeType.User);
        await ShareRoomAsync(project, member.Id, FileShare.ContentCreator);
        await ShareRoomAsync(unpinnedProject, member.Id, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);
        await PinRoomAsync(project);
        IdsOf(await GetRootAiHubSectionAsync()).Should().Equal(project);

        // Act
        await _filesClient.Authenticate(Owner);
        await ShareRoomAsync(project, member.Id, FileShare.None);
        await _filesClient.Authenticate(member);
        var whileOut = await GetRootAiHubSectionAsync();

        await _filesClient.Authenticate(Owner);
        await ShareRoomAsync(project, member.Id, FileShare.ContentCreator);
        await _filesClient.Authenticate(member);
        var afterReinvite = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(whileOut).Should().BeEmpty();
        whileOut.Total.Should().Be(0);
        IdsOf(afterReinvite).Should().Equal(project);
    }

    [Fact]
    public async Task GetRootFolders_DocSpaceAdminNeverInvited_FindsTheProjectThatAdminPinned()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var project = await CreateProjectAsync();
        var unpinnedProject = await CreateProjectAsync();
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        await PinRoomAsync(project);

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().Equal(project);
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain([project, unpinnedProject], "the listing of the section shows every project to the admin");
    }

    [Fact]
    public async Task GetRootFolders_RoomAdminCreatorPinsProject_ListedWithoutShareRecord()
    {
        // Arrange
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);
        var project = await CreateProjectAsync();
        var unpinnedProject = await CreateProjectAsync();
        await PinRoomAsync(project);

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().Equal(project);
        section.Total.Should().Be(1);
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain(unpinnedProject);
    }

    private static List<int> AiHubIdsOf(List<FolderDto> folders)
    {
        return [.. folders.Select(f => f.Id)];
    }

    private static List<int> PinnedOf(List<FolderDto> folders)
    {
        return [.. folders.Where(f => f.Pinned == true).Select(f => f.Id)];
    }
}
