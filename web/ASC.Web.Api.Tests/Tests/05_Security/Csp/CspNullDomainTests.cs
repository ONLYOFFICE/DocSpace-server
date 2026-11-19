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
namespace ASC.Web.Api.Tests.Tests._05_Security.Csp;

/// <summary>
/// POST /api/2.0/security/csp with a null entry in <c>domains</c>. The entry reached <c>string.Replace</c>, and the
/// NullReferenceException came back as a 500; it is refused with 400 now, like an entry that is not an address, and
/// nothing is saved.
/// </summary>
[Trait("Category", "Security")]
public class CspNullDomainTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task ConfigureCsp_NullDomain_Returns400AndKeepsDomains()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var stored = $"https://{Guid.NewGuid():N}.example.com";
        await _cspApi.ConfigureCspAsync(new CspRequestDto([stored]), TestContext.Current.CancellationToken);

        // Act - the SDK list cannot carry a null element, so the body is raw
        using var response = await _webApi.PostRawAsync(
            "api/2.0/security/csp", """{"domains":["https://autotest.example.com",null]}""", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        body.Should().Contain("An entry of domains is null");

        var current = (await _cspApi.GetCspSettingsWithHttpInfoAsync(TestContext.Current.CancellationToken)).Data.Response;
        current.Domains.Should().Equal(stored);
    }
}
