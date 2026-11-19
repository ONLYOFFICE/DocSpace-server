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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Common;

/// <summary>
/// POST /api/2.0/settings/deeplink with a body that has no <c>deepLinkSettings</c>. The action read the mode of the
/// missing object, and the NullReferenceException came back as a 500; it is refused with 400 now, like a mode outside
/// the defined set, and the stored mode stays.
/// </summary>
[Trait("Category", "Settings")]
public class DeepLinkMissingSettingsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task ConfigureDeepLink_NoDeepLinkSettings_Returns400AndKeepsMode()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _commonSettingsApi.ConfigureDeepLinkAsync(
            new DeepLinkConfigurationRequestDto(new DeepLinkSettingsRequestDto(DeepLinkHandlingMode.App)),
            TestContext.Current.CancellationToken);

        // Act - the generated model leaves out an unset deepLinkSettings, so the body is {}
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _commonSettingsApi.ConfigureDeepLinkAsync(
                new DeepLinkConfigurationRequestDto(),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("The request has no deepLinkSettings");

        var current = await _commonSettingsApi.GetDeepLinkSettingsAsync(TestContext.Current.CancellationToken);
        current.Response.HandlingMode.Should().Be(DeepLinkHandlingMode.App);
    }
}
