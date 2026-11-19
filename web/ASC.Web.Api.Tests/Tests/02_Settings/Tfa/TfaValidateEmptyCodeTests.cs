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
/// POST /api/2.0/settings/tfaapp/validate with an empty or blank code, signed with a <c>TfaActivation</c> link while the
/// portal requires the authenticator app. The check threw a plain <c>Exception</c>, which nothing maps, so a 500 came
/// back; the code is the caller's input, refused with 400 like a wrong code.
/// </summary>
/// <remarks>
/// The link key is computed from the stand's machine key the way the server issues it (user id, type and an empty
/// <c>first</c>), since the link itself reaches a browser only as an httpOnly cookie.
/// </remarks>
[Trait("Category", "Settings")]
public class TfaValidateEmptyCodeTests(
    AspireAppFixture fixture)
    : TfaTestBase(fixture)
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TfaValidateAuthCode_EmptyCode_Returns400(string code)
    {
        // Arrange
        await EnableTfaAppAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") })
            .Build();
        var provider = new ASC.Security.Cryptography.EmailValidationKeyProvider(
            new ASC.Security.Cryptography.MachinePseudoKeys(configuration),
            null!,
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ASC.Security.Cryptography.EmailValidationKeyProvider>.Instance);
        var key = provider.GetEmailKey(Owner.Id + "TfaActivation", TenantId);

        await _webApiClient.Authenticate(null);
        _webApiClient.DefaultRequestHeaders.Add("confirm", $"type=TfaActivation&key={Uri.EscapeDataString(key)}&uid={Owner.Id}");

        try
        {
            // Act
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _tfaSettingsApi.TfaValidateAuthCodeAsync(
                    new TfaValidateRequestDto(code), TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(400);
            exception.ErrorContent!.ToString().Should().Contain("Validation code field cannot be empty");
        }
        finally
        {
            _webApiClient.DefaultRequestHeaders.Remove("confirm");
        }
    }
}
