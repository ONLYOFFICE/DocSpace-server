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


namespace ASC.Web.Api.Tests.Tests._09_Apps;

/// <summary>
/// PUT then GET /api/2.0/apps/{id}/settings — every kind of JSON value goes in through the write and
/// comes back unchanged through all three readers (the PUT answer, GET settings, GET application).
/// </summary>
[Trait("Category", "Apps")]
public class AppSettingsRoundTripTests(
    AspireAppFixture fixture)
    : AppsTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(AppsTestData.JsonValues), MemberType = typeof(AppsTestData))]
    public async Task SetSettings_JsonValue_ReadsBackUnchanged(string name, string document)
    {
        // Arrange
        var id = (await PickAppsAsync(1))[0];
        var expected = Canonical(document);

        // Act
        var (status, body) = await SendAsync(HttpMethod.Put, $"{AppPath(id)}/settings", $"{{\"settings\":{document}}}");

        // Assert
        status.Should().Be(HttpStatusCode.OK, $"{name}: {body}");
        ResponseOf(body)!.Value.GetProperty("id").GetString().Should().Be(id);
        Canonical(ResponseOf(body)!.Value.GetProperty("settings")).Should().Be(expected, name);

        (await ReadSettingsAsync(id)).Should().Be(expected, name);
        (await ReadSettingsAsync(id)).Should().Be(expected, "a repeated read is identical");
        (await SnapshotAsync(id)).Settings.Should().Be(expected, name);
    }
}
