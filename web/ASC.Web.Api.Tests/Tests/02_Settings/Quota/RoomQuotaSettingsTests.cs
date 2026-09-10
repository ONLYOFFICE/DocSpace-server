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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Quota;

/// <summary>
/// POST /api/2.0/settings/roomquotasettings — the portal's default per-room storage quota.
/// Only Owner and DocSpaceAdmin may change it.
/// </summary>
[Trait("Category", "Settings")]
public class RoomQuotaSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    // 1 GB, in bytes.
    private const int DefaultQuota = 1073741824;

    [Fact]
    public async Task SaveRoomQuotaSettings_Owner_DisablesQuota()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(false, new QuotaSettingsRequestsDtoDefaultQuota(-1)),
            TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.EnableQuota.Should().BeFalse();
    }

    [Fact]
    public async Task SaveRoomQuotaSettings_Owner_EnablesQuotaWithSize()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var settings = await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(DefaultQuota)),
            TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.EnableQuota.Should().BeTrue();
        settings.Response.DefaultQuota.Should().Be(DefaultQuota);
    }

    [Fact]
    public async Task SaveRoomQuotaSettings_DocSpaceAdmin_DisablesQuota()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var settings = await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(false, new QuotaSettingsRequestsDtoDefaultQuota(-1)),
            TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.EnableQuota.Should().BeFalse();
    }

    [Fact]
    public async Task SaveRoomQuotaSettings_DocSpaceAdmin_EnablesQuotaWithSize()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var settings = await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
            new QuotaSettingsRequestsDto(true, new QuotaSettingsRequestsDtoDefaultQuota(DefaultQuota)),
            TestContext.Current.CancellationToken);

        // Assert
        settings.StatusCode.Should().Be(200);
        settings.Response.EnableQuota.Should().BeTrue();
        settings.Response.DefaultQuota.Should().Be(DefaultQuota);
    }

    [Fact]
    public async Task SaveRoomQuotaSettings_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
                new QuotaSettingsRequestsDto(false, new QuotaSettingsRequestsDtoDefaultQuota(-1)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task SaveRoomQuotaSettings_NonAdminMember_ThrowsAccessDenied(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _settingsQuotaApi.SaveRoomQuotaSettingsAsync(
                new QuotaSettingsRequestsDto(false, new QuotaSettingsRequestsDtoDefaultQuota(-1)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
