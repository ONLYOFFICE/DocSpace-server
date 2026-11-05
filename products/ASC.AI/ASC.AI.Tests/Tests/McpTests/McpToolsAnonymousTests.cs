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
/// Every route of the tools surface answers 401 to an anonymous caller — not a sample: <c>list-system-tools</c> asks
/// for no membership, so it is the one most likely to have been left unauthenticated by accident.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Mcp")]
public class McpToolsAnonymousTests(AspireAppFixture fixture) : McpToolsPermissionTestBase(fixture)
{
    public static TheoryData<string> Routes()
    {
        var data = new TheoryData<string> { "list-system-tools" };

        foreach (var route in _readOps.Concat(_writeOps).Concat(_manageOps))
        {
            data.Add(route);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task EveryRoute_AnonymousGets401(string route)
    {
        // A portal-level registration, so the routes that name a server are refused for the session and not for a
        // missing resource.
        await RegisterAsync(OwnersServer, UrlConfig());
        await ActAsAsync(null);

        var reply = route == "list-system-tools" ? await GetToolsAsync(route) : await RunAsync(route, McpRole.User);

        reply.Code.Should().Be(401, reply.ToString());
        reply.Error.Should().Be("Unauthorized");
    }
}
