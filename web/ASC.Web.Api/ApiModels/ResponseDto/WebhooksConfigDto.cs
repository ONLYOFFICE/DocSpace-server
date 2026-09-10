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
/// One webhook subscription of the portal: where deliveries go, which events they cover, and how they have fared.
/// </summary>
/// <example>
/// {
///   "id": 1,
///   "name": "Room activity",
///   "uri": "https://example.com/hooks/docspace",
///   "enabled": true,
///   "sSL": true,
///   "triggers": 128,
///   "targetId": "00000000-0000-0000-0000-000000000001",
///   "createdBy": { "displayName": "Mike Zanyatski" },
///   "createdOn": "2024-01-15T10:30:00Z",
///   "lastSuccessOn": "2024-01-15T10:30:00Z"
/// }
/// </example>
public class WebhooksConfigDto
{
    /// <summary>
    /// The identifier of the subscription, which is what `PUT api/2.0/settings/webhook`,
    /// `DELETE api/2.0/settings/webhook/{id}` and the `configId` filter of the delivery log address it by.
    /// </summary>
    /// <example>1</example>
    public required int Id { get; set; }

    /// <summary>
    /// The label the subscription was given, free text with no meaning to the portal.
    /// </summary>
    /// <example>Room activity</example>
    public string Name { get; set; }

    /// <summary>
    /// The address every delivery is posted to. The signing secret that lets the receiver verify a delivery is
    /// never part of this answer, so it has to be kept from the moment the subscription was created.
    /// </summary>
    /// <example>https://example.com/hooks/docspace</example>
    public string Uri { get; set; }

    /// <summary>
    /// Whether the subscription is delivering. While it is `false` events are dropped rather than queued, so
    /// nothing arrives late after it is switched back on.
    /// </summary>
    /// <example>true</example>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether the certificate of `uri` is verified before a delivery. While it is `false` a self-signed
    /// certificate is accepted as well.
    /// </summary>
    /// <example>true</example>
    public bool SSL { get; set; }

    /// <summary>
    /// The events the subscription covers, as the bits of `GET api/2.0/settings/webhook/triggers` added
    /// together. `0` is the catch-all and means every event, not none.
    /// </summary>
    /// <example>128</example>
    public WebhookTrigger Triggers { get; set; }

    /// <summary>
    /// The single room or file the subscription is narrowed to, empty for a subscription that covers the whole
    /// portal. It is kept as an opaque value, so both a numeric and a third-party identifier can appear.
    /// </summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public string TargetId { get; set; }

    /// <summary>
    /// The member who created the subscription, which is also who a non-administrator is limited to seeing. It is
    /// empty for a subscription created by a portal background job.
    /// </summary>
    /// <example>{ "displayName": "Mike Zanyatski" }</example>
    public EmployeeDto CreatedBy { get; set; }

    /// <summary>
    /// When the subscription was created, in the portal time zone.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? CreatedOn { get; set; }

    /// <summary>
    /// The member who last changed the subscription, empty while nobody has changed it since it was created.
    /// </summary>
    /// <example>{ "displayName": "Mike Zanyatski" }</example>
    public EmployeeDto ModifiedBy { get; set; }

    /// <summary>
    /// When it was last changed, in the portal time zone, and empty under the same condition as `modifiedBy`.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// When a delivery last failed, in the portal time zone. It is empty for a subscription that has never
    /// failed, and it is not cleared by a later success - compare it with `lastSuccessOn` to see which came last.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? LastFailureOn { get; set; }

    /// <summary>
    /// What the target answered on that failure, truncated, for diagnosing without opening the delivery log. It
    /// is empty when the failure produced no body at all, a timeout for instance.
    /// </summary>
    /// <example>502 Bad Gateway</example>
    public string LastFailureContent { get; set; }

    /// <summary>
    /// When a delivery last succeeded, in the portal time zone, empty for a subscription that has never
    /// delivered. Both this and `lastFailureOn` being empty means nothing has been attempted yet.
    /// </summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime? LastSuccessOn { get; set; }
}

/// <summary>
/// A webhook subscription together with how its last delivery ended.
/// </summary>
public class WebhooksConfigWithStatusDto
{
    /// <summary>
    /// The subscription itself. Despite the plural name it is one subscription, not a list.
    /// </summary>
    /// <example>{ "id": 1, "name": "Room activity" }</example>
    public WebhooksConfigDto Configs { get; set; }

    /// <summary>
    /// The HTTP status code the target answered on the last attempt. `0` means nothing has been delivered yet,
    /// which is not the same as a failure.
    /// </summary>
    /// <example>200</example>
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