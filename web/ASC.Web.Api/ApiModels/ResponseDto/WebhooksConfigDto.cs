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
/// The webhook configuration parameters.
/// </summary>
/// <example>
/// {
///   "name": "example value",
///   "uri": "example value",
///   "enabled": true,
///   "sSL": true,
///   "triggers": 0,
///   "targetId": "example value",
///   "createdBy": {},
///   "createdOn": "2024-01-15T10:30:00Z",
///   "modifiedBy": {},
///   "modifiedOn": "2024-01-15T10:30:00Z",
///   "lastFailureOn": "2024-01-15T10:30:00Z",
///   "lastFailureContent": "example value",
///   "lastSuccessOn": "2024-01-15T10:30:00Z"
/// }
/// </example>
public class WebhooksConfigDto
{
    /// <summary>
    /// The webhook ID.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The webhook name.
    /// </summary>
    /// <example>John</example>
    public string Name { get; set; }

    /// <summary>
    /// The webhook URI.
    /// </summary>
    /// <example>https://example.com</example>
    public string Uri { get; set; }

    /// <summary>
    /// Specifies if the webhooks are enabled or not.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// The webhook SSL verification (enabled or not).
    /// </summary>
    /// <example>true</example>
    public bool SSL { get; set; }

    /// <summary>
    /// The webhook trigger type.
    /// </summary>
    /// <example>All</example>
    public WebhookTrigger Triggers { get; set; }

    /// <summary>
    /// The webhook target ID.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string TargetId { get; set; }

    /// <summary>
    /// The user who created the webhook.
    /// </summary>
    /// <example>{ "displayName": "Mike Zanyatski" }</example>
    public EmployeeDto CreatedBy { get; set; }

    /// <summary>
    /// The date and time when the webhook was created.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? CreatedOn { get; set; }

    /// <summary>
    /// The user who modified the webhook.
    /// </summary>
    /// <example>{ "displayName": "Mike Zanyatski" }</example>
    public EmployeeDto ModifiedBy { get; set; }

    /// <summary>
    /// The date and time when the webhook was modified.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// The date and time of the webhook last failure.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? LastFailureOn { get; set; }

    /// <summary>
    /// The webhook last failure content.
    /// </summary>
    /// <example>example value</example>
    public string LastFailureContent { get; set; }

    /// <summary>
    /// The date and time of the webhook last success.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? LastSuccessOn { get; set; }
}

/// <summary>
/// The webhook configuration with its status.
/// </summary>
public class WebhooksConfigWithStatusDto
{
    /// <summary>
    /// The webhook configuration.
    /// </summary>
    /// <example>{ "id": 1, "name": "John" }</example>
    public WebhooksConfigDto Configs { get; set; }

    /// <summary>
    /// The webhook status.
    /// </summary>
    /// <example>1</example>
    public int Status { get; set; }
}

[Scope]
public class WebhooksConfigDtoHelper(TenantUtil tenantUtil, EmployeeDtoHelper employeeDtoHelper)
{
    public async Task<WebhooksConfigDto> GetAsync(DbWebhooksConfig dbWebhooksConfig)
    {
        return new WebhooksConfigDto
        {
            Id = dbWebhooksConfig.Id,
            Name = dbWebhooksConfig.Name,
            Uri = dbWebhooksConfig.Uri,
            Enabled = dbWebhooksConfig.Enabled,
            SSL = dbWebhooksConfig.SSL,
            Triggers = dbWebhooksConfig.Triggers,
            TargetId = dbWebhooksConfig.TargetId,
            CreatedBy = dbWebhooksConfig.CreatedBy.HasValue ? await employeeDtoHelper.GetAsync(dbWebhooksConfig.CreatedBy.Value) : null,
            CreatedOn = dbWebhooksConfig.CreatedOn.HasValue ? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.CreatedOn.Value) : null,
            ModifiedBy = dbWebhooksConfig.ModifiedBy.HasValue ? await employeeDtoHelper.GetAsync(dbWebhooksConfig.ModifiedBy.Value) : null,
            ModifiedOn = dbWebhooksConfig.ModifiedOn.HasValue ? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.ModifiedOn.Value) : null,
            LastFailureOn = dbWebhooksConfig.LastFailureOn.HasValue ? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.LastFailureOn.Value) : null,
            LastFailureContent = dbWebhooksConfig.LastFailureContent,
            LastSuccessOn = dbWebhooksConfig.LastSuccessOn.HasValue ? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.LastSuccessOn.Value) : null
        };
    }

    public async Task<WebhooksConfigWithStatusDto> GetAsync(WebhooksConfigWithStatus webhooksConfigWithStatus)
    {
        return new WebhooksConfigWithStatusDto
        {
            Configs = await GetAsync(webhooksConfigWithStatus.WebhooksConfig),
            Status = webhooksConfigWithStatus.Status ?? 0
        };
    }
}