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

namespace ASC.Files.Tests.Tests._03_Rooms.Templates;

/// <summary>
/// Functional behavior of PUT /files/roomtemplate/public — the public flag of a room template.
/// Permission coverage of the same endpoint lives in
/// <see cref="RoomTemplatePublicWritePermissionsTests"/>.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomTemplatePublicSettingsTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    /// <remarks>
    /// Bug 81938: setting public:true was not idempotent — the first call enabled the flag and a
    /// second identical call flipped it back to false, because the boolean in the body was ignored
    /// once the template was already public. Fixed — the value in the body is applied as sent.
    /// </remarks>
    [Fact]
    [Trait("Bug", "81938")]
    public async Task SetTemplatePublicSettings_TrueTwice_StaysPublic()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest SetPublic IdemTrue", isPublic: false);

        // Act
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken);
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, true), TestContext.Current.CancellationToken);

        // Assert
        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        actual.Should().BeTrue("re-applying public:true must leave the template public");
    }

    [Fact]
    public async Task SetTemplatePublicSettings_FalseTwice_StaysPrivate()
    {
        // The mirror of the case above: the same value applied twice is not a toggle in either
        // direction.
        await _filesClient.Authenticate(Owner);
        var templateId = await CreateTemplate("Autotest SetPublic IdemFalse", isPublic: true);

        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, false), TestContext.Current.CancellationToken);
        await _roomsApi.SetPublicSettingsAsync(new SetPublicDto(templateId, false), TestContext.Current.CancellationToken);

        var actual = (await _roomsApi.GetPublicSettingsAsync(templateId, TestContext.Current.CancellationToken)).Response;
        actual.Should().BeFalse("re-applying public:false must leave the template private");
    }
}
