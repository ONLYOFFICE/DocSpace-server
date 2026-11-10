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

namespace ASC.Files.Tests.Tests._02_Folders.History;

/// <summary>
/// The columns of a folder history report in CSV: a folder report leaves out Country, City, Page, Action and
/// Context, and shows IP, Browser and Platform only to a DocSpace admin.
/// </summary>
[Trait("Category", "Folders")]
public class FolderHistoryReportColumnsTests(
    AspireAppFixture fixture)
    : FolderHistoryReportTestBase(fixture)
{
    private static readonly string[] _excludedForEveryone = ["Country", "City", "Page"];
    private static readonly string[] _clientColumns = ["IP", "Browser", "Platform"];

    /// <summary>
    /// H8: the CSV writer maps every column of the event (<c>BaseEventMap</c>) and ignores the column set the task
    /// resolved for a folder report, so the CSV file carries the columns the folder report drops.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task CsvReport_Owner_HasNoCountryCityOrPageColumn()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Columns Owner");

        // Act
        var header = await ReportHeaderAsync(room.Id);

        // Assert
        header.Should().NotIntersectWith(_excludedForEveryone);
        header.Should().Contain(_clientColumns);
    }

    /// <summary>
    /// H8: the CSV file of a member who is not a DocSpace admin carries the IP, browser and platform of every
    /// event, which the folder report keeps for DocSpace admins.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task CsvReport_RoomMemberWithRead_HasNoClientColumns()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Report Columns Member");
        var user = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, user, FileShare.Read);
        await _filesClient.Authenticate(user);

        // Act
        var header = await ReportHeaderAsync(room.Id);

        // Assert
        header.Should().NotIntersectWith(_clientColumns);
        header.Should().NotIntersectWith(_excludedForEveryone);
    }

    private async Task<List<string>> ReportHeaderAsync(int roomId)
    {
        var task = await RunCsvReportAsync(roomId);
        task.Should().NotBeNull();
        task!.Status.Should().Be(DistributedTaskStatus.Completed, $"the report ended with: {task.Error}");

        var text = Encoding.UTF8.GetString(await DownloadFileAsync(ResultFileIdOf(task))).TrimStart('﻿');
        var firstLine = text.Split('\n')[0].TrimEnd('\r');

        return [.. firstLine.Split(',').Select(c => c.Trim('"'))];
    }
}
