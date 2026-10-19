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
/// The read side of rebranding as the portal owner: availability flag, built-in company and
/// additional-resource settings, licensor data, logo slots and the logo text. A fresh portal has
/// nothing customised, so everything reports the built-in ONLYOFFICE values.
/// </summary>
/// <remarks>
/// The TypeScript suite expected <c>enablewhitelabel</c> to be <c>false</c> because its SaaS test plan has no
/// branding. That is a property of the plan, not a constant: this host is standalone, so the value is
/// asserted as ported and may diverge on a plan with the Customization option.
/// </remarks>
[Trait("Category", "Settings")]
public class RebrandingReadTests(AspireAppFixture fixture) : RebrandingTestBase(fixture)
{
    [Fact]
    public async Task GetEnableWhitelabel_Owner_ReportsPlanWithoutBranding()
    {
        // Act
        var response = await Rebranding.GetEnableWhitelabelAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().BeFalse();
    }

    [Fact]
    public async Task GetCompanyWhiteLabelSettings_Owner_ReturnsBuiltInCompany()
    {
        // Act
        var response = await Rebranding.GetCompanyWhiteLabelSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.CompanyName.Should().Be("Ascensio System SIA");
        response.Response.Site.Should().Be("https://www.onlyoffice.com");
        response.Response.IsLicensor.Should().BeTrue();
        response.Response.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetAdditionalWhiteLabelSettings_Owner_ReturnsAllResourcesEnabled()
    {
        // Act
        var response = await Rebranding.GetAdditionalWhiteLabelSettingsAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.StartDocsEnabled.Should().BeTrue();
        response.Response.HelpCenterEnabled.Should().BeTrue();
        response.Response.FeedbackAndSupportEnabled.Should().BeTrue();
        response.Response.UserForumEnabled.Should().BeTrue();
        response.Response.VideoGuidesEnabled.Should().BeTrue();
        response.Response.LicenseAgreementsEnabled.Should().BeTrue();
        response.Response.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetLicensorData_Owner_ReturnsSingleBuiltInLicensor()
    {
        // Act
        var response = await Rebranding.GetLicensorDataAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().ContainSingle();
        response.Response[0].CompanyName.Should().Be("Ascensio System SIA");
        response.Response[0].IsLicensor.Should().BeTrue();
    }

    [Fact]
    public async Task GetWhiteLabelLogos_Anonymous_ReturnsLogoSlotsWithoutNotification()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var response = await Rebranding.GetWhiteLabelLogosAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert: the notification slot is derived from the login-page logo and is left out of this list
        response.StatusCode.Should().Be(200);
        response.Response.Should().HaveCount(15);
        response.Response.Select(logo => logo.Name).Should().NotContain("Notification");
    }

    [Fact]
    public async Task GetIsDefaultWhiteLabelLogos_Owner_ReportsEverySlotAsBuiltIn()
    {
        // Act
        var response = await Rebranding.GetIsDefaultWhiteLabelLogosAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().HaveCount(16);
        response.Response.Select(logo => logo.Name).Should().Contain("Notification");
        response.Response.Should().OnlyContain(logo => logo.Default);
    }

    [Fact]
    public async Task GetWhiteLabelLogoText_Owner_ReturnsBuiltInWordmark()
    {
        // Act
        var response = await Rebranding.GetWhiteLabelLogoTextAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Should().Be("ONLYOFFICE");
    }

    [Fact]
    public async Task GetIsDefaultWhiteLabelLogoText_Owner_ReportsBuiltInWordmark()
    {
        // Act
        var response = await Rebranding.GetIsDefaultWhiteLabelLogoTextAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(200);
        response.Response.Name.Should().Be("logotext");
        response.Response.Default.Should().BeTrue();
    }
}
