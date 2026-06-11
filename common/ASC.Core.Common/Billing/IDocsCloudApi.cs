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

namespace ASC.Core.Billing;

/// <summary>
/// Type-safe REST contract for the external DocsCloud service, implemented by Refit.
/// All paths are relative — the base address, authentication and resilience are configured in
/// <see cref="DocsCloudHttpClientExtension.AddDocsCloudHttpClient"/>. The public wrapper is <see cref="DocsCloudClient"/>.
/// </summary>
public interface IDocsCloudApi
{
    [Get("/api/healthcheck")]
    Task HealthCheckAsync();

    [Get("/api/tenant")]
    Task<DocsCloudTenant> GetTenantAsync([Query] string portalId);

    [Get("/api/tenant/info")]
    Task<string> GetTenantInfoAsync([Query] string portalId);

    [Get("/api/tenant/config")]
    Task<DocsCloudConfigDto> GetTenantConfigAsync([Query] string portalId);

    [Post("/api/tenant/config")]
    Task<DocsCloudConfigDto> UpdateTenantConfigAsync([Query] string portalId, [Body] DocsCloudConfigDto config);

    [Get("/api/tenant/quota")]
    Task<Stream> GetTenantQuotaAsync([Query] string portalId);

    [Get("/api/tenant/usage")]
    Task<DocsCloudUsage> GetTenantUsageAsync([Query] string portalId);
}
