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


namespace ASC.Web.Api.Tests.Tests._03_Authentication;

/// <summary>
/// POST /api/2.0/authentication/sendsms — resends the SMS code. Open to anonymous callers; the
/// credentials are checked exactly as by POST /api/2.0/authentication. SMS two-factor cannot be
/// enabled on a test portal, so the success path is out of reach; everything before it is covered.
/// </summary>
[Trait("Category", "Authentication")]
public class SendSmsCodeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const string LoginFailed = "User authentication failed";

    [Fact]
    public async Task SendSmsCode_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.SendSmsCodeAsync(
                new AuthRequestDto(userName: user.Email, password: $"{user.Password}x"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().Contain(LoginFailed);
    }

    [Fact]
    public async Task SendSmsCode_UnknownUser_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.SendSmsCodeAsync(
                new AuthRequestDto(userName: Initializer.FakerMember.Generate().Email, password: "somepassword123"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().Contain(LoginFailed);
    }

    [Fact]
    public async Task SendSmsCode_EmptyCredentials_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.SendSmsCodeAsync(new AuthRequestDto(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().Contain(LoginFailed);
    }

    // The generated client omits Content-Type together with a null body, which ASP.NET refuses
    // with 415 before the controller runs (see AuthenticateMe_WithoutBody_ThrowsUnauthorized), so
    // the "no body" case goes raw with an empty JSON object.
    [Fact]
    public async Task SendSmsCode_EmptyBody_ReturnsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        using var response = await _webApi.PostRawAsync("api/2.0/authentication/sendsms", "{}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain(LoginFailed);
    }

    /// <summary>
    /// Bug 84503: with valid credentials on a portal where SMS two-factor is not enabled the
    /// endpoint answered 500 — <c>SmsManager.PutAuthCodeAsync</c> deliberately throws
    /// MethodAccessException when SMS settings are unavailable or TFA is not enabled for the user
    /// and nothing mapped it to a client response. The exact status is not specified (PUT
    /// /settings/tfaapp reports the same condition as 405), so the test only asserts it is not a 500.
    /// </summary>
    [Trait("Bug", "84503")]
    [Fact]
    public async Task SendSmsCode_ValidCredentialsWhileSmsDisabled_IsNotInternalError()
    {
        // Arrange
        var user = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(null);

        // Act
        int status;

        try
        {
            var response = await _authenticationApi.SendSmsCodeWithHttpInfoAsync(
                new AuthRequestDto(userName: user.Email, password: user.Password), TestContext.Current.CancellationToken);
            status = (int)response.StatusCode;
        }
        catch (ApiException exception)
        {
            status = exception.ErrorCode;
        }

        // Assert
        status.Should().NotBe(500);
    }
}
