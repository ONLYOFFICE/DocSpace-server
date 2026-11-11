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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Tfa;

/// <summary>
/// The authenticator setup and code check reached through the activation link, the way the client reaches them:
/// a password sign-in on a portal that requires the authenticator answers with <c>confirmUrl</c> and sets the key
/// in the httpOnly cookie <c>asc_confirm_key_TfaActivation</c>; the client sends the link's query in the
/// <c>confirm</c> header and the key with it (<c>EmailValidationKeyModelHelper.GetModel</c> reads it from the query
/// or from that cookie). Here the key is taken from the <c>Set-Cookie</c> of the sign-in answer and put into the
/// query. Also: unlinking one's own application hands back a fresh activation link.
/// </summary>
[Trait("Category", "Settings")]
public class TfaActivationLinkTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    private const string ConfirmCookie = "asc_confirm_key_TfaActivation=";

    [Fact]
    public async Task TfaAppGenerateSetupCode_ThroughActivationLink_ReturnsSetupCode()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await EnableTfaAppAsync();
        await UseActivationLinkOfAsync(user);

        // Act
        var setup = await _tfaSettingsApi.TfaAppGenerateSetupCodeAsync(TestContext.Current.CancellationToken);

        // Assert
        setup.Response.ManualEntryKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TfaValidateAuthCode_ThroughActivationLink_WrongCode_ThrowsBadRequest()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await EnableTfaAppAsync();
        var secret = await UseActivationLinkOfAsync(user);
        var wrongCode = ASC.Web.Api.Tests.Tests._03_Authentication.TotpGenerator.GenerateCurrent(secret) == "000000" ? "111111" : "000000";

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.TfaValidateAuthCodeAsync(
                new TfaValidateRequestDto(wrongCode), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task TfaAppGenerateSetupCode_ThroughActivationLink_AfterTheAppIsLinked_ThrowsMethodNotAllowed()
    {
        // Arrange: a valid code through the same link links the application
        var user = await InviteContact(EmployeeType.User);
        await EnableTfaAppAsync();
        var secret = await UseActivationLinkOfAsync(user);
        var code = ASC.Web.Api.Tests.Tests._03_Authentication.TotpGenerator.GenerateCurrent(secret);
        var linked = await _tfaSettingsApi.TfaValidateAuthCodeAsync(
            new TfaValidateRequestDto(code), TestContext.Current.CancellationToken);
        linked.Response.Should().BeTrue();

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _tfaSettingsApi.TfaAppGenerateSetupCodeAsync(TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
    }

    [Fact]
    public async Task UnlinkTfaApp_OwnerSendsOwnId_ReturnsActivationLink()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();

        // Act
        var result = await _tfaSettingsApi.UnlinkTfaAppAsync(
            new TfaRequestDto(id: Owner.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().Contain("/confirm/TfaActivation?type=TfaActivation");
    }

    [Fact]
    public async Task UnlinkTfaApp_OwnerUnlinksAnotherPerson_ReturnsEmptyString()
    {
        // Arrange
        await LinkOwnerTfaAppAsync();
        var user = await InviteContact(EmployeeType.User);
        await LinkTfaAppAsync(user);
        await _webApiClient.Authenticate(Owner);

        // Act
        var result = await _tfaSettingsApi.UnlinkTfaAppAsync(
            new TfaRequestDto(id: user.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeEmpty();
    }

    /// <summary>
    /// Signs <paramref name="user"/> in with a password, puts the activation link with its key on the
    /// <c>confirm</c> header of the Web.Api client, signed out, and returns the setup secret of the answer.
    /// </summary>
    private async Task<string> UseActivationLinkOfAsync(User user)
    {
        await _webApiClient.Authenticate(null);
        var login = await _authenticationApi.AuthenticateMeWithHttpInfoAsync(
            new AuthRequestDto(userName: user.Email, password: user.Password), TestContext.Current.CancellationToken);

        var answer = login.Data.Response;
        answer.Tfa.Should().BeTrue();
        answer.ConfirmUrl.Should().Contain("type=TfaActivation");

        var cookie = login.Headers
            .Where(h => h.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .SelectMany(h => h.Value)
            .SelectMany(v => v.Split([';', ','], StringSplitOptions.TrimEntries))
            .FirstOrDefault(part => part.StartsWith(ConfirmCookie, StringComparison.Ordinal));
        cookie.Should().NotBeNull("the sign-in answer sets the activation key cookie");

        var key = Uri.UnescapeDataString(cookie![ConfirmCookie.Length..]);
        var query = new Uri(answer.ConfirmUrl).Query.TrimStart('?') + "&key=" + Uri.EscapeDataString(key);

        _webApiClient.DefaultRequestHeaders.Remove("confirm");
        _webApiClient.DefaultRequestHeaders.Add("confirm", query);

        return answer.TfaKey;
    }
}
