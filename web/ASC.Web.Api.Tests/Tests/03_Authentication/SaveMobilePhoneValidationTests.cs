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
/// POST /api/2.0/authentication/setphone with a valid phone activation link and a number that has no digits. The check
/// threw a plain <c>Exception</c>, which nothing maps, so the call answered 500; the number is the caller's input,
/// refused with 400. The link key is computed from the stand's machine key the way the server issues it (address,
/// type and an empty <c>first</c>): a real link needs SMS two-factor, which a test portal cannot turn on.
/// </summary>
[Trait("Category", "Authentication")]
public class SaveMobilePhoneValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public async Task SaveMobilePhone_NumberWithoutDigits_Returns400(string mobilePhone)
    {
        // Arrange
        var member = await InviteContact(EmployeeType.User);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") })
            .Build();
        var provider = new ASC.Security.Cryptography.EmailValidationKeyProvider(
            new ASC.Security.Cryptography.MachinePseudoKeys(configuration),
            null!,
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ASC.Security.Cryptography.EmailValidationKeyProvider>.Instance);
        var key = provider.GetEmailKey(member.Email + "PhoneActivation", TenantId);

        await _webApiClient.Authenticate(null);
        _webApiClient.DefaultRequestHeaders.Add(
            "confirm", $"type=PhoneActivation&key={Uri.EscapeDataString(key)}&email={Uri.EscapeDataString(member.Email)}");

        try
        {
            // Act
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _authenticationApi.SaveMobilePhoneAsync(
                    new MobileRequestDto(mobilePhone), TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(400);
            exception.ErrorContent!.ToString().Should().Contain("Enter mobile phone number");
        }
        finally
        {
            _webApiClient.DefaultRequestHeaders.Remove("confirm");
        }
    }
}
