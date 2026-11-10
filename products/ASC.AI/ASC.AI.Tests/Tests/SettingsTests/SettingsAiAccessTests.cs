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

namespace ASC.AI.Tests.Tests.SettingsTests;

/// <summary>
/// "Managing the AI services is a portal administrator's job, not only the payer's": who may flip the portal AI switch
/// (<c>PUT /api/2.0/settings/ai-access</c>). The owner and a DocSpaceAdmin may, a RoomAdmin, a User and a Guest may not
/// (403, and the switch does not move), anonymous gets 401, and an administrator's flip is portal-wide: it gates AI for
/// another member's session too. The gated route used to prove a flip reached the AI service is
/// <c>web-search/is-configured</c> (200 for every member type but a Guest, 403 with AI off).
/// </summary>
/// <remarks>
/// Not ported: the payer boundary (an administrator manages the AI add-ons but not the wallet) and the provisioning
/// sequence that pays for the AI Tools service — both are SaaS billing, which the standalone stand does not have. The
/// provisioning case is reduced to what exists here: an administrator makes the portal AI-ready by adding a profile.
/// </remarks>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Settings")]
public class SettingsAiAccessTests(AspireAppFixture fixture) : SettingsTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task SetAiAccess_Manager_TurnsThePortalAiSwitchOffAndBackOn(EmployeeType? type)
    {
        var manager = type is null ? Owner : await ActAsMemberAsync(type.Value);

        // The premise: a fresh portal ships the switch on, so "off" is a real change and the gated route is reachable.
        (await ReadAiAccessAsync(manager)).Should().BeTrue();
        (await WaitForTheAiGateAsync(open: true)).Should().Be(HttpStatusCode.OK, "before the flip");

        (await TrySetAiAccessAsync(manager, false)).Should().Be(200);
        (await ReadAiAccessAsync(manager)).Should().BeFalse("the switch reads back as off");

        // A 200 on the PUT is not the same answer as AI being off: the flip has to reach the routes the switch gates.
        var gated = await WaitForTheAiGateAsync(open: false);

        (await TrySetAiAccessAsync(manager, true)).Should().Be(200);
        (await ReadAiAccessAsync(manager)).Should().BeTrue("the switch reads back as on again");
        var reopened = await WaitForTheAiGateAsync(open: true);

        gated.Should().Be(HttpStatusCode.Forbidden, "with AI switched off");
        reopened.Should().Be(HttpStatusCode.OK, "with AI switched back on");
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SetAiAccess_RoleBelowAdministrator_Gets403AndTheSwitchDoesNotMove(EmployeeType type)
    {
        (await ReadAiAccessAsync(Owner)).Should().BeTrue("the switch is on before the attempt");
        var member = await InviteMemberAsync(type);

        var attempt = await TrySetAiAccessAsync(member, false);

        // Stored state first: a refusal that still wrote is the worse defect.
        (await ReadAiAccessAsync(Owner)).Should().BeTrue($"{type} must not have changed the switch");
        attempt.Should().Be(403);

        // Positive control: the route works in this very session, so the 403 is this role being refused and not a
        // switch nobody can move.
        (await TrySetAiAccessAsync(Owner, false)).Should().Be(200, "the owner can still flip the switch");
        (await ReadAiAccessAsync(Owner)).Should().BeFalse();
    }

    [Fact]
    public async Task SetAiAccess_Anonymous_Returns401AndTheSwitchDoesNotMove()
    {
        var status = await TrySetAiAccessAsync(null, false);

        (await ReadAiAccessAsync(Owner)).Should().BeTrue("an unauthenticated call must not have changed the switch");
        status.Should().Be(401);
    }

    [Fact]
    public async Task SetAiAccess_ByADocSpaceAdmin_GatesAiForTheOtherMembersToo()
    {
        // The switch is portal-wide, and an admin who could only gate themselves would satisfy every per-role
        // assertion above while the requirement went unmet. So the flip is made by the admin and measured in a
        // User's session.
        var admin = await InviteMemberAsync(EmployeeType.DocSpaceAdmin);
        var user = await InviteMemberAsync(EmployeeType.User);

        await _aiChatClient.Authenticate(user);
        (await WaitForTheAiGateAsync(open: true)).Should().Be(HttpStatusCode.OK, "as the User before the flip");

        (await TrySetAiAccessAsync(admin, false)).Should().Be(200);
        (await ReadAiAccessAsync(admin)).Should().BeFalse("the admin's flip is stored");
        var gated = await WaitForTheAiGateAsync(open: false);

        (await TrySetAiAccessAsync(admin, true)).Should().Be(200);
        var reopened = await WaitForTheAiGateAsync(open: true);

        gated.Should().Be(HttpStatusCode.Forbidden, "the admin's flip reaches another member's session");
        reopened.Should().Be(HttpStatusCode.OK, "as the User after the flip");
    }

    [Fact]
    public async Task ProvisionAi_ByADocSpaceAdmin_MakesThePortalAiReady()
    {
        var admin = await ActAsMemberAsync(EmployeeType.DocSpaceAdmin);
        (await ReadConfigAsync()).AiReady.Should().BeFalse("a portal with no profile is not AI-ready");

        // The administrator's management steps that exist without billing: the switch is on, and a model is added.
        (await TrySetAiAccessAsync(admin, true)).Should().Be(200);
        await MakePortalAiReadyAsync();

        var config = await ReadConfigAsync();
        config.AiReady.Should().BeTrue();
        config.SystemAiEnabled.Should().BeFalse("no gateway is configured");
    }
}
