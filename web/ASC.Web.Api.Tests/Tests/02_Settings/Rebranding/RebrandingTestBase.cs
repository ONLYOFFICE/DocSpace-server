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
/// Shared plumbing for the rebranding / white-label suites.
/// </summary>
/// <remarks>
/// <see cref="PortalClients"/> does not expose <c>RebrandingApi</c> yet, so the typed client is built
/// here on the per-test Web.Api <see cref="HttpClient"/> (same base address, same auth headers as
/// every other Web.Api client). Replace with a <c>PortalClients.RebrandingApi</c> property once it exists.
///
/// The integration host is started with <c>APP_HOSTING_STANDALONE=true</c>, while the TypeScript suite
/// ran against SaaS. The gates in front of the mutating calls depend on that:
/// <c>DemandAccessSpacePermissionAsync</c> (company/additional save and delete) refuses every non-standalone
/// installation, and the plan check (<c>402</c>) follows the portal quota's <c>Customization</c> flag.
/// Nothing here may write the default tenant (-1) settings: they are shared by every portal on the host.
/// </remarks>
public abstract class RebrandingTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    private RebrandingApi? _rebrandingApi;

    protected RebrandingApi Rebranding => _rebrandingApi ??= new RebrandingApi(
        _webApiClient,
        new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });
}
