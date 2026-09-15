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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// POST /files/thirdparty — open bugs in the connect/update flow. Grouped separately from
/// <see cref="ThirdPartyCredentialIsolationTests"/> because both of these stay red until fixed.
/// </summary>
[Trait("Category", "Rooms")]
public class ThirdPartyProviderConnectBugTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    /// <remarks>
    /// Bug 83265: <c>saveThirdParty</c> with <c>providerKey=Box</c> and an invalid OAuth token
    /// throws <c>System.NullReferenceException</c> in
    /// <c>FileStorageService.SaveThirdPartyAsync</c> (500) instead of a controlled 4xx.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83265")]
    public async Task SaveThirdParty_BoxWithFakeToken_ShouldReturnControlledError()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyApi.SaveThirdPartyAsync(
                new ThirdPartyRequestDto(
                    customerTitle: "Autotest Box Fake Token",
                    providerKey: "Box",
                    token: "fake-oauth-token"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().BeLessThan(500,
            "an invalid OAuth token must be rejected with a controlled 4xx, not crash the server");
    }

    /// <remarks>
    /// Bug 83303: calling <c>saveThirdParty</c> again with an existing <c>providerId</c> and a new
    /// <c>customerTitle</c> returns 200 (implying success) but <c>getThirdPartyAccounts</c> still
    /// shows the original title — the update is silently a no-op.
    /// </remarks>
    [Fact]
    [Trait("Bug", "83303")]
    public async Task SaveThirdParty_ReSaveWithExistingProviderId_ShouldRenameAccount()
    {
        // Arrange
        RequireNextcloud();
        await _filesClient.Authenticate(Owner);

        var connected = await ConnectNextcloud("Autotest Rename Before");

        // Act
        await _thirdPartyApi.SaveThirdPartyAsync(
            NextcloudRequestWithProviderId("Autotest Rename After", connected.ProviderId!.Value),
            TestContext.Current.CancellationToken);

        // Assert
        var accounts = await _thirdPartyApi.GetThirdPartyAccountsAsync(TestContext.Current.CancellationToken);
        var account = accounts.Response.Single(a => a.ProviderId == connected.ProviderId);
        account.CustomerTitle.Should().Be("Autotest Rename After",
            "re-saving an existing connection with a new title must actually rename it");
    }

    /// <summary>
    /// Builds a re-save request for the configured Nextcloud account, carrying the existing
    /// <paramref name="providerId"/> so the server treats it as an update rather than a new
    /// connection.
    /// </summary>
    private static ThirdPartyRequestDto NextcloudRequestWithProviderId(string customerTitle, int providerId)
    {
        var request = NextcloudRequest(customerTitle);
        request.ProviderId = providerId;
        return request;
    }
}
