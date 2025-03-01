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

namespace ASC.Webhooks.Core;

public class WebhookPayload<T>
{
    public WebhookPayloadTriggerInfo Trigger { get; init; }
    public T Payload { get; init; }
    public WebhookPayloadConfigInfo Webhook { get; init; }

    public WebhookPayload(WebhookTrigger trigger, DbWebhooksConfig config, T data, Guid userId)
    {

        Trigger = new WebhookPayloadTriggerInfo
        {
            CreateBy = userId,
            CreateOn = DateTime.UtcNow,
            Trigger = trigger
        };

        Payload = data;

        Webhook = new WebhookPayloadConfigInfo
        {
            Id = config.Id,
            Name = config.Name,
            Url = config.Uri,
            Triggers = config.Triggers,
            LastFailureOn = config.LastFailureOn,
            LastFailureContent = config.LastFailureContent,
            LastSuccessOn = config.LastSuccessOn,
            RetryCount = 0
        };
    }
}

public class WebhookPayloadTriggerInfo
{
    public DateTime CreateOn { get; set; }
    public Guid CreateBy { get; set; }
    public WebhookTrigger Trigger { get; set; }
}

public class WebhookPayloadConfigInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public WebhookTrigger Triggers { get; set; }

    public DateTime? LastFailureOn { get; set; }
    public string LastFailureContent { get; set; }
    public DateTime? LastSuccessOn { get; set; }

    public int RetryCount { get; set; }

    //public DateTime RetryOn { get; set; }
}