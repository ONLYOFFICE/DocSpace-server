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

namespace ASC.People.Tests.Tests._13_Groups;

/// <summary>
/// GET /api/2.0/group - listing groups: basic shape, filtering, pagination and sorting.
/// The TS suite spreads the same "userId" / "manager=true" / "count" / "startIndex" assertions
/// across four separate describe blocks ("Query params", "Filtering", "Pagination", "Sorting")
/// that duplicate each other almost verbatim; they are merged here into one behavior per case.
/// </summary>
public class GroupsQueryTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private async Task<Guid> CreateGroupAsync(string name, Guid? manager = null, List<Guid>? members = null)
    {
        var group = await _groupApi.AddGroupAsync(
            new GroupRequestDto(groupName: name, groupManager: manager ?? Owner.Id, members: members!),
            TestContext.Current.CancellationToken);

        return group.Response.Id;
    }

    [Fact]
    public async Task GetGroups_ReturnsArrayWhereEveryGroupHasAnId()
    {
        await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var groups = await _groupApi.GetGroupsAsync(cancellationToken: TestContext.Current.CancellationToken);

        groups.Response.Should().NotBeNull();
        groups.Response.Should().OnlyContain(g => g.Id != Guid.Empty);
    }

    [Fact]
    public async Task GetGroups_FilterValue_ReturnsExactNameMatchOnly()
    {
        var uniqueName = Guid.NewGuid().ToString("N");
        await CreateGroupAsync(uniqueName);
        await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var groups = await _groupApi.GetGroupsAsync(filterValue: uniqueName, cancellationToken: TestContext.Current.CancellationToken);

        groups.Response.Should().ContainSingle(g => g.Name == uniqueName);
    }

    [Fact]
    public async Task GetGroups_FilterValue_MatchesByPartialName()
    {
        var uniquePart = Guid.NewGuid().ToString("N");
        var fullName = $"Prefix_{uniquePart}_Suffix";
        var targetGroupId = await CreateGroupAsync(fullName);
        await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var groups = await _groupApi.GetGroupsAsync(filterValue: uniquePart, cancellationToken: TestContext.Current.CancellationToken);

        groups.Response.Select(g => g.Id).Should().Contain(targetGroupId);
    }

    [Fact]
    public async Task GetGroups_FilterValue_NoMatches_ReturnsEmptyArray()
    {
        await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var groups = await _groupApi.GetGroupsAsync(
            filterValue: $"NO_MATCH_{Guid.NewGuid():N}",
            cancellationToken: TestContext.Current.CancellationToken);

        groups.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroups_UserId_ReturnsOnlyGroupsWhereUserIsMember()
    {
        var member = await InviteContact(EmployeeType.User);

        var memberGroupId = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [member.Id]);
        var otherGroupId = await CreateGroupAsync(Guid.NewGuid().ToString("N"));

        var groups = await _groupApi.GetGroupsAsync(userId: member.Id, cancellationToken: TestContext.Current.CancellationToken);

        var ids = groups.Response.Select(g => g.Id).ToList();
        ids.Should().Contain(memberGroupId);
        ids.Should().NotContain(otherGroupId);
    }

    [Fact]
    public async Task GetGroups_ManagerTrue_ReturnsOnlyGroupsWhereUserIsManager()
    {
        var user = await InviteContact(EmployeeType.User);

        var managerGroupId = await CreateGroupAsync(Guid.NewGuid().ToString("N"), manager: user.Id);
        var memberOnlyGroupId = await CreateGroupAsync(Guid.NewGuid().ToString("N"), members: [user.Id]);

        var groups = await _groupApi.GetGroupsAsync(userId: user.Id, manager: true, cancellationToken: TestContext.Current.CancellationToken);

        var ids = groups.Response.Select(g => g.Id).ToList();
        ids.Should().Contain(managerGroupId);
        ids.Should().NotContain(memberOnlyGroupId);
    }

    [Fact]
    public async Task GetGroups_Count_LimitsTheNumberOfReturnedGroups()
    {
        for (var i = 0; i < 5; i++)
        {
            await CreateGroupAsync(Guid.NewGuid().ToString("N"));
        }

        var groups = await _groupApi.GetGroupsAsync(count: 3, cancellationToken: TestContext.Current.CancellationToken);

        groups.Response.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetGroups_StartIndex_ShiftsTheReturnedPage()
    {
        for (var i = 0; i < 3; i++)
        {
            await CreateGroupAsync(Guid.NewGuid().ToString("N"));
        }

        var withoutOffset = await _groupApi.GetGroupsAsync(count: 2, startIndex: 0, cancellationToken: TestContext.Current.CancellationToken);
        var withOffset = await _groupApi.GetGroupsAsync(count: 2, startIndex: 1, cancellationToken: TestContext.Current.CancellationToken);

        var firstIds = withoutOffset.Response.Select(g => g.Id).ToList();
        var secondIds = withOffset.Response.Select(g => g.Id).ToList();

        firstIds[0].Should().NotBe(secondIds[0]);
        firstIds[1].Should().Be(secondIds[0]);
    }

    [Fact]
    public async Task GetGroups_CountAndStartIndex_ProduceNoDuplicatesAcrossPages()
    {
        for (var i = 0; i < 4; i++)
        {
            await CreateGroupAsync(Guid.NewGuid().ToString("N"));
        }

        var page1 = await _groupApi.GetGroupsAsync(count: 2, startIndex: 0, cancellationToken: TestContext.Current.CancellationToken);
        var page2 = await _groupApi.GetGroupsAsync(count: 2, startIndex: 2, cancellationToken: TestContext.Current.CancellationToken);

        var page1Ids = page1.Response.Select(g => g.Id);
        var page2Ids = page2.Response.Select(g => g.Id);

        page1Ids.Should().NotIntersectWith(page2Ids);
    }

    [Fact]
    public async Task GetGroups_StartIndexGreaterThanTotal_ReturnsEmptyArray()
    {
        for (var i = 0; i < 2; i++)
        {
            await CreateGroupAsync(Guid.NewGuid().ToString("N"));
        }

        var groups = await _groupApi.GetGroupsAsync(startIndex: 100, cancellationToken: TestContext.Current.CancellationToken);

        groups.Response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroups_SortByNameAscending_ReturnsGroupsInAscendingOrder()
    {
        var nameA = $"A_{Guid.NewGuid():N}";
        var nameM = $"M_{Guid.NewGuid():N}";
        var nameZ = $"Z_{Guid.NewGuid():N}";

        foreach (var name in new[] { nameZ, nameA, nameM })
        {
            await CreateGroupAsync(name);
        }

        var groups = await _groupApi.GetGroupsAsync(
            sortBy: "Name",
            sortOrder: SortOrder.Ascending,
            cancellationToken: TestContext.Current.CancellationToken);

        var names = groups.Response.Select(g => g.Name).ToList();
        for (var i = 0; i < names.Count - 1; i++)
        {
            string.CompareOrdinal(names[i], names[i + 1]).Should().BeLessThanOrEqualTo(0);
        }
    }

    [Fact]
    public async Task GetGroups_SortByNameDescending_ReturnsGroupsInDescendingOrder()
    {
        var nameA = $"A_{Guid.NewGuid():N}";
        var nameM = $"M_{Guid.NewGuid():N}";
        var nameZ = $"Z_{Guid.NewGuid():N}";

        foreach (var name in new[] { nameA, nameZ, nameM })
        {
            await CreateGroupAsync(name);
        }

        var groups = await _groupApi.GetGroupsAsync(
            sortBy: "Name",
            sortOrder: SortOrder.Descending,
            cancellationToken: TestContext.Current.CancellationToken);

        var names = groups.Response.Select(g => g.Name).ToList();
        for (var i = 0; i < names.Count - 1; i++)
        {
            string.CompareOrdinal(names[i], names[i + 1]).Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
