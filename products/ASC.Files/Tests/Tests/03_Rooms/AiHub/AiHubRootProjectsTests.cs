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
/// The AI rooms section of the root folders (`GET api/2.0/files/@root`) lists at most five AI projects the caller
/// created or was invited to: the pinned ones first, then the others, each group by the last change, newest first,
/// whatever paging, filters and order the request carries.
/// </summary>
[Trait("Category", "Rooms")]
[Trait("Feature", "AiHub")]
public class AiHubRootProjectsTests(AspireAppFixture fixture) : AiHubTestBase(fixture)
{
    [Fact]
    public async Task GetRootFolders_PinnedAndUnpinnedProjects_PinnedFirstThenNewestFirst()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var first = await CreateProjectAsync();
        var second = await CreateProjectAsync();
        var third = await CreateProjectAsync();
        var fourth = await CreateProjectAsync();
        var instructions = await CreateInstructionsRoomAsync();
        await PinRoomAsync(first);
        await PinRoomAsync(second);
        await PinRoomAsync(instructions);

        // Act
        var section = await GetRootAiHubSectionAsync(filterType: FilterType.FoldersOnly, count: 1, startIndex: 5, sortBy: "AZ", filterValue: "NoProjectHasThisTitle");

        // Assert
        IdsOf(section).Should().Equal(second, first, fourth, third);
        PinnedOf(section).Should().Equal(second, first);
        section.Total.Should().Be(4);
        section.Count.Should().Be(4);
        section.StartIndex.Should().Be(0);
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain(instructions);
    }

    [Fact]
    public async Task GetRootFolders_NothingPinned_FiveNewestProjectsListed()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var projects = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            projects.Add(await CreateProjectAsync());
        }

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().Equal(Enumerable.Reverse(projects).Take(5));
        PinnedOf(section).Should().BeEmpty();
        section.Total.Should().Be(5);
    }

    [Fact]
    public async Task GetRootFolders_SixPinnedProjects_FiveNewestPinnedListedWithoutUnpinned()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var pinned = new List<int>();
        for (var i = 0; i < 6; i++)
        {
            pinned.Add(await CreateProjectAsync());
        }

        var unpinned = await CreateProjectAsync();

        foreach (var project in pinned)
        {
            await PinRoomAsync(project);
        }

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().Equal(Enumerable.Reverse(pinned).Take(5));
        IdsOf(section).Should().NotContain(unpinned);
        section.Total.Should().Be(5);
    }

    [Fact]
    public async Task GetRootFolders_OlderProjectRenamed_MovesToTheTop()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var older = await CreateProjectAsync();
        var middle = await CreateProjectAsync();
        var newer = await CreateProjectAsync();
        IdsOf(await GetRootAiHubSectionAsync()).Should().Equal(newer, middle, older);
        await Task.Delay(1_100, TestContext.Current.CancellationToken);

        // Act
        using var response = await UpdateRoomResponseAsync(older, new { title = "Autotest Renamed Project" });
        response.EnsureSuccessStatusCode();
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().Equal(older, newer, middle);
    }

    [Fact]
    public async Task GetRootFolders_OnlyInstructionsRoomInTheSection_SectionIsListedEmpty()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var instructions = await CreateInstructionsRoomAsync();
        await PinRoomAsync(instructions);

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        AiHubPinnedOf(await GetAiHubFoldersAsync()).Should().Contain(instructions);
        IdsOf(section).Should().BeEmpty();
        section.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetRootFolders_UnpinnedProject_MovesBehindThePinnedOnes()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var older = await CreateProjectAsync();
        var newer = await CreateProjectAsync();
        await PinRoomAsync(older);
        IdsOf(await GetRootAiHubSectionAsync()).Should().Equal(older, newer);

        // Act
        await UnpinRoomAsync(older);
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().Equal(newer, older);
        PinnedOf(section).Should().BeEmpty();
    }

    [Fact]
    public async Task GetRootFolders_CountOne_RoomsSectionKeepsItsPaging()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await CreateCustomRoom("Autotest Root Projects Room 1");
        await CreateCustomRoom("Autotest Root Projects Room 2");
        var project = await CreateProjectAsync();
        var otherProject = await CreateProjectAsync();
        await PinRoomAsync(project);
        var roomsRootId = (await _roomsApi.GetRoomsFolderAsync(searchArea: SearchArea.Active, cancellationToken: TestContext.Current.CancellationToken)).Response.Current.Id;

        // Act
        var sections = await GetRootSectionsAsync(count: 1);

        // Assert
        var rooms = sections.Single(s => s.Current.Id == roomsRootId);
        IdsOf(rooms).Should().HaveCount(1);
        rooms.Total.Should().Be(2);
        IdsOf(AiHubSectionOf(sections)).Should().Equal(project, otherProject);
    }

    [Fact]
    public async Task GetRootFolders_MemberPinsProject_PinnedForTheMemberOnly()
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
        PinnedOf(memberSection).Should().Equal(project);
        IdsOf(ownerSection).Should().Equal(project);
        PinnedOf(ownerSection).Should().BeEmpty();
    }

    [Fact]
    public async Task GetRootFolders_MemberLosesAccessAndRegainsIt_ListedAgainWithThePin()
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
        IdsOf(await GetRootAiHubSectionAsync()).Should().Equal(project, unpinnedProject);

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
        IdsOf(whileOut).Should().Equal(unpinnedProject);
        whileOut.Total.Should().Be(1);
        IdsOf(afterReinvite).Should().Equal(project, unpinnedProject);
        PinnedOf(afterReinvite).Should().Equal(project);
    }

    [Fact]
    public async Task GetRootFolders_DocSpaceAdminNeverInvited_ProjectsAreNotListedEvenPinned()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var project = await CreateProjectAsync();
        var unpinnedProject = await CreateProjectAsync();
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        await _filesClient.Authenticate(admin);
        await PinRoomAsync(project);
        AiHubIdsOf(await GetAiHubFoldersAsync()).Should().Contain([project, unpinnedProject], "the listing of the section shows every project to the admin");

        // Act
        var section = await GetRootAiHubSectionAsync();

        // Assert
        IdsOf(section).Should().BeEmpty();
        section.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetRootFolders_RoomAdminCreator_OwnProjectsListedWithoutShareRecord()
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
        IdsOf(section).Should().Equal(project, unpinnedProject);
        section.Total.Should().Be(2);
    }

    private static List<int> PinnedOf(FolderContentDto section)
    {
        return [.. section.Folders.OfType<FolderDto>().Where(f => f.Pinned == true).Select(f => f.Id)];
    }

    private static List<int> AiHubIdsOf(List<FolderDto> folders)
    {
        return [.. folders.Select(f => f.Id)];
    }

    private static List<int> AiHubPinnedOf(List<FolderDto> folders)
    {
        return [.. folders.Where(f => f.Pinned == true).Select(f => f.Id)];
    }
}
