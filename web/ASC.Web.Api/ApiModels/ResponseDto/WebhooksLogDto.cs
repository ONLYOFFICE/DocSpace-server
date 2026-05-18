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

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// The webhook log parameters.
/// </summary>
/// <example>
/// {
///   "configName": "example value",
///   "trigger": 0,
///   "creationTime": "2024-01-15T10:30:00Z",
///   "method": "example value",
///   "route": "example value",
///   "requestHeaders": "example value",
///   "requestPayload": "example value",
///   "responseHeaders": "example value",
///   "responsePayload": "example value",
///   "status": 1,
///   "delivery": "2024-01-15T10:30:00Z"
/// }
/// </example>
public class WebhooksLogDto
{
    /// <summary>
    /// The webhook log ID.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The webhook configuration name.
    /// </summary>
    /// <example>Example Name</example>
    public string ConfigName { get; set; }

    /// <summary>
    /// The webhook trigger type.
    /// </summary>
    /// <example>0</example>
    public WebhookTrigger Trigger { get; set; }

    /// <summary>
    /// The webhook creation time.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// The webhook method.
    /// </summary>
    /// <example>example value</example>
    public string Method { get; set; }

    /// <summary>
    /// The webhook route.
    /// </summary>
    /// <example>example value</example>
    public string Route { get; set; }

    /// <summary>
    /// The webhook request headers.
    /// </summary>
    /// <example>example value</example>
    public string RequestHeaders { get; set; }

    /// <summary>
    /// The webhook request payload.
    /// </summary>
    /// <example>example value</example>
    public string RequestPayload { get; set; }

    /// <summary>
    /// The webhook response headers.
    /// </summary>
    /// <example>example value</example>
    public string ResponseHeaders { get; set; }

    /// <summary>
    /// The webhook response payload.
    /// </summary>
    /// <example>example value</example>
    public string ResponsePayload { get; set; }

    /// <summary>
    /// The webhook status.
    /// </summary>
    /// <example>1</example>
    public int Status { get; set; }

    /// <summary>
    /// The webhook delivery time.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? Delivery { get; set; }
}

[Scope]
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class WebhooksLogDtoMapper(TenantUtil tenantUtil)
{
    public partial WebhooksLogDto Map(DbWebhooksLog source);

    private DateTime MapDateToUtc(DateTime source) => tenantUtil.DateTimeToUtc(source);
}