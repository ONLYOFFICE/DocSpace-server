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
/// The pinned SDK cannot read a room of the AI rooms section, so the root folders and the pin calls go over raw HTTP.
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
        var section = await GetRootAiHubSectionAsync("?filterType=2&count=1");

        // Assert
        IdsOf(section).Should().BeEquivalentTo([first, second]);
        section.GetProperty("total").GetInt32().Should().Be(2);
        section.GetProperty("count").GetInt32().Should().Be(2);
        section.GetProperty("startIndex").GetInt32().Should().Be(0);
        IdsOf(await GetAiHubSectionAsync()).Should().Contain([unpinned, instructions]);
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
        IdsOf(await GetAiHubSectionAsync()).Should().Contain(project);
        IdsOf(section).Should().BeEmpty();
        section.GetProperty("total").GetInt32().Should().Be(0);
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
        PinnedOf(await GetAiHubSectionAsync()).Should().Contain(instructions);
        IdsOf(section).Should().BeEmpty();
        section.GetProperty("total").GetInt32().Should().Be(0);
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
        var section = await GetRootAiHubSectionAsync("?count=1&startIndex=5&filterValue=NoProjectHasThisTitle&sortBy=AZ");

        // Assert
        IdsOf(section).Should().Equal(projects);
        section.GetProperty("total").GetInt32().Should().Be(10);
        section.GetProperty("count").GetInt32().Should().Be(10);
        section.GetProperty("startIndex").GetInt32().Should().Be(0);
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
        using var unpinned = await UnpinRoomResponseAsync(project);
        var section = await GetRootAiHubSectionAsync();

        // Assert
        unpinned.StatusCode.Should().Be(HttpStatusCode.OK);
        IdsOf(section).Should().NotContain(project);
        IdsOf(await GetAiHubSectionAsync()).Should().Contain(project);
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
        var sections = await GetRootSectionsAsync("?count=1");

        // Assert
        var rooms = sections.EnumerateArray().Single(s => s.GetProperty("current").GetProperty("id").GetInt32() == roomsRootId);
        IdsOf(rooms).Should().HaveCount(1);
        rooms.GetProperty("total").GetInt32().Should().Be(2);
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
        IdsOf(await GetAiHubSectionAsync()).Should().Contain(project, "the owner sees the project in the section, only the pin is missing");
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
        whileOut.GetProperty("total").GetInt32().Should().Be(0);
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
        IdsOf(await GetAiHubSectionAsync()).Should().Contain([project, unpinnedProject], "the listing of the section shows every project to the admin");
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
        section.GetProperty("total").GetInt32().Should().Be(1);
        IdsOf(await GetAiHubSectionAsync()).Should().Contain(unpinnedProject);
    }

    private static List<int> PinnedOf(JsonElement section)
    {
        return section.GetProperty("folders").EnumerateArray()
            .Where(f => f.TryGetProperty("pinned", out var pinned) && pinned.GetBoolean())
            .Select(f => f.GetProperty("id").GetInt32())
            .ToList();
    }
}
