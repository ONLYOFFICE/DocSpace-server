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

namespace ASC.Web.Api.Tests.Tests._08_OAuth2.ClientQuerying;

/// <summary>
/// GET /api/2.0/clients/consents — the OAuth2 consents the caller has granted to third-party
/// apps. A real, non-empty consent can only be produced by the authorization-code browser flow
/// (see <c>authorization.spec.ts</c>), which is not automatable here, so only the cases that need
/// no prior consent are ported: an empty list, the pagination envelope, and the permission checks.
///
/// The two positive cases read raw JSON for the same reason as <see cref="GetClientsTests"/>: the
/// generated <c>PageableModificationResponse.LastModifiedOn</c> is a non-nullable <c>DateTime</c>
/// that is <c>null</c> on the wire whenever there is no last-seen cursor, which is always true
/// here since neither case has a prior consent — see <see cref="ClientQueryingTestBase"/>.
/// </summary>
[Trait("Category", "OAuth2")]
public class GetConsentsTests(
    AspireAppFixture fixture)
    : ClientQueryingTestBase(fixture)
{
    [Fact]
    public async Task GetConsents_UserWithNoConsents_ReturnsEmptyList()
    {
        // Arrange
        await ApplySignatureAsync(Owner);

        // Act
        var page = await GetPageAsync("api/2.0/clients/consents?limit=50");

        // Assert
        page.GetProperty("data").EnumerateArray().Should().BeEmpty();
    }

    [Fact]
    public async Task GetConsents_ReturnsPaginationFields()
    {
        // Arrange
        await ApplySignatureAsync(Owner);

        // Act
        var page = await GetPageAsync("api/2.0/clients/consents?limit=50");

        // Assert
        page.GetProperty("limit").GetInt32().Should().Be(50);
    }

    [Fact]
    public async Task GetConsents_WithoutSignature_ThrowsForbidden()
    {
        // Arrange
        ApplySignature(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetConsentsAsync(50, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    // TS ported this as test.fail("BUG 81729"): a guest is refused everywhere else in this suite
    // with 403, so a guest reading their own (empty) consent list should be refused the same way.
    [Trait("Bug", "81729")]
    [Fact]
    public async Task GetConsents_Guest_ThrowsForbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await ApplySignatureAsync(guest);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _clientQueryingApi.GetConsentsAsync(50, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }
}
