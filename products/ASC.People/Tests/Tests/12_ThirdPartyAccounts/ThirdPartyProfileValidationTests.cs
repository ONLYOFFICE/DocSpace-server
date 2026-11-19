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

namespace ASC.People.Tests.Tests._12_ThirdPartyAccounts;

/// <summary>
/// <c>PUT /people/thirdparty/linkaccount</c> and <c>POST /people/thirdparty/signup</c> with a <c>serializedProfile</c>
/// the sign-in cannot use: a value the login flow did not issue, a profile that carries the error the provider
/// sign-in ended with, and (for signup) an identity without an email. Each threw an exception that nothing maps,
/// so a 500 came back; they answer 400 now. No OAuth provider is configured here, so the profiles the login flow would
/// issue are built the way it builds them: encrypted with the installation key, the portal id appended.
/// </summary>
public class ThirdPartyProfileValidationTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task LinkThirdPartyAccount_ProfileNotIssuedByLogin_Returns400()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyAccountsApi.LinkThirdPartyAccountAsync(
                new LinkAccountRequestDto("not-a-profile!"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task LinkThirdPartyAccount_ProfileWithAuthorizationError_Returns400()
    {
        // Arrange
        await _peopleClient.Authenticate(Owner);
        var profile = await SerializeProfileAsync("AeR→Provider refused the sign-in");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyAccountsApi.LinkThirdPartyAccountAsync(
                new LinkAccountRequestDto(profile), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Provider refused the sign-in");
    }

    [Fact]
    public async Task SignupThirdPartyAccount_ProfileWithAuthorizationError_Returns400()
    {
        // Arrange
        await _peopleClient.Authenticate(null);
        var profile = await SerializeProfileAsync("AeR→Provider refused the sign-in");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyAccountsApi.SignupThirdPartyAccountAsync(
                new SignupAccountRequestDto(key: "any", serializedProfile: profile), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Provider refused the sign-in");
    }

    // The email is checked before the invitation key, so any key reaches it.
    [Fact]
    public async Task SignupThirdPartyAccount_ProfileWithoutEmail_Returns400()
    {
        // Arrange
        await _peopleClient.Authenticate(null);
        var profile = await SerializeProfileAsync("Id→12345");

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _thirdPartyAccountsApi.SignupThirdPartyAccountAsync(
                new SignupAccountRequestDto(key: "any", serializedProfile: profile), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Incorrect email");
    }

    /// <summary>
    /// The transport form of a login profile (<c>LoginProfileTransport.ToString</c>): the profile fields, the id of the
    /// portal appended, encrypted with the installation key and Base64url-encoded.
    /// </summary>
    private async Task<string> SerializeProfileAsync(string fields)
    {
        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        Microsoft.Extensions.Configuration.MemoryConfigurationBuilderExtensions.AddInMemoryCollection(
            builder, new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") });
        var crypto = new ASC.Security.Cryptography.InstanceCrypto(new ASC.Security.Cryptography.MachinePseudoKeys(builder.Build()));

        var encrypted = await crypto.EncryptAsync(Encoding.UTF8.GetBytes(fields + TenantId));

        return Convert.ToBase64String(encrypted).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
