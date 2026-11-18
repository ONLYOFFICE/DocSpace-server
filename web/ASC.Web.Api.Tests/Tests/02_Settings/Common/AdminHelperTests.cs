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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// PUT /api/2.0/settings/closeadminhelper on the stand's self-hosted installation outside custom mode
/// (<c>SettingsController.CloseAdminHelper</c>). No operation reads the flag back, so the stored per-user settings
/// row is read from the database (<c>AdminHelperSettings</c>, id <c>342CBBF7-...</c>).
/// </summary>
[Trait("Category", "Settings")]
public class AdminHelperTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static readonly Guid _adminHelperSettingsId = new("342CBBF7-FE08-4261-AB38-9C6BA8FA22B9");

    private async Task<Dictionary<Guid, string>> StoredRowsAsync()
    {
        await using var connection = await Fixture.OpenDatabaseAsync(TestContext.Current.CancellationToken);

        return await SettingsAudit.ReadSettingsRowsAsync(connection, TenantId, _adminHelperSettingsId);
    }

    [Fact]
    public async Task CloseAdminHelper_DocSpaceAdmin_MarksViewedForThatAdminOnly()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);

        // Act
        var closed = await _commonSettingsApi.CloseAdminHelperWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        closed.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = await StoredRowsAsync();
        rows.Should().ContainKey(admin.Id);
        rows[admin.Id].Replace(" ", "").Should().ContainEquivalentOf("\"Viewed\":true");
        rows.Should().NotContainKey(Owner.Id);
        rows.Should().NotContainKey(Guid.Empty);
    }

    [Fact]
    public async Task CloseAdminHelper_Owner_ReturnsOk()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var closed = await _commonSettingsApi.CloseAdminHelperWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        closed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await StoredRowsAsync()).Should().ContainKey(Owner.Id);
    }

    // PS-6: a non-administrator gets 415 "Not available." rather than 403, which is what the spec states.
    [Theory]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task CloseAdminHelper_NotAnAdministrator_ThrowsUnsupportedMediaTypeAndStoresNothing(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.CloseAdminHelperAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(415);
        exception.ErrorContent!.ToString().Should().Contain("Not available.");
        (await StoredRowsAsync()).Should().NotContainKey(member.Id);
    }

    [Fact]
    public async Task CloseAdminHelper_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.CloseAdminHelperAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
