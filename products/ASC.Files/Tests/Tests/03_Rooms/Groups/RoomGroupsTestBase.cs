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

namespace ASC.Files.Tests.Tests._03_Rooms.Groups;

/// <summary>
/// Shared helpers for the room-groups suites (<c>POST/GET/PUT/DELETE /files/group</c> and
/// <c>POST /files/group/{id}/icon</c>). A room group is a per-user collection of rooms — access is
/// gated by the rooms it references, not by the caller's role.
/// </summary>
public abstract class RoomGroupsTestBase(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>Icons the group endpoints are expected to accept. "none" is deliberately excluded — see BUG 80921.</summary>
    protected static readonly string[] _validGroupIcons = ["star", "heart", "flag", "folder"];

    /// <summary>Creates a plain Custom room and returns its id.</summary>
    protected async Task<int> CreateGroupRoomId(string title)
    {
        var room = await CreateCustomRoom(title);
        return room.Id;
    }

    /// <summary>Creates <paramref name="count"/> plain Custom rooms and returns their ids.</summary>
    protected async Task<List<int>> CreateGroupRoomIds(int count, string prefix = "Group Room")
    {
        var ids = new List<int>();

        for (var i = 1; i <= count; i++)
        {
            ids.Add(await CreateGroupRoomId($"{prefix} {i}"));
        }

        return ids;
    }

    /// <summary>Creates a room group with a default valid icon unless overridden and returns the created DTO.</summary>
    protected async Task<RoomGroupDto> CreateRoomGroup(string name, IEnumerable<int> rooms, string icon = "star")
    {
        var created = await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto(name, icon, [.. rooms.Select(r => new DuplicateRequestDtoAllOfFileIds(r))]),
            cancellationToken: TestContext.Current.CancellationToken);

        return created.Response;
    }

    /// <summary>
    /// Asserts the shape/contract of a <see cref="RoomGroupDto"/> returned by any of the six group
    /// endpoints. Centralised so every positive test enforces the same contract.
    /// </summary>
    protected static void AssertRoomGroupShape(RoomGroupDto dto)
    {
        dto.Should().NotBeNull();
        dto.Id.Should().BeGreaterThan(0);
        dto.Name.Should().NotBeNull();
        dto.TotalRooms.Should().BeGreaterThanOrEqualTo(0);
        dto.Rooms.Should().NotBeNull();
        dto.Icon.Should().NotBeNull();
        // totalRooms must match the number of rooms actually returned.
        dto.TotalRooms.Should().Be(dto.Rooms.Count);
        // no duplicate rooms by title (FileEntryBaseDto has no id).
        var titles = dto.Rooms.ConvertAll(r => r.Title);
        titles.Distinct().Count().Should().Be(titles.Count);
    }

    /// <summary>
    /// Low-level request against the room-group endpoints, needed for validation / HTTP-contract
    /// tests that the typed SDK cannot express: raw/malformed bodies, wrong element types,
    /// unsupported methods and content types. Uses the shared <see cref="BaseTest._filesClient"/>,
    /// so its current authentication (or lack of it) applies.
    /// </summary>
    protected async Task<HttpResponseMessage> RoomGroupRaw(
        HttpMethod method,
        string path = "",
        string? query = null,
        string? body = null,
        bool omitBody = false,
        string? contentType = "application/json")
    {
        var url = $"api/2.0/files/group{path}";

        if (!string.IsNullOrEmpty(query))
        {
            url += $"?{query}";
        }

        using var request = new HttpRequestMessage(method, url);

        if (!omitBody && body != null)
        {
            request.Content = new StringContent(body, Encoding.UTF8);
            request.Content.Headers.ContentType = contentType == null ? null : new MediaTypeHeaderValue(contentType);
        }

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <inheritdoc cref="RoomGroupRaw(HttpMethod, string, string?, string?, bool, string?)"/>
    /// <remarks>Convenience overload that JSON-serializes an arbitrary object body.</remarks>
    protected Task<HttpResponseMessage> RoomGroupRaw(
        HttpMethod method,
        object body,
        string path = "",
        string? query = null,
        string? contentType = "application/json")
    {
        return RoomGroupRaw(method, path, query, JsonSerializer.Serialize(body), contentType: contentType);
    }
}
