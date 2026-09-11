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

namespace ASC.People.Tests.Tests._07_Quota;

/// <summary>
/// Shared setup for the per-user quota suites (<c>PUT /people/userquota</c>,
/// <c>PUT /people/resetquota</c>): enabling the portal-wide default quota and, for the one
/// validation case that needs it, capping the portal's total storage.
/// </summary>
public abstract class QuotaTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    protected const long QuotaMinimalBytes = 104_857_600; // 100 MB
    protected const long DefaultQuotaUserBytes = 524_288_000; // 500 MB

    /// <summary>
    /// Enables the per-user storage quota and sets its portal-wide default, which is what
    /// <c>PUT /people/resetquota</c> falls back a user's quota to. The endpoint is marked
    /// <c>[ApiExplorerSettings(IgnoreApi = true)]</c> (see
    /// <c>SettingsController.SaveUserQuotaSettings</c>) and is therefore absent from the SDK, so
    /// this goes over raw HTTP against the WebApi client.
    /// </summary>
    protected async Task EnableUserQuotaAsync(long defaultQuotaBytes)
    {
        await _webApiClient.Authenticate(Owner);

        var json = JsonSerializer.Serialize(new { enableQuota = true, defaultQuota = defaultQuotaBytes });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _webApiClient.PostAsync(
            "api/2.0/settings/userquotasettings", content, TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// The current portal's tenant id, read back from <c>GET /api/2.0/portal</c>. No SDK client on
    /// <see cref="BaseTest"/> surfaces the tenant id of the running test's portal, so it is read
    /// over raw HTTP instead of adding one.
    /// </summary>
    protected async Task<int> GetTenantIdAsync()
    {
        await _webApiClient.Authenticate(Owner);

        using var response = await _webApiClient.GetAsync("api/2.0/portal", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(body);

        return document.RootElement.GetProperty("response").GetProperty("tenantId").GetInt32();
    }

    /// <summary>
    /// Caps the portal's total storage through the standalone-only tenant quota setting, so that a
    /// per-user quota request can be made to exceed "the total storage" deterministically. This
    /// host runs no billing plan, so the portal's total storage is otherwise effectively unbounded
    /// and a huge per-user quota would never be rejected for that reason.
    /// </summary>
    protected async Task CapTotalStorageAsync(long quotaBytes)
    {
        await _webApiClient.Authenticate(Owner);

        var tenantId = await GetTenantIdAsync();

        await _settingsQuotaApi.SetTenantQuotaSettingsAsync(
            new TenantQuotaSettingsRequestsDto(tenantId, quotaBytes),
            TestContext.Current.CancellationToken);
    }
}
