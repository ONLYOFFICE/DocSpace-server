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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ScopeManagement;

/// <summary>
/// GET /api/2.0/scopes (identity service) — the OAuth2 scope catalog. Any member with an
/// <c>x-signature</c> JWT sees it; a request without the signature is refused with 403.
///
/// The positive cases read raw JSON: the generated <c>ScopeManagementApi.GetScopesAsync</c> is
/// typed to return a single <c>ScopeResponse</c>, but the endpoint returns an array of them —
/// an SDK/OpenAPI generation defect worth reporting.
/// </summary>
[Trait("Category", "OAuth2")]
public class ScopeManagementTests(
    AspireAppFixture fixture)
    : OAuth2TestBase(fixture)
{
    public static readonly (string Name, string Group, string Type)[] ExpectedScopes =
    [
        ("openid", "openid", "openid"),
        ("files:read", "files", "read"),
        ("files:write", "files", "write"),
        ("rooms:read", "rooms", "read"),
        ("rooms:write", "rooms", "write"),
        ("accounts:read", "accounts", "read"),
        ("accounts:write", "accounts", "write")
    ];

    [Theory]
    [InlineData(null)]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task GetScopes_Member_ReturnsScopeCatalog(EmployeeType? employeeType)
    {
        // Arrange
        var user = employeeType is null ? Owner : await InviteContact(employeeType.Value);
        await ApplySignatureAsync(user);

        // Act
        using var response = await _identityClient.GetAsync("api/2.0/scopes", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(body);
        json.RootElement.ValueKind.Should().Be(JsonValueKind.Array);

        var scopes = json.RootElement.EnumerateArray()
            .Select(s => (
                Name: s.GetProperty("name").GetString(),
                Group: s.GetProperty("group").GetString(),
                Type: s.GetProperty("type").GetString()))
            .ToList();

        foreach (var expected in ExpectedScopes)
        {
            scopes.Should().Contain(s => s.Name == expected.Name && s.Group == expected.Group && s.Type == expected.Type);
        }
    }

    [Fact]
    public async Task GetScopes_WithoutSignature_ThrowsForbidden()
    {
        // Arrange
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _scopeManagementApi.GetScopesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // The TS "Guest cannot get OAuth2 scopes" case never attaches a signature either — a guest is
    // rejected the same way an anonymous caller is, before any role check.
    [Fact]
    public async Task GetScopes_GuestWithoutSignature_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _webApiClient.Authenticate(guest);
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _scopeManagementApi.GetScopesAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
