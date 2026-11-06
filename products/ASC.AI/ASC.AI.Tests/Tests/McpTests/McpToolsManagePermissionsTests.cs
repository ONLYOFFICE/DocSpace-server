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
/// Who may manage the server registry: only a DocSpace administrator. A refused write must also leave the registry
/// untouched — a 403 that still wrote is the failure a status-only assertion cannot see.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpToolsManagePermissionsTests(AspireAppFixture fixture) : McpToolsPermissionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(ManageMatrix), MemberType = typeof(McpToolsPermissionTestBase))]
    public async Task ManageRoute_IsAllowedOnlyToADocSpaceAdmin(McpRole role, string op)
    {
        await ArrangeMemberAsync(role);
        var allowed = role == McpRole.DocSpaceAdmin;

        var reply = await RunAsync(op, role);

        // Read back as the owner first.
        await ActAsAsync(Owner);
        var servers = await ListServersAsync();

        if (allowed)
        {
            reply.Code.Should().Be(200, reply.ToString());
            reply.Success.Should().BeTrue("these routes report soft failures as 200 + success:false, so the status alone does not say the write was accepted: " + reply);
        }
        else
        {
            reply.Code.Should().Be(403, reply.ToString());
            reply.Error.Should().Be("Forbidden");
        }

        string[] expected = op switch
        {
            "add-custom-server" => allowed ? [OwnersServer, $"{role}-server"] : [OwnersServer],
            "remove-custom-server" => allowed ? [] : [OwnersServer],
            "replace-all-custom-servers" => allowed ? [$"{role}-replaced"] : [OwnersServer],
            _ => [OwnersServer]
        };
        servers.Keys.Should().BeEquivalentTo(expected, $"after {op} as {role}");

        if (op == "update-custom-server")
        {
            ShouldBeConfig(servers[OwnersServer], allowed ? UrlConfig("updated.example.invalid") : UrlConfig(), $"after {op} as {role}");
        }
    }
}
