// (c) Copyright Ascensio System SIA 2009-2025
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

public class WebhookPayload<T1, T2>
{
    public WebhookPayloadEventInfo Event { get; set; }
    public T1 Payload { get; set; }
    public WebhookPayloadConfigInfo<T2> Webhook { get; set; }

    public WebhookPayload()
    {
    }

    public WebhookPayload(WebhookTrigger trigger, DbWebhooksConfig config, T1 data, T2 dataId, Guid userId)
    {
        var now = GetShortUtcNow();

        Event = new WebhookPayloadEventInfo
        {
            CreateBy = userId,
            CreateOn = now,
            Id = 0, // log Id is unknown until saved. initialized on send
            Trigger = trigger.ToCustomString(),
            TriggerId = (int)trigger
        };

        Payload = data;

        var triggers = config.Triggers == WebhookTrigger.All
            ? [config.Triggers.ToCustomString()]
            : Enum.GetValues<WebhookTrigger>()
                .Where(flag => config.Triggers.HasFlag(flag) && flag != 0)
                .Select(flag => flag.ToCustomString())
                .ToArray();

        Webhook = new WebhookPayloadConfigInfo<T2>
        {
            Id = config.Id,
            Name = config.Name,
            Url = config.Uri,
            Triggers = triggers,

            Target = new WebhookPayloadTargetInfo<T2>
            {
                Id = dataId,
                Type = trigger.GetTargetType()
            }

            // initialized on send: LastFailureOn, LastFailureContent, LastSuccessOn, RetryCount, RetryOn
        };
    }

    public DateTime GetShortUtcNow()
    {
        var now = DateTime.UtcNow;
        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);
        return now;
    }
}

public class WebhookPayloadEventInfo
{
    public int Id { get; set; }
    public DateTime CreateOn { get; set; }
    public Guid CreateBy { get; set; }
    public string Trigger { get; set; }
    public int TriggerId { get; set; }
}

public class WebhookPayloadTargetInfo<T>
{
    public T Id { get; set; }
    public string Type { get; set; }
}

public class WebhookPayloadConfigInfo<T2>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
    public string[] Triggers { get; set; }

    public WebhookPayloadTargetInfo<T2> Target { get; set; }

    public DateTime? LastFailureOn { get; set; }
    public string LastFailureContent { get; set; }
    public DateTime? LastSuccessOn { get; set; }

    public int RetryCount { get; set; }
    public DateTime? RetryOn { get; set; }
}