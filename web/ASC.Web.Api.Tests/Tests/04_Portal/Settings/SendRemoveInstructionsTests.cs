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


namespace ASC.Web.Api.Tests.Tests._04_Portal.Settings;

/// <summary>
/// <c>POST /api/2.0/portal/sendremoveinstructions</c> - the anonymous call that mails the owner of a server
/// installation the removal link. It is hidden from the OpenAPI document, so it is called over raw HTTP, with the key
/// the server would have issued for the owner, computed here from the stack's machine key.
/// </summary>
public class SendRemoveInstructionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task SendRemoveInstructions_SixTimesInARow_SendOneLetter()
    {
        // A repeat builds nothing: no link and no letter. In this stack no notify service marks letters, so the repeats
        // are held back by the acceptance the first call records; the engine's mark is covered by RepeatAcceptKeyTests.
        await _webApiClient.Authenticate(null);
        await using var bus = await Fixture.ListenToEventBusAsync(nameof(NotifyItemIntegrationEvent), TestContext.Current.CancellationToken);
        var path = $"api/2.0/portal/sendremoveinstructions?userid={Owner.Id}&key={Uri.EscapeDataString(RemovalKey())}";

        for (var call = 0; call < 6; call++)
        {
            using var response = await _webApiClient.PostAsync(path, null, TestContext.Current.CancellationToken);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var letters = await bus.ReadAsync<NotifyItemIntegrationEvent>(
            l => l.TenantId == TenantId && l.Action.NotifyActionType == "ASC.Web.Studio.Core.Notify.PortalDeleteNotifyAction",
            TestContext.Current.CancellationToken);

        letters.Should().ContainSingle("the first call sends the letter, and none of the five repeats builds another");
    }

    // The key PortalController checks: issued for the owner's id and the removal, by the services' machine key.
    private string RemovalKey()
    {
        var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
        Microsoft.Extensions.Configuration.MemoryConfigurationBuilderExtensions.AddInMemoryCollection(
            builder, new Dictionary<string, string?> { ["core:machinekey"] = Fixture.GetAppHostSetting("core:machinekey") });
        var configuration = builder.Build();

        var provider = new ASC.Security.Cryptography.EmailValidationKeyProvider(
            new ASC.Security.Cryptography.MachinePseudoKeys(configuration),
            null!,
            configuration,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ASC.Security.Cryptography.EmailValidationKeyProvider>.Instance);

        return provider.GetEmailKey(Owner.Id + "PortalRemove", TenantId);
    }
}
