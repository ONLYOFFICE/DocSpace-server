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
/// What the rebranding endpoints answer as the owner of a portal on a plan without the customization option:
/// the logo slot listing (sizes, light/dark paths), request validation in front of the plan gate, the gate's
/// message, and the installation-default (<c>isDefault=true</c>) variants that only read or are stopped by the gate.
/// </summary>
/// <remarks>
/// Nothing here writes: every save is refused by validation or by the plan gate, and no restore is called with
/// <c>isDefault=true</c>, which would reset the installation defaults shared by every portal on the host.
/// </remarks>
[Trait("Category", "Settings")]
public class RebrandingContractTests(AspireAppFixture fixture) : RebrandingTestBase(fixture)
{
    private static readonly Dictionary<string, (int Width, int Height)> _slotSizes = new()
    {
        ["LightSmall"] = (422, 48),
        ["LoginPage"] = (772, 88),
        ["Favicon"] = (32, 32),
        ["DocsEditor"] = (172, 40),
        ["DocsEditorEmbed"] = (172, 40),
        ["LeftMenu"] = (56, 56),
        ["AboutPage"] = (442, 48),
        ["SpreadsheetEditor"] = (172, 40),
        ["SpreadsheetEditorEmbed"] = (172, 40),
        ["PresentationEditor"] = (172, 40),
        ["PresentationEditorEmbed"] = (172, 40),
        ["PdfEditor"] = (172, 40),
        ["PdfEditorEmbed"] = (172, 40),
        ["DiagramEditor"] = (172, 40),
        ["DiagramEditorEmbed"] = (172, 40)
    };

    [Fact]
    public async Task GetWhiteLabelLogos_Anonymous_ReportsTheTargetSizeOfEverySlot()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var logos = (await Rebranding.GetWhiteLabelLogosAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        logos.ToDictionary(logo => logo.Name, logo => (logo.Size.Width, logo.Size.Height)).Should().BeEquivalentTo(_slotSizes);
    }

    [Fact]
    public async Task GetWhiteLabelLogos_WithoutTheme_OmitsDarkWhereItEqualsLight()
    {
        // Act
        var logos = (await Rebranding.GetWhiteLabelLogosAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        logos.Should().OnlyContain(logo => !string.IsNullOrEmpty(logo.Path.Light) && logo.Path.Dark != logo.Path.Light);
        logos.Should().Contain(logo => logo.Path.Dark == null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetWhiteLabelLogos_WithTheme_FillsOnlyThatThemesPath(bool isDark)
    {
        // Act
        var logos = (await Rebranding.GetWhiteLabelLogosAsync(isDark: isDark, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        logos.Should().HaveCount(15);
        logos.Should().OnlyContain(logo => isDark
            ? logo.Path.Light == null && !string.IsNullOrEmpty(logo.Path.Dark)
            : logo.Path.Dark == null && !string.IsNullOrEmpty(logo.Path.Light));
    }

    [Fact]
    public async Task SaveWhiteLabelLogoText_Owner_PlanGateAnswersNotAllowedOption()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelLogoTextAsync(
                whiteLabelRequestDto: new WhiteLabelRequestDto(logoText: "Autotest Brand"),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
        exception.ErrorContent!.ToString().Should().Contain("Your pricing plan does not support this option");
    }

    [Fact]
    public async Task SaveWhiteLabelLogoText_LongerThan40Characters_ThrowsBadRequestBeforePlanGate()
    {
        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelLogoTextAsync(
                whiteLabelRequestDto: new WhiteLabelRequestDto(logoText: new string('a', 41)),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task SaveCompanyWhiteLabelSettings_InvalidEmail_ThrowsBadRequestBeforePlanGate()
    {
        // Arrange
        var request = new CompanyWhiteLabelSettingsRequestDto(new SaveCompanyInfoRequest(
            companyName: "Autotest Co",
            site: "https://example.com",
            email: "not-an-email",
            address: "Autotest address",
            phone: "+10000000000"));

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveCompanyWhiteLabelSettingsAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task GetWhiteLabelLogoText_OwnerWithIsDefault_ReadsInstallationDefault()
    {
        // Act
        var response = await Rebranding.GetWhiteLabelLogoTextAsync(isDefault: true, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SaveWhiteLabelLogoText_OwnerWithIsDefault_ThrowsPaymentRequired()
    {
        // Act: the installation check is open on a standalone host, the plan gate is not
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Rebranding.SaveWhiteLabelLogoTextAsync(
                isDefault: true,
                whiteLabelRequestDto: new WhiteLabelRequestDto(logoText: "Autotest Brand"),
                cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(402);
    }
}
