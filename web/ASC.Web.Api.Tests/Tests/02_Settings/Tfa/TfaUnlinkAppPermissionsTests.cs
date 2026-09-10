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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Tfa;

/// <summary>
/// Access control for PUT /api/2.0/settings/tfaappnewapp — unlinking another user's TFA App is
/// Owner-only. Confirmed live: DocSpaceAdmin gets the exact same 403 as RoomAdmin/User/Guest here,
/// despite otherwise mirroring Owner for the self-service and portal-settings TFA endpoints (see
/// <see cref="TfaSettingsPermissionsTests"/>).
/// </summary>
[Trait("Category", "Settings")]
public class TfaUnlinkAppPermissionsTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    [Fact]
    public async Task UnlinkTfaApp_Anonymous_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UnlinkTfaAppAsync(
                new TfaRequestsDto(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task UnlinkTfaApp_NonOwner_CannotUnlinkAnotherUsersTfaApp(EmployeeType employeeType)
    {
        // Arrange
        // The target doesn't need to actually have TFA linked - a permission check should reject
        // the caller before any target-state business logic runs. Skipping it also keeps this test
        // under DocSpace's brute-force login-attempt threshold (each link completes 2 logins).
        var target = await InviteContact(EmployeeType.User);

        var caller = await InviteMember(employeeType);
        await EnableTfaAppAsync();
        await LinkTfaAppAsync(caller);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.UnlinkTfaAppAsync(
                new TfaRequestsDto(id: target.Id), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("No permissions to perform this action");
    }
}
