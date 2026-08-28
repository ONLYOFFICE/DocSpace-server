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

namespace ASC.Files.Tests.Tests._07_Settings.Storage;

/// <summary>
/// <c>PUT /files/storeoriginal</c> - a per-user preference controlling whether documents uploaded
/// in a foreign format are also kept in their original form.
/// </summary>
[Trait("Category", "Settings")]
public class StoreOriginalTests(
    AspireAppFixture fixture)
    : StorageSettingsTestBase(fixture)
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StoreOriginal_SetsRequestedState(bool set)
    {
        // Act
        var response = await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = set }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(set);
    }

    [Fact]
    public async Task StoreOriginal_TogglesOnAndOff()
    {
        // Act & Assert
        (await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = true }, TestContext.Current.CancellationToken))
            .Response.Should().BeTrue();

        (await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = false }, TestContext.Current.CancellationToken))
            .Response.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StoreOriginal_SettingTwiceIsIdempotent(bool set)
    {
        // Arrange
        await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = set }, TestContext.Current.CancellationToken);

        // Act
        var response = await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = set }, TestContext.Current.CancellationToken);

        // Assert
        response.Response.Should().Be(set);
    }

    [Fact]
    public async Task StoreOriginal_NoBody_ReturnsBoolean()
    {
        // Act - sent raw: the generated client drops the Content-Type header together with the body,
        // so a bodyless typed call is refused by ASP.NET with 415 before the controller runs.
        using var response = await SendRawEmptyBodyPut("api/2.0/files/storeoriginal");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StoreOriginal_ReflectedInGetFilesSettings(bool set)
    {
        // Arrange
        await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = set }, TestContext.Current.CancellationToken);

        // Act
        var settings = await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        settings.Response.StoreOriginalFiles.Should().Be(set);
    }

    [Fact]
    public async Task StoreOriginal_IsIsolatedPerUser()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = false }, TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(user);
        await _filesSettingsApi.StoreOriginalAsync(new SettingsRequestDto { Set = true }, TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(Owner);
        var settings = await _filesSettingsApi.GetFilesSettingsAsync(TestContext.Current.CancellationToken);

        // Assert - the user's change must not leak into the owner's own setting.
        settings.Response.StoreOriginalFiles.Should().BeFalse();
    }
}
