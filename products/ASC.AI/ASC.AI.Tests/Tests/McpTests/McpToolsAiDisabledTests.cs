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
/// The tools surface with the portal AI switch off: every route but the catalogue is 403, a refused write changes
/// nothing, and the catalogue stays readable.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpToolsAiDisabledTests(AspireAppFixture fixture) : McpToolsPermissionTestBase(fixture)
{
    public static TheoryData<string> GatedRoutes()
    {
        var data = new TheoryData<string>();

        foreach (var route in _readOps.Concat(_writeOps).Concat(_manageOps))
        {
            data.Add(route);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(GatedRoutes))]
    public async Task Route_WithAiAccessDisabled_Is403_AndARefusedWriteChangesNothing(string route)
    {
        await RegisterAsync(OwnersServer, UrlConfig());
        await RegisterAsync("autotest-throwaway-server", UrlConfig("throwaway.example.invalid"));
        await SetDisabledAsync(WebSearch, "calculate");
        await SetAllowAlwaysAsync("docspace", "delete_file");
        var before = await SnapshotAsync();

        // The same call with AI on is the control: the route is not refused for any other reason.
        var control = await RunAsync(route, McpRole.DocSpaceAdmin);
        control.Code.Should().Be(200, "control, AI on: " + control);
        await ResetAsync(before);

        await SetPortalAiAccessAsync(false);
        var refused = await RunAsync(route, McpRole.DocSpaceAdmin);
        await SetPortalAiAccessAsync(true);

        refused.Code.Should().Be(403, refused.ToString());
        refused.Error.Should().Be("Forbidden");
        (await SnapshotAsync()).Should().Be(before, "the refused call changed nothing");
    }

    /// <summary>list-system-tools is the one route AI-off does not gate.</summary>
    [Fact]
    public async Task ListSystemTools_StaysReadableWithAiAccessDisabled()
    {
        (await GetToolsAsync("list-system-tools")).Code.Should().Be(200);

        await SetPortalAiAccessAsync(false);
        var reply = await GetToolsAsync("list-system-tools");

        reply.Code.Should().Be(200, reply.ToString());
        var system = reply.Json!.Value.GetProperty("system").EnumerateArray().Select(s => s.GetString()).ToList();
        reply.Json!.Value.GetProperty("groups").EnumerateObject().Select(p => p.Name).Except(system).Should().BeEmpty("no registered server appears");
    }

    /// <summary>Puts the state a control call changed back, so the refused call is measured against the same state.</summary>
    private async Task ResetAsync(string expectedSnapshot)
    {
        if (await SnapshotAsync() == expectedSnapshot)
        {
            return;
        }

        await ReplaceAllAsync(new Dictionary<string, object> { [OwnersServer] = UrlConfig(), ["autotest-throwaway-server"] = UrlConfig("throwaway.example.invalid") });
        await SetDisabledAsync(WebSearch, "calculate");
        await SetAllowAlwaysAsync("docspace", "delete_file");
    }
}
