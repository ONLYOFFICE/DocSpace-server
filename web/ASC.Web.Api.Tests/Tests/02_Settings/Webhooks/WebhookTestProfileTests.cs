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


namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// The webhook settings the integration-test profile of the AppHost gives the services. No portal is involved, and
/// the test needs no running host: it lives here only because the profile's constants are in the AppHost project,
/// which no unit-test project references. Run on its own, it still waits for the Aspire host that the assembly
/// fixture of this suite starts for every test.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookTestProfileTests
{
    [Fact]
    public void TestBlacklist_IsTheDefaultWithOnlyTheLoopbackNarrowed()
    {
        // Arrange: the one intended difference, which lets the WireMock receiver on 127.0.0.2 through
        var expected = ASC.Core.Common.Security.UrlValidator.DefaultBlacklist
            .Select(network => network == "127.0.0.0/8" ? "127.0.0.1/32" : network);

        // Act
        var actual = ASC.AppHost.Configuration.Constants.WebhookTestBlacklist;

        // Assert: a configured list replaces the default as a whole, so a network added to the default and not to
        // the copy would be open in every test run
        actual.Should().Equal(expected);
    }
}
