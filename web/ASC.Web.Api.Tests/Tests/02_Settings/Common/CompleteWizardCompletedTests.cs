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
/// PUT /api/2.0/settings/wizard/complete on a portal whose setup wizard is already completed. A test portal is
/// registered with a password, so the wizard is never reopened for it and stays completed, as its default is. The call
/// used to fail with a plain exception, a 500; it now returns 409 and changes nothing. The other refusals of the call
/// (AMI instance ID, email, password, license) come after this check, so the stack cannot reach them. The wizard link is
/// what <c>wizardToken</c> of GET settings carries, computed here from the stack's machine key, as
/// <c>UpdatePortalOwnerRefusalTests</c> computes its link.
/// </summary>
[Trait("Category", "Settings")]
public class CompleteWizardCompletedTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CompleteWizard_WizardAlreadyCompleted_Returns409AndChangesNothing()
    {
        // Arrange: a body that would move the owner to another address and password if it were applied
        var request = new WizardRequestDto(
            email: $"wizard-{Guid.NewGuid():N}@example.com",
            passwordHash: Initializer.GetClientPassword("Wizard-Pass-" + Guid.NewGuid().ToString("N")[..8]));

        // Act
        var exception = await CompleteWizardWithLinkAsync(request);

        // Assert
        exception.ErrorCode.Should().Be(409);
        exception.ErrorContent!.ToString().Should().Contain("Wizard passed.");

        await _peopleClient.Authenticate(Owner);
        var self = await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken);
        self.Response.Email.Should().Be(Owner.Email);
    }

    /// <summary>Calls the action anonymously, signed with the wizard link of the portal, and returns the refusal.</summary>
    private async Task<ApiException> CompleteWizardWithLinkAsync(WizardRequestDto request)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") })
            .Build();
        var provider = new ASC.Security.Cryptography.EmailValidationKeyProvider(
            new ASC.Security.Cryptography.MachinePseudoKeys(configuration),
            null!,
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ASC.Security.Cryptography.EmailValidationKeyProvider>.Instance);

        // The key the server issues for the wizard: no address, the confirmation type, the owner as the user.
        var key = provider.GetEmailKey("Wizard", TenantId);
        var confirm = $"type=Wizard&key={Uri.EscapeDataString(key)}&uid={Owner.Id}";

        await _webApiClient.Authenticate(null);
        _webApiClient.DefaultRequestHeaders.Add("confirm", confirm);
        try
        {
            return await Assert.ThrowsAsync<ApiException>(
                async () => await _commonSettingsApi.CompleteWizardAsync(request, TestContext.Current.CancellationToken));
        }
        finally
        {
            _webApiClient.DefaultRequestHeaders.Remove("confirm");
        }
    }
}
