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

namespace ASC.Files.Tests.Tests._03_Rooms.Covers;

/// <summary>
/// GET /files/rooms/covers — the built-in cover gallery. It is static content: the same for every
/// role, every language and regardless of what exists on the portal.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomCoverGalleryTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GetCovers_Owner_ReturnsNonEmptyGallery()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var response = await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().NotBeEmpty();
        response.Count.Should().BePositive();
        response.Response[0].Id.Should().NotBeNullOrEmpty();
        response.Response[0].Data.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCovers_EveryCover_HasIdAndData()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        covers.Should().OnlyContain(c => !string.IsNullOrEmpty(c.Id) && !string.IsNullOrEmpty(c.Data));
    }

    [Fact]
    public async Task GetCovers_Ids_AreUnique()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        covers.Select(c => c.Id).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task GetCovers_ContainsDefaultScheduleCover()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        covers.Should().Contain(c => c.Id == "schedule");
    }

    [Fact]
    public async Task GetCovers_EveryRole_SeesTheSameIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var expected = await GetCoverIds();

        // Act & Assert
        foreach (var employeeType in new[] { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User })
        {
            await _filesClient.Authenticate(Owner);
            var member = await InviteContact(employeeType);

            await _filesClient.Authenticate(member);
            var actual = await GetCoverIds();

            actual.Should().Equal(expected, $"{employeeType} must see the same gallery as the owner");
        }
    }

    [Fact]
    public async Task GetCovers_AcceptLanguage_DoesNotChangeIds()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var ru = await GetCoverIds("ru-RU");
        var en = await GetCoverIds("en-US");

        // Assert
        en.Should().Equal(ru);
    }

    [Fact]
    public async Task GetCovers_DoesNotDependOnExistingRooms()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var before = await GetCoverIds();

        // Act
        await CreateCustomRoom("Autotest Covers Portal-State Room");

        // Assert
        var after = await GetCoverIds();
        after.Should().Equal(before);
    }

    /// <summary>Returns the gallery cover ids, sorted, optionally under a given <c>Accept-Language</c>.</summary>
    private async Task<List<string>> GetCoverIds(string? acceptLanguage = null)
    {
        if (acceptLanguage != null)
        {
            _filesClient.DefaultRequestHeaders.Remove("Accept-Language");
            _filesClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", acceptLanguage);
        }

        var covers = (await _roomsApi.GetRoomCoversAsync(TestContext.Current.CancellationToken)).Response;

        if (acceptLanguage != null)
        {
            _filesClient.DefaultRequestHeaders.Remove("Accept-Language");
        }

        return [.. covers.Select(c => c.Id).Order()];
    }
}
