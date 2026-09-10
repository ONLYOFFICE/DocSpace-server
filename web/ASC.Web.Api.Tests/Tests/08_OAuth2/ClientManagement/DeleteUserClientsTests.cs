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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientManagement;

/// <summary>
/// DELETE /api/2.0/clients — every role can wipe every OAuth2 client it registered itself,
/// whether or not there is anything to delete.
/// </summary>
[Trait("Category", "OAuth2")]
public class DeleteUserClientsTests(
    AspireAppFixture fixture)
    : ClientManagementTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task DeleteUserClients_ByRole_ReturnsOk(EmployeeType? employeeType)
    {
        // Arrange
        var user = employeeType is null ? Owner : await InviteContact(employeeType.Value);
        await ApplySignatureAsync(user);

        await _clientManagementApi.CreateClientAsync(ValidCreateClientRequest(), TestContext.Current.CancellationToken);

        // Act
        var result = await _clientManagementApi.DeleteUserClientsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteUserClients_ClientsAreGoneAfterDeletion()
    {
        // Arrange
        await ApplySignatureAsync();

        await _clientManagementApi.CreateClientAsync(ValidCreateClientRequest("Test Client 1"), TestContext.Current.CancellationToken);
        await _clientManagementApi.CreateClientAsync(ValidCreateClientRequest("Test Client 2"), TestContext.Current.CancellationToken);

        await _clientManagementApi.DeleteUserClientsAsync(TestContext.Current.CancellationToken);

        // Act — raw JSON, see the comment on DeleteTenantClientsTests.ClientsAreGoneAfterDeletion
        // for why PageableResponse.Data can't be used here.
        await ApplySignatureAsync();
        using var response = await _identityClient.GetAsync("api/2.0/clients?limit=50", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("data").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task DeleteUserClients_NoClientsExist_ReturnsOk()
    {
        // Arrange
        await ApplySignatureAsync();

        // Act
        var result = await _clientManagementApi.DeleteUserClientsWithHttpInfoAsync(TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
