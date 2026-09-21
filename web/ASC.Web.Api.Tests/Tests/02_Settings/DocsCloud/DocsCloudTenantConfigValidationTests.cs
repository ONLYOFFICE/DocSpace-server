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

namespace ASC.Web.Api.Tests.Tests._02_Settings.DocsCloud;

/// <summary>
/// PUT /api/2.0/settings/docscloud/tenant/config — data annotation validation on the bound
/// <see cref="DocsCloudConfig"/> model. <c>[ApiEndpoint]</c> derives from
/// <c>ApiControllerAttribute</c>, so an invalid model never reaches the action body (and never
/// reaches the DocsCloud client this environment cannot talk to) — ASP.NET Core answers 400
/// on its own. Every case here used to be accepted and forwarded to DocsCloud unbounded, or (for
/// the file size limit) throw and answer 500; both are now rejected with a clean 400.
/// </summary>
[Trait("Category", "Settings")]
public class DocsCloudTenantConfigValidationTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const int MaxStringLength = 255;

    [Fact]
    [Trait("Bug", "83327")]
    public async Task UpdateTenantConfig_TenantNameTooLong_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var config = new DocsCloudConfig { TenantName = new string('a', MaxStringLength + 1) };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.UpdateTenantConfigAsync(config, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("TenantName");
    }

    [Fact]
    [Trait("Bug", "83327")]
    public async Task UpdateTenantConfig_SecuritySecretTooLong_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var config = new DocsCloudConfig { Security = new DocsCloudSecurityConfig { Secret = new string('a', MaxStringLength + 1) } };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.UpdateTenantConfigAsync(config, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Secret");
    }

    [Fact]
    [Trait("Bug", "83327")]
    public async Task UpdateTenantConfig_SecurityHeaderTooLong_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var config = new DocsCloudConfig { Security = new DocsCloudSecurityConfig { Header = new string('a', MaxStringLength + 1) } };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.UpdateTenantConfigAsync(config, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Header");
    }

    [Fact]
    [Trait("Bug", "83327")]
    public async Task UpdateTenantConfig_IpFilterAddressTooLong_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var config = new DocsCloudConfig
        {
            IpFilter = new DocsCloudIpFilterConfig
            {
                Rules = [new DocsCloudIpFilterRule { Address = new string('a', MaxStringLength + 1), Allowed = true }]
            }
        };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.UpdateTenantConfigAsync(config, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Address");
    }

    [Fact]
    [Trait("Bug", "83326")]
    public async Task UpdateTenantConfig_FileSizeLimitAboveInt32Range_ReturnsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var config = new DocsCloudConfig { Server = new DocsCloudServerConfig { FileSizeLimit = 9999999999 } };

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _docsCloudApi.UpdateTenantConfigAsync(config, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }
}
