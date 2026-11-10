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

namespace ASC.AI.Tests.Tests.McpTests;

/// <summary>
/// The disabled-tools list with <c>scope</c> <c>tenant</c>: the portal-wide list an administrator keeps, readable by
/// every member, written only by an administrator, and folded into each user's own list on read.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpToolPrefsTenantScopeTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    private const string Target = "autotest-tenant-scope";
    private const string TenantScope = "tenant";

    private async Task SetTenantDisabledAsync(params string[] toolNames)
    {
        var result = await _aiTools.AiToolsSetDisabledAsync(
            new AiToolsSetDisabledRequest(serverType: Target, toolNames: [.. toolNames], scope: AiChatToolPrefsScope.Tenant),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue();
    }

    private Task<Dictionary<string, List<string>>> TenantDisabledAsync() =>
        _aiTools.AiToolsGetDisabledAsync(TenantScope, null, TestContext.Current.CancellationToken);

    [Theory]
    [InlineData(McpRole.DocSpaceAdmin)]
    [InlineData(McpRole.RoomAdmin)]
    [InlineData(McpRole.User)]
    public async Task TenantList_WrittenByTheOwner_IsReadByEveryMember_AndFoldedIntoTheirOwnList(McpRole role)
    {
        await RegisterAsync(Target);
        await SetTenantDisabledAsync("calculate");
        await ActAsAsync(await InviteRoleAsync(role));

        var tenant = await TenantDisabledAsync();
        var own = await DisabledAsync();

        tenant.Should().ContainKey(Target).WhoseValue.Should().Equal("calculate");
        own.Should().ContainKey(Target, "the portal-wide list is folded into every member's own one on read")
            .WhoseValue.Should().Contain("calculate");
        (await IsDisabledAsync(Target, "calculate")).Should().BeTrue();
    }

    [Theory]
    [InlineData(McpRole.RoomAdmin)]
    [InlineData(McpRole.User)]
    public async Task TenantList_WrittenByANonAdministrator_Returns403AndKeepsTheList(McpRole role)
    {
        await RegisterAsync(Target);
        await SetTenantDisabledAsync("calculate");
        await ActAsAsync(await InviteRoleAsync(role));

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _aiTools.AiToolsSetDisabledAsync(
            new AiToolsSetDisabledRequest(serverType: Target, toolNames: ["get_time"], scope: AiChatToolPrefsScope.Tenant),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403, exception.ErrorContent!.ToString());
        (await TenantDisabledAsync())[Target].Should().Equal("calculate");
    }

    [Fact]
    public async Task TenantList_AnEmptyList_SwitchesEverythingBackOn()
    {
        await RegisterAsync(Target);
        await SetTenantDisabledAsync("calculate", "get_time");

        await SetTenantDisabledAsync();

        (await TenantDisabledAsync()).GetValueOrDefault(Target).Should().BeNullOrEmpty();
    }
}
