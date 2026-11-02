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
/// Shared arrangement of the role matrix of the thirteen <c>/api/2.0/ai/tools/*</c> routes.
/// </summary>
/// <remarks>
/// Measured on the portal registry (no entity), for every role:
/// <code>
///                            owner  DSAdmin  RoomAdmin  User  Guest  anon
///   list-system-tools          200    200      200      200    200   401
///   list-custom-servers        200    200      200      200    403   401
///   get-custom-server          200    200      200      200    403   401
///   get-disabled               200    200      200      200    403   401
///   is-tool-disabled           200    200      200      200    403   401
///   get-allow-always           200    200      200      200    403   401
///   is-allow-always            200    200      200      200    403   401
///   set-disabled               200    200      200      200    403   401
///   set-allow-always           200    200      200      200    403   401
///   add-custom-server          200    200      403      403    403   401
///   update-custom-server       200    200      403      403    403   401
///   remove-custom-server       200    200      403      403    403   401
///   replace-all-custom-servers 200    200      403      403    403   401
/// </code>
/// Registering MCP servers is administrator-only, while reading them and toggling individual tools is open to any
/// non-guest member. The TypeScript matrix was measured on an agent the member was invited to; membership is gone with
/// the agents and the registry is portal-wide, so the matrix is on the portal.
/// The calls are raw on purpose: the matrix is about the status each role gets, the same way for all thirteen routes,
/// and the SDK's bool-returning operations throw a NullReferenceException instead of an ApiException on a refusal.
/// </remarks>
public abstract class McpToolsPermissionTestBase(AspireAppFixture fixture) : McpTestBase(fixture)
{
    protected const string OwnersServer = "owners-server";

    protected static readonly string[] _manageOps = ["add-custom-server", "update-custom-server", "remove-custom-server", "replace-all-custom-servers"];

    protected static readonly string[] _readOps = ["list-custom-servers", "get-custom-server", "get-disabled", "is-tool-disabled", "get-allow-always", "is-allow-always"];

    protected static readonly string[] _writeOps = ["set-disabled", "set-allow-always"];

    /// <summary>Every (role, operation) pair of one operation family.</summary>
    public static TheoryData<McpRole, string> Matrix(IEnumerable<string> ops)
    {
        var data = new TheoryData<McpRole, string>();

        foreach (var role in Enum.GetValues<McpRole>())
        {
            foreach (var op in ops)
            {
                data.Add(role, op);
            }
        }

        return data;
    }

    public static TheoryData<McpRole, string> ManageMatrix() => Matrix(_manageOps);

    public static TheoryData<McpRole, string> ReadMatrix() => Matrix(_readOps);

    public static TheoryData<McpRole, string> WriteMatrix() => Matrix(_writeOps);

    /// <summary>The Owner registers the server the matrix reads, then a member of <paramref name="role"/> is signed in.</summary>
    protected async Task<User> ArrangeMemberAsync(McpRole role)
    {
        await RegisterAsync(OwnersServer, UrlConfig());
        var member = await InviteRoleAsync(role);
        await ActAsAsync(member);

        return member;
    }

    protected Task<ToolsReply> RunAsync(string op, McpRole role) => op switch
    {
        "add-custom-server" => PostToolsAsync(op, new { name = $"{role}-server", config = UrlConfig("added.example.invalid") }),
        "update-custom-server" => PutToolsAsync(op, new { name = OwnersServer, config = UrlConfig("updated.example.invalid") }),
        "remove-custom-server" => DeleteToolsAsync(op, new { name = OwnersServer }),
        "replace-all-custom-servers" => PutToolsAsync(op, new { map = new Dictionary<string, object> { [$"{role}-replaced"] = UrlConfig("updated.example.invalid") } }),
        "list-custom-servers" => GetToolsAsync(op),
        "get-custom-server" => GetToolsAsync(op, $"?name={OwnersServer}"),
        "get-disabled" => GetToolsAsync(op, $"?scope={UserScope}"),
        "is-tool-disabled" => GetToolsAsync(op, "?serverType=docspace&toolName=delete_file"),
        "get-allow-always" => GetToolsAsync(op),
        "is-allow-always" => GetToolsAsync(op, "?serverType=docspace&toolName=delete_file"),
        "set-disabled" => PutToolsAsync(op, new { serverType = WebSearch, toolNames = new[] { "calculate" } }),
        "set-allow-always" => PutToolsAsync(op, new { serverType = "docspace", toolName = "delete_file", value = true }),
        _ => throw new ArgumentOutOfRangeException(nameof(op), op, "unknown operation")
    };
}
