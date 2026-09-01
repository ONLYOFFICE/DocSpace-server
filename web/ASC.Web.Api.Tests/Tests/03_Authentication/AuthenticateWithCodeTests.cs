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
/// POST /api/2.0/authentication/{code} — signing in with a TFA-App code once the portal requires
/// it. The TypeScript suite re-signs the owner in and disables TFA again at the end of every test
/// to restore its (shared) portal for later suites; this suite gets a brand-new portal per test
/// (<c>BaseTest.InitializeAsync</c>), so that cleanup step is dropped — nothing else ever reads
/// this portal's TFA setting again.
/// </summary>
[Trait("Category", "Authentication")]
public class AuthenticateWithCodeTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AuthenticateMeFromBodyWithCode_Owner_ReturnsToken()
    {
        // Arrange
        await EnableTfaAppAsync();
        var tfaKey = await GetTfaKeyAsync(Owner);
        var code = TotpGenerator.GenerateCurrent(tfaKey);

        // Act
        var result = await AuthenticateWithCodeAsync(Owner, code);

        // Assert
        result.Response.Token.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task AuthenticateMeFromBodyWithCode_Member_ReturnsToken(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await EnableTfaAppAsync();
        var tfaKey = await GetTfaKeyAsync(member);
        var code = TotpGenerator.GenerateCurrent(tfaKey);

        // Act
        var result = await AuthenticateWithCodeAsync(member, code);

        // Assert
        result.Response.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateMeFromBodyWithCode_Owner_ReAuthenticating_GetsNewToken()
    {
        // Arrange
        await EnableTfaAppAsync();
        var tfaKey = await GetTfaKeyAsync(Owner);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;

        // Act
        var first = await AuthenticateWithCodeAsync(Owner, TotpGenerator.GenerateAtCounter(tfaKey, counter));
        var second = await AuthenticateWithCodeAsync(Owner, TotpGenerator.GenerateAtCounter(tfaKey, counter + 1));

        // Assert
        second.Response.Token.Should().NotBeNullOrEmpty();
        second.Response.Token.Should().NotBe(first.Response.Token);
    }

    [Fact]
    public async Task AuthenticateMeFromBodyWithCode_TfaNotEnabled_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await AuthenticateWithCodeAsync(Owner, "123456"));

        // Assert
        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AuthenticateMeFromBodyWithCode_WrongCredentials_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.AuthenticateMeFromBodyWithCodeAsync(
                "123456",
                new AuthWithCodeRequestsDto { UserName = "wrong@email.com", Password = "wrongpassword", Code = "123456" },
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().Contain("User authentication failed");
    }

    // The generated client omits Content-Type together with the body, which ASP.NET refuses with
    // 415 before the controller runs — not the case the TS suite exercises. Its client still sends
    // an (empty) JSON body, so this goes raw: an all-empty payload the typed DTO cannot produce.
    [Fact]
    public async Task AuthenticateMeFromBodyWithCode_EmptyBody_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        using var response = await _webApi.PostRawAsync("api/2.0/authentication/123456", "{}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("User authentication failed");
    }

    /// <summary>
    /// Enables the TFA App requirement for the whole portal. Done as the owner, who is always a
    /// <see cref="EmployeeType.DocSpaceAdmin"/> in a fresh portal.
    /// </summary>
    private async Task EnableTfaAppAsync()
    {
        await _webApiClient.Authenticate(Owner);
        await _tfaSettingsApi.UpdateTfaSettingsAsync(
            new TfaRequestsDto(TfaRequestsDtoType.App), TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Authenticates <paramref name="user"/> with their plaintext password to obtain the TFA setup
    /// key the portal hands back instead of a token once TFA App is required.
    /// </summary>
    private async Task<string> GetTfaKeyAsync(User user)
    {
        await _webApiClient.Authenticate(null);
        var result = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: user.Email, password: user.Password), TestContext.Current.CancellationToken);

        return result.Response.TfaKey;
    }

    private async Task<AuthenticationTokenWrapper> AuthenticateWithCodeAsync(User user, string code)
    {
        await _webApiClient.Authenticate(null);
        return await _authenticationApi.AuthenticateMeFromBodyWithCodeAsync(
            code,
            new AuthWithCodeRequestsDto { UserName = user.Email, Password = user.Password, Code = code },
            TestContext.Current.CancellationToken);
    }
}
