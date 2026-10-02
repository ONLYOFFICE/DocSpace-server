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

namespace ASC.Files.Tests.Tests._10_Metadata;

/// <summary>
/// Covers the rooms listing filtered by the room metadata: one condition per field type and their combination.
/// </summary>
[Trait("Category", "Metadata")]
public class RoomsMetadataFieldFilterTests(AspireAppFixture fixture) : RoomsMetadataSearchTestsBase(fixture)
{
    #region String

    [Fact]
    public async Task Rooms_FilteredByStringField_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Eq(ClientField, "ACME"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
        rooms.Total.Should().Be(1, "the total must reflect the filtered listing");
    }

    [Fact]
    public async Task Rooms_FilteredByStringField_IsCaseInsensitive()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Eq(ClientField, "aCmE"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByStringField_DoesNotMatchSubstring()
    {
        var data = await ArrangeAsync();

        // the matching room holds exactly "ACME", the condition is an exact match and must not match a prefix
        var rooms = await data.SearchAsync(data.Eq(ClientField, "ACM"), expected: []);

        rooms.Folders.Should().BeEmpty();
    }

    #endregion

    #region Number

    [Fact]
    public async Task Rooms_FilteredByNumberRange_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.Range(AmountField, from: 100, to: 200), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByNumberRange_IncludesTheBoundaries()
    {
        var data = await ArrangeAsync();

        // the matching room holds exactly 150
        var rooms = await data.SearchAsync(data.Range(AmountField, from: 150, to: 150), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByNumberRange_SupportsTheOpenUpperBound()
    {
        var data = await ArrangeAsync();

        // 150 and 900: both rooms match
        var rooms = await data.SearchAsync(data.Range(AmountField, from: 100, to: null), expected: [data.MatchingRoomId, data.PartialRoomId]);

        rooms.RoomIds().Should().BeEquivalentTo(new[] { data.MatchingRoomId, data.PartialRoomId });
    }

    #endregion

    #region Date

    [Fact]
    public async Task Rooms_FilteredByDateRange_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(
            data.DateRange(SignedField, from: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), to: new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc)),
            expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredByDateRange_ExcludesTheRoomOutsideTheRange()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(
            data.DateRange(SignedField, from: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc), to: new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc)),
            expected: []);

        rooms.Folders.Should().BeEmpty();
    }

    #endregion

    #region Choice

    [Fact]
    public async Task Rooms_FilteredBySingleChoice_ReturnsOnlyTheMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.In(StatusField, "Signed"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredBySingleChoice_CombinesTheOptionsWithOr()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(data.In(StatusField, "Signed", "Draft"), expected: [data.MatchingRoomId, data.PartialRoomId]);

        rooms.RoomIds().Should().BeEquivalentTo(new[] { data.MatchingRoomId, data.PartialRoomId });
    }

    [Fact]
    public async Task Rooms_FilteredByMultiChoice_MatchesAnySelectedOptionWithoutDuplicates()
    {
        var data = await ArrangeAsync();

        // the matching room holds both Legal and Finance: it must be returned exactly once
        var rooms = await data.SearchAsync(data.In(TagsField, "Legal", "Finance"), expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    #endregion

    #region Several conditions

    [Fact]
    public async Task Rooms_FilteredBySeveralConditions_CombinesThemWithAnd()
    {
        var data = await ArrangeAsync();

        // the partial room shares the client but neither the amount nor the status
        var rooms = await data.SearchAsync(
            [data.Eq(ClientField, "ACME"), data.Range(AmountField, from: 100, to: 200), data.In(StatusField, "Signed")],
            expected: [data.MatchingRoomId]);

        rooms.RoomIds().Should().Equal(data.MatchingRoomId);
    }

    [Fact]
    public async Task Rooms_FilteredBySeveralConditions_ExcludesThePartiallyMatchingRoom()
    {
        var data = await ArrangeAsync();

        var rooms = await data.SearchAsync(
            [data.Eq(ClientField, "ACME"), data.In(StatusField, "Draft")],
            expected: []);

        rooms.Folders.Should().BeEmpty("the partially matching room holds the Draft status but not the ACME client");
    }

    #endregion
}
