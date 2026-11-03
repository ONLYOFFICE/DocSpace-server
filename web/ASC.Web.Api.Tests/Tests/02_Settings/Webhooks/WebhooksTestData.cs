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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Webhooks;

/// <summary>
/// Request bodies shared by the webhooks suites — kept here, one level up, so every sibling in
/// this folder can reach it without a <c>using</c>.
/// </summary>
internal static class WebhooksTestData
{
    /// <summary>
    /// A fresh webhook configuration payload with a unique name, for a caller that sets <c>uri</c> itself. The
    /// backend probes the URL with a HEAD request and wants a success code, so the default points at a path of
    /// the WireMock receiver that nothing is stubbed for: it answers 404 and the create fails at once, offline,
    /// instead of reaching the internet. Use <see cref="CreateWebhookDtoAsync"/> for a body that is accepted.
    /// <c>secretKey</c> is required by the backend on create even though the SDK marks it optional.
    /// </summary>
    public static CreateWebhooksConfigRequestDto CreateWebhookDto(
        bool enabled = false, bool ssl = false, WebhookTrigger? triggers = null)
    {
        var suffix = Initializer.Faker.Random.AlphaNumeric(10);

        return new CreateWebhooksConfigRequestDto(
            name: $"webhook-{suffix}",
            uri: $"http://{ASC.AppHost.Configuration.Constants.WebhookReceiverHost}:{ASC.AppHost.Configuration.Constants.WebhookReceiverPort}/hooks/unstubbed-{suffix}",
            secretKey: Initializer.Faker.Random.AlphaNumeric(20),
            enabled: enabled,
            ssl: ssl,
            triggers: triggers);
    }

    /// <summary>
    /// A fresh webhook configuration payload pointed at an endpoint of its own on the WireMock receiver, which
    /// answers the HEAD probe with 200 and every delivery with 200. The suite needs no internet for it.
    /// </summary>
    public static async Task<CreateWebhooksConfigRequestDto> CreateWebhookDtoAsync(
        HttpClient receiverAdmin, bool enabled = false, WebhookTrigger? triggers = null, string? targetId = null)
    {
        var receiver = await WebhookReceiver.CreateAsync(receiverAdmin);
        var dto = CreateWebhookDto(enabled: enabled, triggers: triggers);

        dto.Uri = receiver.TargetUri;

        if (targetId is not null)
        {
            dto.TargetId = targetId;
        }

        return dto;
    }
}
