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

namespace ASC.Web.Api.Tests.Tests._05_Security.OAuth2;

/// <summary>
/// GET /api/2.0/security/oauth2/token — a JWT used for the handshake between the login (client) and
/// the identity service. The controller performs no role check beyond requiring an authenticated
/// caller, so every portal role can generate one; only an anonymous caller is rejected.
/// </summary>
[Trait("Category", "Security")]
public class GenerateJwtTokenTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task GenerateJwtToken_Owner_ReturnsJwt()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _oauth2Api.GenerateJwtTokenAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertJwtToken(result.Response);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GenerateJwtToken_ByRole_ReturnsJwt(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        // Act
        var result = await _oauth2Api.GenerateJwtTokenAsync(TestContext.Current.CancellationToken);

        // Assert
        AssertJwtToken(result.Response);
    }

    [Fact]
    public async Task GenerateJwtToken_Anonymous_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _oauth2Api.GenerateJwtTokenAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    private static void AssertJwtToken(string? token)
    {
        token.Should().NotBeNullOrEmpty();
        token!.Split('.').Should().HaveCount(3);
        token.Should().StartWith("eyJ");
    }
}
