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

namespace ASC.Web.Api.Tests.Tests._02_Settings.License;

/// <summary>
/// Shared plumbing for the license suites.
/// </summary>
/// <remarks>
/// <see cref="PortalClients"/> does not expose <c>LicenseApi</c>, so the typed client is built here on the
/// per-test Web.Api <see cref="HttpClient"/> (same base address, same auth headers).
///
/// The integration host is a standalone installation without a license path (<c>license:file:path</c> is empty
/// in the shared configuration), i.e. the open-source edition: <c>TenantExtra.Enterprise</c> is false. Every
/// license call that could replace the installation's license or rewrite its quota returns before doing so on
/// such a host (accept answers the "not allowed" message, refresh answers false, upload 415), which is what makes
/// the calls safe to make from parallel test portals. Never call them on a stack that has a license path.
/// </remarks>
public abstract class LicenseTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    private LicenseApi? _licenseApi;

    protected LicenseApi LicenseClient => _licenseApi ??= new LicenseApi(
        _webApiClient,
        new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

    /// <summary>
    /// The resource text the open-source edition answers to license activation and to the plan gate.
    /// </summary>
    protected const string NotAllowedOption = "Your pricing plan does not support this option";

    /// <summary>
    /// A file that is not a license; it is never read, because the edition check comes first.
    /// </summary>
    protected static List<FileParameter> NotALicenseFile() =>
        [new FileParameter("license.lic", "application/octet-stream", new MemoryStream("not a license"u8.ToArray()))];
}
