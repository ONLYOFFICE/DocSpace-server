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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Storage;

/// <summary>
/// Shared plumbing for the portal / CDN / backup storage suites.
/// </summary>
/// <remarks>
/// <see cref="PortalClients"/> does not expose <c>StorageApi</c> yet, so the typed client is built here on the
/// per-test Web.Api <see cref="HttpClient"/>.
///
/// The TypeScript suite ran against SaaS, where <c>DemandAccessSpacePermissionAsync</c> refuses every call that
/// reconfigures storage with 403 even for the owner. This host is standalone, where that gate is open, so those
/// calls are no longer refused. The two resets (<c>DELETE storage</c> starts a data migration of the portal,
/// <c>DELETE storage/cdn</c> clears the CDN settings through the shared storage helper) and a successful update
/// would change real storage state, so they are not ported; the update calls are only exercised with a module
/// that is not configured on the host, which is refused before anything is written.
/// </remarks>
public abstract class StorageTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    private StorageApi? _storageApi;

    protected StorageApi Storage => _storageApi ??= new StorageApi(
        _webApiClient,
        new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

    /// <summary>
    /// A storage request for a module the host does not have configured.
    /// </summary>
    protected static StorageRequestDto UnconfiguredModuleRequest()
    {
        return new StorageRequestDto("s3", [new ItemKeyValuePairStringString("acesskey", "x")]);
    }
}
