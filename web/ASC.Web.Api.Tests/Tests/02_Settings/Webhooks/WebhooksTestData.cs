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
    /// A fresh webhook configuration payload with a unique name and target URL. The backend
    /// validates the URL with a HEAD request and requires a 200 response — with redirects NOT
    /// followed, so <c>onlyoffice.com</c> (301 to www) fails; <c>example.com</c> answers 200
    /// directly, which is also what the TS suite uses. <c>secretKey</c> is required by the
    /// backend on create/update even though the SDK marks it optional.
    /// </summary>
    public static CreateWebhooksConfigRequestsDto CreateWebhookDto(
        bool enabled = false, bool ssl = false, WebhookTrigger? triggers = null)
    {
        var suffix = Initializer.Faker.Random.AlphaNumeric(10);

        return new CreateWebhooksConfigRequestsDto(
            name: $"webhook-{suffix}",
            uri: $"https://example.com/?id={suffix}",
            secretKey: Initializer.Faker.Random.AlphaNumeric(20),
            enabled: enabled,
            ssl: ssl,
            triggers: triggers);
    }
}
