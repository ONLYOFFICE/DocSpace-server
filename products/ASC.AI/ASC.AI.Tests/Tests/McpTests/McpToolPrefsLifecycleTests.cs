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
/// The tools surface across routes: a server that leaves the registry takes its disabled and allow-always entries with
/// it, the two preference lists are independent of each other, and each user keeps their own.
/// </summary>
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Mcp")]
public class McpToolPrefsLifecycleTests(AspireAppFixture fixture) : McpTestBase(fixture)
{
    [Fact]
    public async Task Remove_AServer_TakesItsDisabledAndAllowAlwaysEntriesWithIt_AndANewOneStartsClean()
    {
        await RegisterAsync("doomed");
        await RegisterAsync("bystander", UrlConfig("bystander.example.invalid"));

        foreach (var serverType in new[] { "doomed", "bystander" })
        {
            await SetDisabledAsync(serverType, "t1", "t2");
            await SetAllowAlwaysAsync(serverType, "t1");
        }

        await SetAllowAlwaysAsync("docspace", "generate_docx");

        (await RemoveServerAsync("doomed")).Success.Should().BeTrue();

        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["bystander"] = ["t1", "t2"] });
        (await AllowedAsync()).Order().Should().Equal("bystander_t1", "docspace_generate_docx");

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _aiTools.AiToolsSetDisabledAsync(
                new AiToolsSetDisabledRequest(serverType: "doomed", toolNames: ["t3"]),
                TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(400, "a removed server is no longer a valid serverType");

        await RegisterAsync("doomed", UrlConfig("again.example.invalid"));

        (await DisabledAsync()).Should().BeEquivalentTo(
            new Dictionary<string, List<string>> { ["bystander"] = ["t1", "t2"] },
            "the new `doomed` inherited no disabled tools");
        (await AllowedAsync()).Should().NotContain(t => t.StartsWith("doomed_", StringComparison.Ordinal), "and no standing approvals");
        (await IsAllowedAsync("doomed", "t1")).Should().BeFalse();
    }

    /// <summary>A server dropped by replace-all must lose its entries like a removed one, or a re-registered namesake inherits an approval.</summary>
    [Fact]
    [Trait("Bug", "84406")]
    public async Task ReplaceAll_ADroppedServer_LosesItsEntriesLikeARemovedOneDoes()
    {
        await RegisterAsync("kept");
        await RegisterAsync("dropped", UrlConfig("dropped.example.invalid"));

        foreach (var serverType in new[] { "kept", "dropped" })
        {
            await SetDisabledAsync(serverType, "t");
            await SetAllowAlwaysAsync(serverType, "t");
        }

        (await ReplaceAllAsync(new Dictionary<string, object> { ["kept"] = UrlConfig() })).Success.Should().BeTrue();
        await RegisterAsync("dropped", UrlConfig("dropped.example.invalid"));

        (await IsAllowedAsync("dropped", "t")).Should().BeFalse("the re-registered server must not inherit an approval");
        (await AllowedAsync()).Should().Equal("kept_t");
        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["kept"] = ["t"] });
    }

    [Fact]
    public async Task SetDisabledAndSetAllowAlways_AreIndependent_AndADisabledToolIsNotEnabledByItsApproval()
    {
        await RegisterAsync("srv");

        await SetAllowAlwaysAsync("srv", "t");
        (await DisabledAsync()).Should().BeEmpty("allow wrote no disabled");

        await SetDisabledAsync("srv", "t");
        (await AllowedAsync()).Should().BeEquivalentTo(["srv_t"], "disable kept the approval");
        (await IsDisabledAsync("srv", "t")).Should().BeTrue("the approval did not un-disable the tool");

        await SetDisabledAsync("srv");
        (await AllowedAsync()).Should().BeEquivalentTo(["srv_t"], "enabling does not revoke the approval");

        await SetAllowAlwaysAsync("srv", "t", false);
        await SetDisabledAsync("srv", "t");
        await SetAllowAlwaysAsync("srv", "t", false);
        (await DisabledAsync()).Should().BeEquivalentTo(
            new Dictionary<string, List<string>> { ["srv"] = ["t"] },
            "revoking the approval does not enable the tool");
    }

    /// <summary>
    /// The disabled and allow-always state is per user: if it were shared, a plain User could disarm the owner's
    /// confirmation dialog for a tool as destructive as delete_file.
    /// </summary>
    [Fact]
    public async Task AllowAlways_OneUsersPreApproval_DoesNotDisarmAnotherUsersDialog()
    {
        await SetAllowAlwaysAsync("docspace", "delete_file");
        var member = await InviteRoleAsync(McpRole.User);

        await ActAsAsync(member);
        await SetAllowAlwaysAsync("docspace", "create_room");

        // The member's own write proves the surface works for them, so the owner's tool being absent is a real absence.
        (await AllowedAsync()).Should().Equal("docspace_create_room");
        (await IsAllowedAsync("docspace", "delete_file")).Should().BeFalse("the owner's pre-approval must not answer for the member");

        await ActAsAsync(Owner);
        (await AllowedAsync()).Should().Equal("docspace_delete_file");
        (await IsAllowedAsync("docspace", "create_room")).Should().BeFalse("a User must not be able to pre-approve a tool for the owner");
    }

    [Fact]
    public async Task Preferences_AnInvitedUserStartsEmpty_WritesTheirOwn_AndNeitherSideSeesTheOthers()
    {
        await RegisterAsync("portal-secret");
        await SetDisabledAsync("portal-secret", "owner-disabled");
        await SetAllowAlwaysAsync("portal-secret", "owner-allowed");
        var member = await InviteRoleAsync(McpRole.User);

        await ActAsAsync(member);
        (await DisabledAsync()).Should().BeEmpty("the owner's disabled list is not the member's");
        (await AllowedAsync()).Should().BeEmpty();

        await SetDisabledAsync("portal-secret", "user-disabled");
        await SetAllowAlwaysAsync("portal-secret", "user-allowed");
        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["portal-secret"] = ["user-disabled"] });
        (await AllowedAsync()).Should().Equal("portal-secret_user-allowed");

        await ActAsAsync(Owner);
        (await DisabledAsync()).Should().BeEquivalentTo(new Dictionary<string, List<string>> { ["portal-secret"] = ["owner-disabled"] });
        (await AllowedAsync()).Should().Equal("portal-secret_owner-allowed");
        (await IsDisabledAsync("portal-secret", "user-disabled")).Should().BeFalse("the member's disable does not reach the owner");
    }
}
