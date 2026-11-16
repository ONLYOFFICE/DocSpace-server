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
/// POST /api/2.0/security/csp — which entries of <c>domains</c> are accepted (<c>SecurityController.ConfigureCsp</c>)
/// and the size limit of the built header (<c>CspSettingsHelper.SaveAsync</c>, <c>csp:default:MaxSize</c>, 15 KB
/// unless configured), each refusal leaving the stored domains as they were.
/// </summary>
[Trait("Category", "Security")]
public class CspDomainsRulesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string KeptDomain = "https://kept.example.com";

    private async Task<List<string>> StoredDomainsAsync()
    {
        return (await _cspApi.GetCspSettingsAsync(TestContext.Current.CancellationToken)).Response.Domains ?? [];
    }

    [Theory]
    [InlineData("*.example.com")]
    [InlineData("https://*.example.com")]
    [InlineData("example.com")]
    [InlineData("https://example.com")]
    public async Task ConfigureCsp_EntryWithOrWithoutSchemeOrWildcard_IsAccepted(string entry)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);

        // Act
        var configured = await _cspApi.ConfigureCspAsync(new CspRequestDto([entry]), TestContext.Current.CancellationToken);

        // Assert
        configured.StatusCode.Should().Be(200);
        configured.Response.Domains.Should().Contain(entry);
        (await StoredDomainsAsync()).Should().Contain(entry);
    }

    [Theory]
    [InlineData("https://пример.рф")]
    [InlineData("exämple.com")]
    [InlineData("http://exa mple.com")]
    [InlineData("https://")]
    public async Task ConfigureCsp_EntryNotAnAsciiAddress_ThrowsBadRequestAndDomainsStay(string entry)
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _cspApi.ConfigureCspAsync(new CspRequestDto([KeptDomain]), TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _cspApi.ConfigureCspAsync(new CspRequestDto([entry]), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        (await StoredDomainsAsync()).Should().BeEquivalentTo(KeptDomain);
    }

    [Fact]
    public async Task ConfigureCsp_HeaderLargerThanAllowed_ThrowsForbiddenAndDomainsStay()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _cspApi.ConfigureCspAsync(new CspRequestDto([KeptDomain]), TestContext.Current.CancellationToken);
        var many = Enumerable.Range(0, 400)
            .Select(i => $"https://host{i:D4}-{new string('a', 40)}.example.com")
            .ToList();

        // Act: 400 entries of ~70 bytes are already 28 KB before the header repeats them per directive
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _cspApi.ConfigureCspAsync(new CspRequestDto(many), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        (await StoredDomainsAsync()).Should().BeEquivalentTo(KeptDomain);
    }
}
