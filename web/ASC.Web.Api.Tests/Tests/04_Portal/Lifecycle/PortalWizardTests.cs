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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Lifecycle;

/// <summary>
/// The first-run wizard of a server installation: a portal registered without a password waits for it, hands
/// its Wizard link to an anonymous caller in <c>getPortalSettings</c>, and <c>completeWizard</c> with that link
/// sets the owner's password once.
/// </summary>
[Trait("Category", "Portal")]
public class PortalWizardTests(AspireAppFixture fixture) : PortalLifecycleTestBase(fixture)
{
    private Task<PortalClients> CreateWizardPortalAsync()
    {
        return Fixture.CreatePortalAsync(model => model.Password = null, TestContext.Current.CancellationToken);
    }

    private static async Task<string> WizardTokenAsync(PortalClients portal)
    {
        var settings = (await portal.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        return settings.WizardToken;
    }

    private static WizardRequestDto Wizard(string? email = null, string? passwordHash = null)
    {
        return new WizardRequestDto(
            email: email ?? Initializer.OwnerEmail,
            passwordHash: passwordHash ?? Initializer.GetClientPassword(Initializer.OwnerPassword),
            lng: "en-US",
            timeZone: "UTC");
    }

    [Fact]
    public async Task GetPortalSettings_PortalRegisteredWithoutPassword_GivesAnonymousCallerTheWizardLink()
    {
        using var portal = await CreateWizardPortalAsync();

        (await WizardTokenAsync(portal)).Should().NotBeNullOrEmpty("the wizard is pending until the owner sets a password");
    }

    [Fact]
    public async Task GetPortalSettings_PortalRegisteredWithPassword_HasNoWizardLink()
    {
        await _webApiClient.Authenticate(null);

        (await WizardTokenAsync(Clients)).Should().BeNullOrEmpty("a registration with a password completes the wizard at once");
    }

    [Fact]
    public async Task CompleteWizard_WithTheWizardLink_SetsThePasswordOnce()
    {
        using var portal = await CreateWizardPortalAsync();
        var token = await WizardTokenAsync(portal);

        portal.WebApiHttpClient.DefaultRequestHeaders.Add("confirm", token);

        var result = (await portal.CommonSettingsApi.CompleteWizardAsync(Wizard(), TestContext.Current.CancellationToken)).Response;

        result.Completed.Should().BeTrue();

        // The same link again finds the wizard passed.
        var repeat = await Assert.ThrowsAsync<ApiException>(
            async () => await portal.CommonSettingsApi.CompleteWizardAsync(Wizard(), TestContext.Current.CancellationToken));

        repeat.ErrorCode.Should().Be(500);
        repeat.ErrorContent!.ToString().Should().Contain("Wizard passed.");

        portal.WebApiHttpClient.DefaultRequestHeaders.Remove("confirm");

        (await WizardTokenAsync(portal)).Should().BeNullOrEmpty("the wizard is completed");

        // The owner signs in with the password the wizard set.
        await portal.WebApiHttpClient.Authenticate(portal.Owner);

        var information = (await portal.PortalSettingsApi.GetPortalInformationAsync(TestContext.Current.CancellationToken)).Response;

        information.OwnerId.Should().Be(portal.Owner.Id);
    }

    [Fact]
    public async Task CompleteWizard_WithoutTheLink_IsRefused()
    {
        using var portal = await CreateWizardPortalAsync();

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await portal.CommonSettingsApi.CompleteWizardAsync(Wizard(), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task CompleteWizard_MalformedEmail_ReturnsBadRequest()
    {
        using var portal = await CreateWizardPortalAsync();

        portal.WebApiHttpClient.DefaultRequestHeaders.Add("confirm", await WizardTokenAsync(portal));

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await portal.CommonSettingsApi.CompleteWizardAsync(Wizard(email: "not-an-address"), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);

        // The refusal left the wizard pending.
        portal.WebApiHttpClient.DefaultRequestHeaders.Remove("confirm");

        (await WizardTokenAsync(portal)).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CompleteWizard_EmptyPasswordHash_ReturnsServerError()
    {
        using var portal = await CreateWizardPortalAsync();

        portal.WebApiHttpClient.DefaultRequestHeaders.Add("confirm", await WizardTokenAsync(portal));

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await portal.CommonSettingsApi.CompleteWizardAsync(Wizard(passwordHash: ""), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(500);
    }
}
