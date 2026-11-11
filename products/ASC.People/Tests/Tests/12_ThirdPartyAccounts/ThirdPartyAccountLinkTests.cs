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

namespace ASC.People.Tests.Tests._12_ThirdPartyAccounts;

/// <summary>
/// What the third-party endpoints do without a live provider: the provider list answers an
/// anonymous caller, and unlinking a provider that was never linked is a 200 that changes nothing.
/// A real link needs a profile produced by a completed provider authorization, which this host
/// cannot produce.
/// </summary>
public class ThirdPartyAccountLinkTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetThirdPartyAuthProviders_Anonymous_ReturnsListWithNothingLinked()
    {
        await _peopleClient.Authenticate(null);

        var result = await _thirdPartyAccountsApi.GetThirdPartyAuthProvidersAsync(cancellationToken: TestContext.Current.CancellationToken);

        // the stand configures no provider, so the list is usually empty: the check is that the
        // anonymous call answers 200 and that whatever comes back is unlinked and carries its login URL
        result.Response.Should().NotBeNull();
        result.Response.Where(p => p.Linked || !p.Url.Contains("auth=" + p.Provider)).Should().BeEmpty();
    }

    [Fact]
    public async Task UnlinkThirdPartyAccount_ProviderNotLinked_SucceedsAndLinksNothing()
    {
        var member = await InviteContact(EmployeeType.User);
        await _peopleClient.Authenticate(member);

        await _thirdPartyAccountsApi.UnlinkThirdPartyAccountAsync("google", TestContext.Current.CancellationToken);

        var providers = await _thirdPartyAccountsApi.GetThirdPartyAuthProvidersAsync(cancellationToken: TestContext.Current.CancellationToken);
        providers.Response.Should().NotContain(p => p.Linked);
    }
}
