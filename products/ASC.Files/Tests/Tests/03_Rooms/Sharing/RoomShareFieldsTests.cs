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

namespace ASC.Files.Tests.Tests._03_Rooms.Sharing;

/// <summary>
/// <c>GET /files/rooms/{id}/share</c> with the <c>fields</c> query parameter, which projects every
/// returned entry down to the requested keys. The generated <c>GetRoomSecurityInfoAsync</c> has no such
/// parameter, so the requests go over raw HTTP and the bodies are compared with the unprojected response.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomShareFieldsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Owner plus one invited user, so the list has two distinguishable members.
    /// </summary>
    private async Task<(int RoomId, Guid UserId)> SetupRoom()
    {
        var user = await InviteMember(EmployeeType.User);
        var room = await CreateCustomRoom("Autotest Share Fields");
        await InviteToRoom(room.Id, user, FileShare.Editing);

        return (room.Id, user.Id);
    }

    private async Task<(HttpStatusCode Status, string Text, JsonElement? Body)> GetShareRaw(int roomId, string[] fields, string extra = "")
    {
        var parts = fields.Select(f => $"fields={f}").ToList();

        if (extra.Length > 0)
        {
            parts.Add(extra);
        }

        var query = string.Join('&', parts);
        using var response = await _filesClient.GetAsync(
            $"api/2.0/files/rooms/{roomId}/share{(query.Length == 0 ? string.Empty : "?" + query)}",
            TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        JsonElement? body = null;

        try
        {
            body = JsonDocument.Parse(text).RootElement.Clone();
        }
        catch (JsonException)
        {
            // a half-written response is reported through the assertion on the body below
        }

        return (response.StatusCode, text, body);
    }

    private async Task AssertFieldsMatchBaseline(string[] fields, string extra = "")
    {
        // Arrange
        var (roomId, userId) = await SetupRoom();
        var baseline = await GetShareRaw(roomId, [], extra);
        baseline.Status.Should().Be(HttpStatusCode.OK);
        var baselineEntries = baseline.Body!.Value.GetProperty("response").EnumerateArray().ToList();
        baselineEntries.Should().HaveCountGreaterThanOrEqualTo(2);

        // Act
        var (status, text, body) = await GetShareRaw(roomId, fields, extra);

        // Assert
        status.Should().Be(HttpStatusCode.OK, $"body: {text[^Math.Min(400, text.Length)..]}");
        body.Should().NotBeNull("the response body is complete, valid JSON - not a half-written one with an error tail");
        body!.Value.TryGetProperty("error", out _).Should().BeFalse();
        body.Value.GetProperty("statusCode").GetInt32().Should().Be(200);
        body.Value.GetProperty("total").GetInt64().Should().Be(baseline.Body!.Value.GetProperty("total").GetInt64());

        var entries = body.Value.GetProperty("response").EnumerateArray().ToList();
        entries.Should().HaveSameCount(baselineEntries);

        for (var i = 0; i < entries.Count; i++)
        {
            // Projection returns exactly the requested keys.
            entries[i].EnumerateObject().Select(p => p.Name).Order().Should().Equal(fields.Order());

            foreach (var f in fields)
            {
                entries[i].GetProperty(f).GetRawText().Should().Be(baselineEntries[i].GetProperty(f).GetRawText(), $"entry {i} field {f}");
            }
        }

        if (fields.Contains("sharedTo"))
        {
            var ids = entries.Select(e => e.GetProperty("sharedTo").GetProperty("id").GetString()).ToList();
            ids.Should().Contain(userId.ToString());
            ids.Should().Equal(baselineEntries.Select(e => e.GetProperty("sharedTo").GetProperty("id").GetString()));

            foreach (var e in entries)
            {
                var shared = e.GetProperty("sharedTo");
                shared.GetProperty("id").ValueKind.Should().Be(JsonValueKind.String);
                shared.GetProperty("displayName").ValueKind.Should().Be(JsonValueKind.String);
                shared.GetProperty("email").ValueKind.Should().Be(JsonValueKind.String);
            }
        }
    }

    [Fact]
    public async Task GetRoomSecurityInfo_WithoutFields_ReturnsTheFullEntryShape()
    {
        // Arrange
        var (roomId, userId) = await SetupRoom();

        // Act
        var (status, _, body) = await GetShareRaw(roomId, []);

        // Assert
        status.Should().Be(HttpStatusCode.OK);
        body!.Value.TryGetProperty("error", out _).Should().BeFalse();
        var entries = body.Value.GetProperty("response").EnumerateArray().ToList();
        entries.Should().HaveCount(2);

        foreach (var e in entries)
        {
            foreach (var key in new[] { "access", "sharedTo", "isLocked", "isOwner", "canEditAccess", "subjectType" })
            {
                e.TryGetProperty(key, out _).Should().BeTrue($"key {key}");
            }
        }

        entries.Select(e => e.GetProperty("sharedTo").GetProperty("id").GetString()).Should().Contain(userId.ToString());
    }

    /// <summary>
    /// Single fields that do not involve <c>sharedTo</c> - a positive control: they prove <c>fields</c>
    /// itself works, so the <c>sharedTo</c> failures below are specific to that projection.
    /// </summary>
    [Theory]
    [InlineData("access")]
    [InlineData("isLocked")]
    [InlineData("isOwner")]
    [InlineData("canEditAccess")]
    [InlineData("subjectType")]
    public async Task GetRoomSecurityInfo_SingleField_ReturnsOnlyThatFieldForEveryMember(string field)
    {
        await AssertFieldsMatchBaseline([field]);
    }

    /// <summary>
    /// Projecting <c>sharedTo</c> failed with System.Text.Json "possible object cycle": the body was
    /// half-written and ended with an <c>error</c> object under HTTP 500, while every other field projected
    /// fine. The TypeScript original marks these as a known failure without a bug number yet, so there is
    /// <c>Category=Bug</c> and no <c>Bug</c> number; the cases assert the intended 200 and stay red while the defect is open.
    /// </summary>
    [Theory]
    [Trait("Category", "Bug")]
    [InlineData("sharedTo", "")]
    [InlineData("access|sharedTo", "")]
    [InlineData("access|sharedTo|isOwner", "")]
    [InlineData("access|sharedTo|isOwner", "filterType=0")]
    [InlineData("access|sharedTo|isOwner", "filterType=16")]
    [InlineData("access|sharedTo|isOwner", "count=30")]
    public async Task GetRoomSecurityInfo_FieldsWithSharedTo_ReturnsTheProjectedEntries(string fields, string extra)
    {
        await AssertFieldsMatchBaseline(fields.Split('|'), extra);
    }

    [Fact]
    public async Task GetRoomSecurityInfo_FieldsWithoutSharedTo_MatchTheFullResponseWithFilterTypeAndCount()
    {
        await AssertFieldsMatchBaseline(["access", "isOwner"], "filterType=0&count=30");
    }
}
