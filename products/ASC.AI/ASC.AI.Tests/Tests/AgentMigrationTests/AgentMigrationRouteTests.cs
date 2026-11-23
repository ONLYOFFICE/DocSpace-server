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

[Trait("Category", "Permissions")]
[Trait("Feature", "AI/AgentMigration")]
public class AgentMigrationRouteTests(AspireAppFixture fixture) : AgentMigrationTestBase(fixture)
{
    [Fact]
    public async Task Migrate_PortalWithoutAgents_ReturnsZeros()
    {
        var result = await MigrateAgentsAsync();

        result.Migrated.Should().Be(0);
        result.Failed.Should().Be(0);
    }

    [Fact]
    public async Task Migrate_RepeatedCall_ReturnsZeros()
    {
        await MigrateAgentsAsync();

        var result = await MigrateAgentsAsync();

        result.Migrated.Should().Be(0);
        result.Failed.Should().Be(0);
    }

    [Fact]
    public async Task Migrate_DocSpaceAdmin_Returns200()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(admin);

        using var response = await PostAgentsMigrationAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Migrate_OwnerWithTwoAgents_ReturnsMigratedTwo()
    {
        await CreateAgentAsync(NewTitle("First agent"));
        await CreateAgentAsync(NewTitle("Second agent"));
        await _aiClient.Authenticate(Owner);

        var result = await MigrateAgentsAsync();

        result.Migrated.Should().Be(2);
        result.Failed.Should().Be(0);
        (await AgentsInAnySectionAsync()).Should().BeEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task Migrate_MemberBelowDocSpaceAdmin_Returns403AndConvertsNothing(EmployeeType employeeType)
    {
        var agent = await CreateAgentAsync(NewTitle("Agent"));
        var member = await InviteContact(employeeType, TestContext.Current.CancellationToken);
        await _aiClient.Authenticate(member);

        using var response = await PostAgentsMigrationAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await RoomInfoAsync(agent.Id)).RoomType.Should().Be(RoomType.AiRoom, "a refused call converts no agent");
    }

    [Fact]
    public async Task Migrate_Unauthorized_Returns401()
    {
        await _aiClient.Authenticate(null);

        using var response = await PostAgentsMigrationAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Migrate_AiSwitchedOff_ConvertsTheAgentsAllTheSame()
    {
        var agent = await CreateAgentAsync(NewTitle("Agent"));
        await SwitchPortalAiOffAsync();
        await _aiClient.Authenticate(Owner);

        var result = await MigrateAgentsAsync();

        result.Migrated.Should().Be(1);
        result.Failed.Should().Be(0);

        // the AI sections cannot be listed while the switch is off, so it is turned on again for the check
        await SetPortalAiAccessAsync(true);
        (await AgentsInAnySectionAsync()).Select(room => room.Id).Should().NotContain(agent.Id);
    }
}
