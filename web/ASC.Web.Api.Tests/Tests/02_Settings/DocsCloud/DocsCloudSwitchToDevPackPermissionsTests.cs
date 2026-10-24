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


namespace ASC.Web.Api.Tests.Tests._02_Settings.DocsCloud;

/// <summary>
/// POST /api/2.0/settings/docscloud/switchtodevpack — access control. Only a portal owner or a
/// DocSpaceAdmin may switch the portal to the DevPack; every other role is denied, and an anonymous caller is rejected before
/// authorization is even evaluated. The allowed path needs a funded wallet with a DocsCloud service
/// (payment setup and top-up), which the TypeScript suite arranges through its payments helper and this
/// project does not have, so it is not ported here.
/// </summary>
/// <remarks>
/// The per-role denials of the TypeScript suite are not ported either: on this host
/// <c>PaymentHelper.DemandConfigured</c> refuses every authenticated caller with 403 "Tariff service is
/// not configured" before the role is checked, so such a test would pass for the wrong reason.
/// </remarks>
[Trait("Category", "Settings")]
public class DocsCloudSwitchToDevPackPermissionsTests(
    AspireAppFixture fixture)
    : DocsCloudTestBase(fixture)
{
    [Fact]
    public async Task SwitchToDevPack_Anonymous_ThrowsUnauthorized()
    {
        await ExpectUnauthorizedAsync(
            async () => await _docsCloudApi.SwitchToDevPackAsync(new DocsCloudDevPackRequestDto(quantity: 1), TestContext.Current.CancellationToken));
    }
}
