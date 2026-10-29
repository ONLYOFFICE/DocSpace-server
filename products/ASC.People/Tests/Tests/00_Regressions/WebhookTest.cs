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

namespace ASC.People.Tests.Tests._00_Regressions;

public class WebhookTest(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "73741")]
    public async Task GetTriggers_CheckUserRestrictions()
    {
        await _peopleClient.Authenticate(Owner);

        var user = await InviteContact(EmployeeType.User);

        await _peopleClient.Authenticate(user);

        var triggers = await _webhooksApi.GetWebhookTriggersAsync(TestContext.Current.CancellationToken);

        triggers.Should().NotBeNull();
        triggers.Response.Should().NotBeNull();

        var trigger = triggers.Response.FirstOrDefault(x => x.Id == (long)WebhookTrigger.UserCreated);
        trigger.Should().NotBeNull();
        trigger!.Available.Should().BeFalse("a User may not subscribe to the creation of users");

        // The triggers are checked before the target is probed, so this address is never contacted; were the
        // trigger check gone, the blacklisted localhost would answer the request with a 400 of its own, which
        // is why the message is asserted and not only the status.
        var createWebhooksConfigRequestsDto = new CreateWebhooksConfigRequestDto(
            "test",
            "http://localhost",
            "test123!@#%ABC",
            true,
            false,
            WebhookTrigger.UserCreated
            );

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _webhooksApi.CreateWebhookAsync(
                createWebhooksConfigRequestsDto,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent?.ToString().Should().Contain("Trigger is not available");
    }

    [Fact]
    [Trait("Category", "Bug")]
    [Trait("Bug", "80980")]
    public async Task WebhookCreation_CheckBlacklist()
    {
        await _peopleClient.Authenticate(Owner);

        var createWebhooksConfigRequestsDto = new CreateWebhooksConfigRequestDto(
            "test",
            "http://localhost",
            "test123!@#%ABC",
            true);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _webhooksApi.CreateWebhookAsync(
                createWebhooksConfigRequestsDto,
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }
}
