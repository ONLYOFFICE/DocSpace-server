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

namespace ASC.Core.Common.Tests.Webhooks;

/// <summary>
/// The bug the typed webhook payloads exist to fix: System.Text.Json serializes by DECLARED type, so publishing a
/// payload typed as its abstract base would silently drop every subtype field. The envelope writes its payload by
/// the runtime type instead, which is why <c>FileEntryWebhookDtoHelper</c> may declare the base.
/// </summary>
[Trait("Category", "Webhooks")]
public class WebhookPayloadSerializationTests
{
    // The publisher's own options, so a change to them reaches these tests. No TenantUtil: it is read only for a
    // DateTimeKind.Local value, and every date in these bodies is UTC or default.
    private static readonly JsonSerializerOptions _options = WebhookPublisher.CreateSerializerOptions(null);

    [Fact]
    public void EntryPayload_KeepsSubtypeFields_WhenDeclaredAsBase()
    {
        FileEntryWebhookDto<int> payload = new FileWebhookDto<int> { Id = 10, Title = "321.xlsx", Version = 3, ContentLength = 4096 };

        var json = JsonSerializer.Serialize(new WebhookPayload<FileEntryWebhookDto<int>, int> { Payload = payload }, _options);

        json.Should().Contain("\"version\":3").And.Contain("\"contentLength\":4096");
    }

    [Fact]
    public void EntryPayload_IsTheSame_WhateverItIsDeclaredAs()
    {
        var room = new RoomWebhookDto<int> { Id = 7, Title = "Contracts", QuotaLimit = 1024 };

        var asBase = JsonSerializer.Serialize(new WebhookPayload<FileEntryWebhookDto<int>, int> { Payload = room }, _options);
        var asObject = JsonSerializer.Serialize(new WebhookPayload<object, int> { Payload = room }, _options);
        var asItself = JsonSerializer.Serialize(new WebhookPayload<RoomWebhookDto<int>, int> { Payload = room }, _options);

        asBase.Should().Be(asItself).And.Be(asObject).And.Contain("\"quotaLimit\":1024");
    }

    [Fact]
    public void MissingPayload_IsLeftOut_AsBefore()
    {
        var asBase = JsonSerializer.Serialize(new WebhookPayload<FileEntryWebhookDto<int>, int>(), _options);
        var asObject = JsonSerializer.Serialize(new WebhookPayload<object, int>(), _options);

        asBase.Should().Be(asObject).And.NotContain("\"payload\"");
    }
}
