// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Web.Api.ApiModels.ResponseDto;

/// <summary>
/// </summary>
public class WebhooksConfigDto
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// URI
    /// </summary>
    public string Uri { get; set; }

    /// <summary>
    /// Specifies if the webhooks are enabled or not
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// SSL Verification
    /// </summary>
    public bool SSL { get; set; }

    /// <summary>
    /// Triggers
    /// </summary>
    public WebhookTrigger Triggers { get; set; }

    /// <summary>
    /// Create by
    /// </summary>
    public EmployeeDto CreatedBy { get; set; }

    /// <summary>
    /// Create on
    /// </summary>
    public DateTime? CreatedOn { get; set; }

    /// <summary>
    /// Modified by
    /// </summary>
    public EmployeeDto ModifiedBy { get; set; }

    /// <summary>
    /// Modified on
    /// </summary>
    public DateTime? ModifiedOn { get; set; }

    /// <summary>
    /// Last failure on
    /// </summary>
    public DateTime? LastFailureOn { get; set; }

    /// <summary>
    /// Last failure content
    /// </summary>
    public string LastFailureContent { get; set; }

    /// <summary>
    /// Last success on
    /// </summary>
    public DateTime? LastSuccessOn { get; set; }
}

public class WebhooksConfigWithStatusDto
{
    /// <summary>
    /// Configs
    /// </summary>
    public WebhooksConfigDto Configs { get; set; }

    /// <summary>
    /// Status
    /// </summary>
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
            CreatedBy = dbWebhooksConfig.CreatedBy.HasValue ? await employeeDtoHelper.GetAsync(dbWebhooksConfig.CreatedBy.Value) : null,
            CreatedOn = dbWebhooksConfig.CreatedOn.HasValue? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.CreatedOn.Value) : null,
            ModifiedBy = dbWebhooksConfig.ModifiedBy.HasValue ? await employeeDtoHelper.GetAsync(dbWebhooksConfig.ModifiedBy.Value) : null,
            ModifiedOn = dbWebhooksConfig.ModifiedOn.HasValue ? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.ModifiedOn.Value) : null,
            LastFailureOn = dbWebhooksConfig.LastFailureOn.HasValue ? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.LastFailureOn.Value) : null,
            LastFailureContent = dbWebhooksConfig.LastFailureContent,
            LastSuccessOn = dbWebhooksConfig.LastSuccessOn.HasValue ? tenantUtil.DateTimeFromUtc(dbWebhooksConfig.LastSuccessOn.Value) : null
        };
    }

    public async Task<WebhooksConfigWithStatusDto> GetAsync(WebhooksConfigWithStatus webhooksConfigWithStatus)
    {
        return new WebhooksConfigWithStatusDto {
            Configs = await GetAsync(webhooksConfigWithStatus.WebhooksConfig),
            Status = webhooksConfigWithStatus.Status ?? 0
        };
    }
}