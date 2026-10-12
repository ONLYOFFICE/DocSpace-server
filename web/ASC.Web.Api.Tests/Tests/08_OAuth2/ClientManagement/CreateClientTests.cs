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
/// POST /api/2.0/clients — every role can register its own OAuth2 client.
/// </summary>
[Trait("Category", "OAuth2")]
public class CreateClientTests(
    AspireAppFixture fixture)
    : ClientManagementTestBase(fixture)
{
    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task CreateClient_ByRole_ReturnsCreatedClient(EmployeeType? employeeType)
    {
        // Arrange
        var user = employeeType is null ? Owner : await InviteContact(employeeType.Value);
        await ApplySignatureAsync(user);

        var request = new CreateClientRequest(
            name: "Test OAuth Client",
            description: "Test OAuth client description",
            logo: OAuthLogo,
            websiteUrl: "https://example.com",
            termsUrl: "https://example.com/terms",
            policyUrl: "https://example.com/policy",
            redirectUris: ["https://example.com/callback"],
            allowedOrigins: ["https://example.com"],
            logoutRedirectUri: "https://example.com/logout",
            isPublic: true,
            allowPkce: true,
            scopes:
            [
                "accounts.self:read", "accounts.self:write", "accounts:read", "accounts:write",
                "rooms:read", "rooms:write", "files:read", "files:write", "openid"
            ]);

        // Act
        var result = await _clientManagementApi.CreateClientWithHttpInfoAsync(request, TestContext.Current.CancellationToken);

        // Assert
        result.StatusCode.Should().Be(HttpStatusCode.Created);

        var client = result.Data;
        client.ClientId.Should().NotBeNullOrEmpty();
        client.ClientSecret.Should().NotBeNullOrEmpty();
        client.Name.Should().Be("Test OAuth Client");
        client.Description.Should().Be("Test OAuth client description");
        client.WebsiteUrl.Should().Be("https://example.com");
        client.TermsUrl.Should().Be("https://example.com/terms");
        client.PolicyUrl.Should().Be("https://example.com/policy");
        client.Enabled.Should().BeTrue();
        client.IsPublic.Should().BeTrue();
        client.RedirectUris.Should().Contain("https://example.com/callback");
        client.AllowedOrigins.Should().Contain("https://example.com");
        client.LogoutRedirectUris.Should().Contain("https://example.com/logout");
        client.Scopes.Should().Contain([
            "openid", "files:read", "files:write", "rooms:read", "rooms:write",
            "accounts:read", "accounts:write", "accounts.self:read", "accounts.self:write"
        ]);
        client.AuthenticationMethods.Should().NotBeNull();
        client.CreatedOn.Should().NotBe(default);
        client.CreatedBy.Should().NotBeNullOrEmpty();
    }
}
