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

namespace ASC.Files.Tests.Tests._07_Settings.SharingDefaults;

/// <summary>
/// <c>PUT /files/changedeleteconfrim</c> - unlike <see cref="ExternalShareTests"/> this setting is
/// stored per user, so every authenticated role can change their own copy of it.
/// </summary>
[Trait("Category", "Settings")]
[Trait("Feature", "SharingDefaults")]
public class ChangeDeleteConfirmTests(AspireAppFixture fixture) : SharingDefaultsTestBase(fixture)
{
    public static TheoryData<EmployeeType?> AllowedRoles =>
    [
        null, // the portal owner
        EmployeeType.DocSpaceAdmin,
        EmployeeType.RoomAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeDeleteConfirm_SetsValueAndReflectsInSettings(bool set)
    {
        // Act
        var result = await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(set), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().Be(set);

        var settings = await GetFilesSettings();
        settings.ConfirmDelete.Should().Be(set);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ChangeDeleteConfirm_RepeatedCall_IsIdempotent(bool set)
    {
        // Arrange
        await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(set), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(set), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().Be(set);
    }

    [Fact]
    public async Task ChangeDeleteConfirm_NoBody_ReturnsBoolean()
    {
        // Act - sent raw: see SharingDefaultsTestBase.SendRawEmptyBodyPut.
        using var response = await SendRawEmptyBodyPut("api/2.0/files/changedeleteconfrim");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("response").ValueKind.Should().BeOneOf(JsonValueKind.True, JsonValueKind.False);
    }

    [Fact]
    public async Task ChangeDeleteConfirm_IsIsolatedPerUser()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(false), TestContext.Current.CancellationToken);

        // Act
        await _filesClient.Authenticate(user);
        await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(true), TestContext.Current.CancellationToken);

        await _filesClient.Authenticate(Owner);
        var settings = await GetFilesSettings();

        // Assert - the owner's own setting was untouched by the member's change
        settings.ConfirmDelete.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeDeleteConfirm_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [MemberData(nameof(AllowedRoles))]
    public async Task ChangeDeleteConfirm_EveryRole_CanChangeOwnSetting(EmployeeType? employeeType)
    {
        // Arrange
        if (employeeType != null)
        {
            var member = await InviteMember(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var result = await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(true), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeDeleteConfirm_TerminatedUser_Unauthorized()
    {
        // Arrange
        var user = await InviteMember(EmployeeType.User);
        await _filesClient.Authenticate(user);

        await TerminateUser(user);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.ChangeDeleteConfirmAsync(new SettingsRequestDto(true), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
