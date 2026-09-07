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
/// POST /api/2.0/authentication — signing in with a plaintext username/password. The SDK's
/// <c>AuthenticateMe</c> takes the plaintext <c>Password</c> field (not the client-hashed one
/// <see cref="Initializer.Authenticate"/> uses for the harness's own sign-ins), matching what the
/// TypeScript suite sends.
/// </summary>
[Trait("Category", "Authentication")]
public class AuthenticateTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AuthenticateMe_Owner_ReturnsToken()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var result = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: Owner.Email, password: Owner.Password),
            TestContext.Current.CancellationToken);

        // Assert
        AssertSuccessfulToken(result);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task AuthenticateMe_Member_ReturnsToken(EmployeeType employeeType)
    {
        // Arrange
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(null);

        // Act
        var result = await _authenticationApi.AuthenticateMeAsync(
            new AuthRequestsDto(userName: member.Email, password: member.Password),
            TestContext.Current.CancellationToken);

        // Assert
        AssertSuccessfulToken(result);
    }

    [Fact]
    public async Task AuthenticateMe_Owner_ReAuthenticating_GetsNewToken()
    {
        // Arrange
        await _webApiClient.Authenticate(null);
        var request = new AuthRequestsDto(userName: Owner.Email, password: Owner.Password);

        // Act
        var first = await _authenticationApi.AuthenticateMeAsync(request, TestContext.Current.CancellationToken);
        var second = await _authenticationApi.AuthenticateMeAsync(request, TestContext.Current.CancellationToken);

        // Assert
        second.Response.Token.Should().NotBeNullOrEmpty();
        second.Response.Token.Should().NotBe(first.Response.Token);
    }

    [Fact]
    public async Task AuthenticateMe_WrongPassword_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.AuthenticateMeAsync(
                new AuthRequestsDto(userName: Owner.Email, password: "wrongpassword"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().Contain("User authentication failed");
    }

    [Fact]
    public async Task AuthenticateMe_NonExistentEmail_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _authenticationApi.AuthenticateMeAsync(
                new AuthRequestsDto(userName: "nonexistent@example.com", password: "somepassword123"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().Contain("User authentication failed");
    }

    // The generated client omits Content-Type together with the body, which ASP.NET refuses with
    // 415 before the controller runs — not the case the TS suite exercises. Its client still sends
    // an (empty) JSON body, so this goes raw: an all-empty payload the typed DTO cannot produce.
    [Fact]
    public async Task AuthenticateMe_WithoutBody_ThrowsUnauthorized()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        using var response = await _webApi.PostRawAsync("api/2.0/authentication", "{}", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).Should().Contain("User authentication failed");
    }

    private static void AssertSuccessfulToken(AuthenticationTokenWrapper result)
    {
        result.Response.Token.Should().NotBeNullOrEmpty();
        result.Response.Expires.Should().NotBe(default);
        result.Response.Sms.Should().BeFalse();
        result.Response.Tfa.Should().BeFalse();
        result.Count.Should().Be(1);
        result.Links.Should().NotBeNullOrEmpty();
        result.Links![0].Action.Should().Be("POST");
    }
}
