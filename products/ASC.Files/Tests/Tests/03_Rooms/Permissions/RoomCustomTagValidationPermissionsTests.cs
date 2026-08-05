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

namespace ASC.Files.Tests.Tests._03_Rooms.Permissions;

[Trait("Category", "Rooms")]
public class RoomCustomTagValidationPermissionsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    // These cases send deliberately malformed bodies, which the typed SDK cannot express,
    // so they go through the raw HTTP client.

    [Fact]
    [Trait("Bug", "80046")]
    public async Task DeleteCustomTags_BodyUsesNameInsteadOfNames_BadRequest()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Test"), TestContext.Current.CancellationToken);

        // Act
        using var response = await SendRawTagsDelete("""{"name":["Test"]}""");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("The Names field is required.");
    }

    [Fact]
    public async Task DeleteCustomTags_EmptyNamesArray_KeepsTagsIntact()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("KeepMeTag"), TestContext.Current.CancellationToken);

        var before = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Act
        using var response = await SendRawTagsDelete("""{"names":[]}""");

        // Assert
        ((int)response.StatusCode).Should().BeOneOf(200, 400);

        var after = (await _roomsApi.GetRoomTagsInfoAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
        after.Should().BeEquivalentTo(before);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"names":null}""")]
    [InlineData("""{"names":"Tag1"}""")]
    [InlineData("""{"names":["Tag1",123]}""")]
    public async Task DeleteCustomTags_MalformedBody_BadRequest(string body)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawTagsDelete(body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    /// <remarks>
    /// BUG 81689: blank entries inside the names array are silently accepted (200) instead of
    /// producing a validation error (400). Marked <c>test.fail</c> in the TypeScript suite.
    /// </remarks>
    [Theory]
    [InlineData("""{"names":[""]}""")]
    [InlineData("""{"names":["   "]}""")]
    [InlineData("""{"names":[null]}""")]
    [Trait("Bug", "81689")]
    public async Task DeleteCustomTags_BlankNameEntry_BadRequest(string body)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        using var response = await SendRawTagsDelete(body);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    public async Task UpdateTag_AllowedRoles_Renamed(EmployeeType? employeeType)
    {
        // Arrange
        var oldName = $"Autotest {employeeType?.ToString() ?? "Owner"} Rename Tag";
        var newName = $"{oldName} Updated";

        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(oldName), TestContext.Current.CancellationToken);

        if (employeeType.HasValue)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var response = await _roomsApi.UpdateRoomTagAsync(
            new UpdateTagRequestDto(oldName, newName),
            TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(newName);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UpdateTag_NonAdminRoles_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        var oldName = $"Autotest {employeeType} Rename Tag";

        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto(oldName), TestContext.Current.CancellationToken);

        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto(oldName, $"{oldName} Updated"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task UpdateTag_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        await _roomsApi.CreateRoomTagAsync(new CreateTagRequestDto("Autotest Anon Rename Tag"), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _roomsApi.UpdateRoomTagAsync(
                new UpdateTagRequestDto("Autotest Anon Rename Tag", "Autotest Anon Rename Tag Updated"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
