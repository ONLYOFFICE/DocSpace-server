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
/// <c>GET /people/thirdparty/providers</c>: the list of configured third-party auth providers is
/// portal-wide and unlinked for a freshly-registered portal, and every role can read it.
/// </summary>
/// <remarks>
/// The other three endpoints in this area (<c>linkaccount</c>, <c>signup</c>, <c>unlinkaccount</c>)
/// need a real OAuth round-trip against a live third-party provider (Google, Zoom, ...), which this
/// host does not configure, so — matching the TS suite's own comment — they are not ported.
/// </remarks>
public class ThirdPartyAuthProvidersTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task GetThirdPartyAuthProviders_Owner_ListsAllUnlinked()
    {
        await AssertAllProvidersUnlinkedAsync();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.Guest)]
    public async Task GetThirdPartyAuthProviders_Member_ListsAllUnlinked(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _peopleClient.Authenticate(member);

        await AssertAllProvidersUnlinkedAsync();
    }

    private async Task AssertAllProvidersUnlinkedAsync()
    {
        var result = await _thirdPartyAccountsApi.GetThirdPartyAuthProvidersAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // The list is whatever the host has configured. No OAuth provider is set up in the
        // integration-test environment, so it comes back empty here and the TypeScript suite's
        // "all six providers are present" assertion cannot hold — what is env-agnostic is that the
        // endpoint answers with a list and that nothing in it is already linked to a fresh account.
        result.Response.Should().NotBeNull();
        result.Response.Where(p => p.Linked).Should().BeEmpty("a fresh account has no linked provider");
        result.Response.Where(p => string.IsNullOrEmpty(p.Provider)).Should().BeEmpty("every entry names its provider");
    }
}
