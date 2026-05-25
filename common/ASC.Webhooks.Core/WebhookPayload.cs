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
            TriggerId = (long)trigger
        };

        Payload = data;

        var triggers = config.Triggers == WebhookTrigger.All
            ? [config.Triggers.ToCustomString()]
            : Enum.GetValues<WebhookTrigger>()
                .Where(flag => config.Triggers.HasFlag(flag) && flag != 0)
                .Select(flag => flag.ToCustomString())
                .ToArray();

        var target = string.IsNullOrEmpty(config.TargetId)
            ? null
            : new WebhookPayloadTargetInfo<T2>
            {
                Id = dataId,
                Type = trigger.GetTargetType()
            };

        Webhook = new WebhookPayloadConfigInfo<T2>
        {
            Id = config.Id,
            Name = config.Name,
            Url = config.Uri,
            Triggers = triggers,
            Target = target

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
    public long TriggerId { get; set; }
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
