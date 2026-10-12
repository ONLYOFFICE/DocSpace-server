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
/// Shared setup for the group permission suites. The tests are split across several classes on
/// purpose: xUnit runs the tests of one class sequentially, so a single large class would
/// serialise the whole suite.
/// </summary>
public abstract class GroupPermissionTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>Creates a group and returns the created group's response body.</summary>
    protected async Task<GroupDto> CreateGroupAsync(string groupName, Guid? groupManager = null, IEnumerable<Guid>? members = null)
    {
        var request = new GroupRequestDto(
            members: members?.ToList()!,
            groupManager: groupManager ?? default,
            groupName: groupName);

        return (await _groupApi.AddGroupAsync(request, TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>Reads a group back with its members included.</summary>
    protected async Task<GroupDto> GetGroupWithMembersAsync(Guid id)
    {
        return (await _groupApi.GetGroupAsync(id, includeMembers: true, cancellationToken: TestContext.Current.CancellationToken)).Response;
    }

    /// <summary>A short random alphanumeric name, good enough for a group name in a positive case.</summary>
    protected static string RandomGroupName(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        return string.Create(length, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = chars[Random.Shared.Next(chars.Length)];
            }
        });
    }

    /// <summary>
    /// Sends a raw request to the group API, bypassing the typed SDK for bodies and query values it
    /// cannot express: a JSON field of the wrong type, an empty/invalid GUID field, an out-of-range
    /// enum, or a path segment that a strongly-typed <see cref="Guid"/> parameter cannot carry.
    /// </summary>
    protected async Task<HttpResponseMessage> SendRawGroupRequest(HttpMethod method, string path, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await _peopleClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
