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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Authorization;

/// <summary>
/// POST /api/2.0/settings/authservice/externaldb/test — probing an external database connection
/// without saving it. Only the negative path (unreachable host) is covered here: the positive
/// path needs a real MySQL/SQLite instance reachable from the portal host, which this harness
/// does not provision (same gap the ported TS suite leaves as a TODO).
/// </summary>
[Trait("Category", "Settings")]
public class TestExternalDatabaseConnectionTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task TestExternalDatabaseConnection_Owner_InvalidCredentials_ReturnsFailure()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var invalidMysqlSettings = AuthorizationTestData.CreateInvalidMysqlSettings();

        // Act
        var result = await _settingsAuthorizationApi.TestExternalDatabaseConnectionAsync(
            invalidMysqlSettings, TestContext.Current.CancellationToken);

        // Assert
        AssertConnectionFailed(result);
    }

    [Fact]
    public async Task TestExternalDatabaseConnection_DocSpaceAdmin_InvalidCredentials_ReturnsFailure()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _webApiClient.Authenticate(admin);
        var invalidMysqlSettings = AuthorizationTestData.CreateInvalidMysqlSettings();

        // Act
        var result = await _settingsAuthorizationApi.TestExternalDatabaseConnectionAsync(
            invalidMysqlSettings, TestContext.Current.CancellationToken);

        // Assert
        AssertConnectionFailed(result);
    }

    private static void AssertConnectionFailed(ConnectionTestResultWrapper result)
    {
        result.StatusCode.Should().Be(200);
        result.Response.Should().NotBeNull();
        result.Response.Success.Should().BeFalse();
        result.Response.Error.Should().NotBeNullOrEmpty();
    }
}
