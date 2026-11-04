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
/// The two settings writes any non-guest member may call (<c>set-disabled</c>, <c>set-allow-always</c>): they land in
/// the caller's own list. A Guest is refused on both.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpToolsSettingsPermissionsTests(AspireAppFixture fixture) : McpToolsPermissionTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(WriteMatrix), MemberType = typeof(McpToolsPermissionTestBase))]
    public async Task SettingsWrite_IsOpenToEveryNonGuestMember_AndLandsInTheirOwnList(McpRole role, string op)
    {
        await ArrangeMemberAsync(role);

        var reply = await RunAsync(op, role);

        if (role == McpRole.Guest)
        {
            // No side effect to check: the settings are per user and the Guest is refused the matching read too.
            reply.Code.Should().Be(403, reply.ToString());
            reply.Error.Should().Be("Forbidden");
            return;
        }

        reply.Code.Should().Be(200, reply.ToString());
        reply.Success.Should().BeTrue(reply.ToString());

        if (op == "set-disabled")
        {
            (await IsDisabledAsync(WebSearch, "calculate")).Should().BeTrue();
        }
        else
        {
            (await IsAllowedAsync("docspace", "delete_file")).Should().BeTrue();
        }
    }
}
