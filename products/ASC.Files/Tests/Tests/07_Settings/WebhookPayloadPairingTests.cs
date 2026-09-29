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

using System.Reflection;
using System.Text.Json.Serialization;

using ASC.Api.Core.Webhook.Payloads;
using ASC.Files.Core.ApiModels.WebhookDto;
using ASC.Webhooks.Core;

using WebhookTrigger = ASC.Webhooks.Core.WebhookTrigger;

namespace ASC.Files.Tests.Tests._07_Settings;

/// <summary>
/// The trigger-to-payload pairing is declared twice - once on the <c>WebhookTrigger</c> field and once on the
/// DTO - because <c>ASC.Webhooks.Core</c> sits below the product assemblies and cannot name their types. These
/// tests are what keeps the two halves honest, and what the SDK contract generator relies on.
/// </summary>
[Trait("Category", "Settings")]
public class WebhookPayloadPairingTests
{
    private static readonly Assembly[] _payloadAssemblies =
    [
        typeof(UserWebhookDto).Assembly,
        typeof(FileWebhookDto<int>).Assembly
    ];

    [Fact]
    public void EveryTrigger_DeclaresAPayloadKind()
    {
        var type = typeof(WebhookTrigger);

        var missing = Enum.GetValues<WebhookTrigger>()
            .Where(t => type.GetField(t.ToString())?.GetCustomAttribute<WebhookPayloadAttribute>() == null)
            .ToList();

        missing.Should().BeEmpty("every trigger must say which payload it carries");
    }

    [Fact]
    public void OnlyTheCatchAll_HasNoPayload()
    {
        var withoutPayload = Enum.GetValues<WebhookTrigger>()
            .Where(t => t.GetPayloadKind() == WebhookPayloadKind.None)
            .ToList();

        withoutPayload.Should().Equal(WebhookTrigger.All);
    }

    [Fact]
    public void EveryDeclaredKind_ResolvesToExactlyOneDto()
    {
        var payloadTypes = WebhookPayloadRegistry.GetPayloadTypes(_payloadAssemblies);

        foreach (var (trigger, kind) in WebhookPayloadRegistry.GetTriggerKinds())
        {
            payloadTypes.Should().ContainKey(kind, "trigger {0} declares {1}", trigger.ToCustomString(), kind);
            payloadTypes[kind].Should().ContainSingle("payload kind {0} must be modelled by exactly one DTO", kind);
        }
    }

    [Fact]
    public void EveryDto_IsClaimedByAtLeastOneTrigger()
    {
        var declaredKinds = WebhookPayloadRegistry.GetTriggerKinds().Values.ToHashSet();

        var orphans = WebhookPayloadRegistry.GetPayloadTypes(_payloadAssemblies)
            .Where(x => !declaredKinds.Contains(x.Key))
            .SelectMany(x => x.Value)
            .ToList();

        orphans.Should().BeEmpty("a payload DTO that no trigger sends is dead weight on the SDK contract");
    }

    /// <summary>
    /// The bug this whole payload change exists to fix: System.Text.Json serializes by DECLARED type, so
    /// publishing a payload typed as its abstract base silently drops every subtype field. A property declared
    /// as <c>object</c> is the one case where the runtime type is used instead, which is why
    /// <c>FileEntryWebhookDtoHelper.GetAsync</c> returns <c>object</c> and must keep doing so.
    /// </summary>
    [Fact]
    public void EntryPayload_KeepsSubtypeFields_WhenPublished()
    {
        // the same options WebhookPublisher serializes with
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        object payload = new FileWebhookDto<int> { Id = 10, Title = "321.xlsx", Version = 3, ContentLength = 4096 };

        var json = JsonSerializer.Serialize(new WebhookPayload<object, int>
        {
            Payload = payload
        }, options);

        json.Should().Contain("\"version\":3").And.Contain("\"contentLength\":4096");
    }

    [Fact]
    public void EntryPayloadFactory_StaysWeaklyTyped()
    {
        var returnType = typeof(FileEntryWebhookDtoHelper)
            .GetMethod(nameof(FileEntryWebhookDtoHelper.GetAsync))!
            .ReturnType;

        returnType.Should().Be(typeof(Task<object>),
            "narrowing this to the abstract base would erase every file, folder and room field on the wire");
    }

    [Theory]
    [InlineData(WebhookTrigger.UserCreated, WebhookPayloadKind.User)]
    [InlineData(WebhookTrigger.GroupUpdated, WebhookPayloadKind.Group)]
    [InlineData(WebhookTrigger.FileUploaded, WebhookPayloadKind.File)]
    [InlineData(WebhookTrigger.FolderMoved, WebhookPayloadKind.Folder)]
    [InlineData(WebhookTrigger.RoomArchived, WebhookPayloadKind.Room)]
    [InlineData(WebhookTrigger.AgentCreated, WebhookPayloadKind.Room)]
    [InlineData(WebhookTrigger.FormSubmit, WebhookPayloadKind.FormSubmit)]
    [InlineData(WebhookTrigger.FormFilledOut, WebhookPayloadKind.File)]
    public void Trigger_CarriesTheExpectedKind(WebhookTrigger trigger, WebhookPayloadKind expected)
    {
        trigger.GetPayloadKind().Should().Be(expected);
    }
}
