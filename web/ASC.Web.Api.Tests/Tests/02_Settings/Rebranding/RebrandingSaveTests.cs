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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Rebranding;

/// <summary>
/// The mutating rebranding calls as the portal owner. Two gates sit in front of them: a plan gate
/// (<c>402</c>, the portal quota has no Customization option) on the four whitelabel save calls, and an
/// installation gate (<c>403</c>, <c>DemandAccessSpacePermissionAsync</c>) on the company/additional save
/// and delete calls, which refuses every non-standalone installation.
/// </summary>
/// <remarks>
/// The TypeScript suite ran on SaaS, where the owner gets 403 from the installation gate. This host is
/// standalone, where that gate is open: the company/additional saves are refused by the plan gate
/// (<c>402</c>) instead, and the two deletes succeed and reset installation-wide (default-tenant) settings
/// that every portal on the host shares. The deletes are therefore not ported. The company save still
/// carries no email and the additional save the built-in values, so neither can write anything should
/// the plan gate ever open.
/// </remarks>
[Trait("Category", "Settings")]
public class RebrandingSaveTests(AspireAppFixture fixture) : RebrandingTestBase(fixture)
{
    [Fact]
    public async Task SaveWhiteLabelLogoText_Owner_ThrowsPaymentRequired()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelLogoTextAsync(
                whiteLabelRequestDto: new WhiteLabelRequestDto(logoText: "Autotest Brand"),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task SaveWhiteLabelSettings_Owner_ThrowsPaymentRequired()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelSettingsAsync(
                whiteLabelRequestDto: new WhiteLabelRequestDto(logo: []),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task SaveWhiteLabelSettingsFromFiles_OwnerWithoutFile_ThrowsAccessDenied()
    {
        // Act: the request is not a multipart form, so the controller refuses before any gate
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelSettingsFromFilesAsync(
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task SaveCompanyWhiteLabelSettings_Owner_ThrowsPaymentRequired()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveCompanyWhiteLabelSettingsAsync(
                CompanyRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }

    [Fact]
    public async Task SaveAdditionalWhiteLabelSettings_Owner_ThrowsPaymentRequired()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveAdditionalWhiteLabelSettingsAsync(
                AdditionalRequest, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }
}
