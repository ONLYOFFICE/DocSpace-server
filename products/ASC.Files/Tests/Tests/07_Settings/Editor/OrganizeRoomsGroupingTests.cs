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

namespace ASC.Files.Tests.Tests._07_Settings.Editor;

/// <summary>PUT /files/settings/organizegrouping - the caller's own preference for seeing rooms arranged by room
/// groups. The answer is the value re-read after the write; it is published as <c>organizeRoomsGrouping</c>, and
/// the rooms listing itself does not depend on it.</summary>
[Trait("Category", "Settings")]
public class OrganizeRoomsGroupingTests(
    AspireAppFixture fixture)
    : SettingsToggleTestBase(fixture)
{
    protected override string TogglePath => "api/2.0/files/settings/organizegrouping";

    protected override async Task<bool> ToggleAsync(bool? set)
    {
        var dto = set.HasValue ? new SettingsRequestDto { Set = set.Value } : null;

        return (await _filesSettingsApi.SetOrganizeRoomsGroupingAsync(dto, TestContext.Current.CancellationToken)).Response;
    }

    protected override bool ReflectedField(FilesSettingsDto settings) => settings.OrganizeRoomsGrouping;

    [Fact]
    public async Task OrganizeGrouping_OnOrOff_RoomsListingIsTheSame()
    {
        // Arrange - two rooms, one of them in a room group
        var grouped = await CreateCustomRoom("Autotest Grouped Room");
        var loose = await CreateCustomRoom("Autotest Loose Room");
        await _roomGroupsApi.AddRoomGroupAsync(
            new RoomGroupRequestDto("Autotest Group", "star", [new DuplicateRequestDtoAllOfFileIds(grouped.Id)]),
            TestContext.Current.CancellationToken);

        // Act
        await ToggleAsync(true);
        var withGrouping = await RoomIds();
        await ToggleAsync(false);
        var withoutGrouping = await RoomIds();

        // Assert
        withGrouping.Should().Contain([grouped.Id, loose.Id]);
        withoutGrouping.Should().BeEquivalentTo(withGrouping);
    }

    private async Task<List<int>> RoomIds()
    {
        var content = (await _roomsApi.GetRoomsFolderAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return [.. content.Folders.OfType<FolderDto>().Select(f => f.Id)];
    }
}
