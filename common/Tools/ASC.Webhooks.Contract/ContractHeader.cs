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

namespace ASC.Webhooks.Contract;

/// <summary>
/// The part of the contract the payload types cannot say: <c>info</c> with the serialization and transport notes,
/// the description of the trigger map and the <see cref="SchemaBuilder.EntryIdSchema"/> schema. Built as
/// <c>Microsoft.OpenApi</c> objects, as the REST documents build theirs in <c>OpenApiExtension</c>, with the
/// transport numbers and header names read from <see cref="WebhookDelivery"/>, so the text follows the sender.
/// </summary>
internal static class ContractHeader
{
    public const string TriggerMapKey = "x-docspace-trigger-payloads";

    private const string TriggerMapDescription =
        "Machine-readable trigger -> payload schema map. Source of truth for the SDK dispatch layer. Generated " +
        "from the trigger table in common/Tools/ASC.Webhooks.Contract.\n";

    private const string DeliverySuffix = "Delivery";

    private const string PayloadSuffix = "Payload";

    /// <summary>
    /// What each trigger family is about, as the description of its tag. A family missing here stops the generator.
    /// </summary>
    private static readonly Dictionary<string, string> _familyDescriptions = new()
    {
        ["user"] = "Members of the portal: added, invited, changed or removed.",
        ["group"] = "Groups of members.",
        ["file"] = "Files, in rooms and in the personal section alike.",
        ["folder"] = "Folders, in rooms and in the personal section alike.",
        ["room"] = "Rooms.",
        ["agent"] = "AI agents, which are rooms of their own kind and are delivered as rooms.",
        ["form"] = "PDF forms: submitted, filled out by everyone, or stopped."
    };

    /// <summary>
    /// What raises a trigger outside the REST API, written to follow "Also raised by". The API operations come from
    /// the generated API documents instead (<see cref="RaisingOperations"/>); this is the part no action declares.
    /// </summary>
    private static readonly Dictionary<WebhookTrigger, string> _otherSources = new()
    {
        [WebhookTrigger.FileCreated] = "an AI agent during a chat, for every document, presentation or form it creates with its editor tools",
        [WebhookTrigger.FileUpdated] = "the editor, every time it saves a document, a force-save included; submitting a filled form raises `form.submit` instead",
        [WebhookTrigger.FileUploaded] = "an upload of the parts to the `location` link that creating an upload session returns, once the last part arrives or that link finalizes the session",
        [WebhookTrigger.FileTrashed] = "the clean-up of a room with a file lifetime, for every expired file it moves to the trash",
        [WebhookTrigger.FileDownloaded] = "a download through the file's `viewUrl`, once per download",
        [WebhookTrigger.FileDeleted] = "the automatic clean-up of the trash, and the clean-up of a room with a file lifetime set to delete permanently, for every file they delete",
        [WebhookTrigger.FolderDeleted] = "the automatic clean-up of the trash, for every expired folder lying directly in the trash that it deletes",
        [WebhookTrigger.FolderCreated] = "the automatic clean-up of a guest's personal folder left unchanged for 30 days, once for the folder it creates in the personal section of the member who takes over the files and folders the guest shared from it",
        [WebhookTrigger.FormSubmit] = "the editor when a user submits a filled form",
        [WebhookTrigger.FormFilledOut] = "the editor when the last role of a form in a virtual data room submits it"
    };

    /// <summary>
    /// What a receiver may expect a trigger to cover and it does not, written as a sentence of its own after the sources.
    /// </summary>
    private static readonly Dictionary<WebhookTrigger, string> _notRaisedFor = new()
    {
        [WebhookTrigger.UserUpdated] = "Not raised when only the groups of a user change - adding to, moving between or removing from a group, or becoming its manager: that is `group.updated` of each group concerned, and the `groups` of the user's payload are read again from the next `user.*` event."
    };

    /// <summary>
    /// The roles a trigger can be offered to, in the order and the names the REST API uses for them.
    /// </summary>
    private static readonly EmployeeType[] _roles = [EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin, EmployeeType.User];

    /// <summary>
    /// The root of the contract with everything but the trigger map's entries, the tags, the webhooks and the
    /// generated schemas, each of which the generator adds to the node already in place.
    /// </summary>
    public static YamlMappingNode Build()
    {
        var info = new OpenApiInfo
        {
            Title = "ONLYOFFICE DocSpace Webhooks",
            Version = "1.0.0",
            Description = GetDescription()
        };

        var entryId = new OpenApiSchema
        {
            Description = "Entry identifier. Generic T in the domain types: integer for entries in DocSpace's own " +
                          "storage, string for entries backed by a third-party provider. Both occur on the same portal.\n",
            OneOf = [new OpenApiSchema { Type = JsonSchemaType.Integer }, new OpenApiSchema { Type = JsonSchemaType.String }]
        };

        return new YamlMappingNode
        {
            { "openapi", "3.1.0" },
            { "info", ToYaml(info.SerializeAsV31) },
            { "tags", new YamlSequenceNode() },
            { "paths", new YamlMappingNode() },
            { "webhooks", new YamlMappingNode() },
            { TriggerMapKey, new YamlMappingNode { { "description", ContractYamlWriter.Text(TriggerMapDescription) } } },
            {
                "components", new YamlMappingNode
                {
                    { "schemas", new YamlMappingNode { { SchemaBuilder.EntryIdSchema, ToYaml(entryId.SerializeAsV31) } } }
                }
            }
        };
    }

    /// <summary>
    /// The name of the schema a delivery with this payload is described by: <c>FilePayload</c> gives
    /// <c>FileDelivery</c>.
    /// </summary>
    public static string GetDeliveryName(string payloadSchema)
    {
        return payloadSchema.EndsWith(PayloadSuffix, StringComparison.Ordinal)
            ? payloadSchema[..^PayloadSuffix.Length] + DeliverySuffix
            : throw new InvalidOperationException($"The payload schema {payloadSchema} does not end in {PayloadSuffix}.");
    }

    /// <summary>
    /// The tag of a trigger family.
    /// </summary>
    public static YamlMappingNode GetTag(string family)
    {
        if (!_familyDescriptions.TryGetValue(family, out var description))
        {
            throw new InvalidOperationException(
                $"No tag description for the trigger family {family}: add it to {nameof(ContractHeader)}.");
        }

        return new YamlMappingNode { { "name", family }, { "description", ContractYamlWriter.Text(description) } };
    }

    /// <summary>
    /// A delivery whose payload is <paramref name="payloadSchema"/>: the envelope with <c>payload</c> typed, so that a
    /// receiver can read the whole body into one model.
    /// </summary>
    public static YamlMappingNode GetDeliverySchema(string payloadSchema)
    {
        return new YamlMappingNode
        {
            {
                "description", ContractYamlWriter.Text(
                    $"A delivery whose payload is a {payloadSchema}: {SchemaBuilder.EnvelopeSchema} with `payload` typed.")
            },
            {
                "allOf", new YamlSequenceNode(
                    Ref(SchemaBuilder.EnvelopeSchema),
                    new YamlMappingNode
                    {
                        { "type", "object" },
                        { "properties", new YamlMappingNode { { "payload", Ref(payloadSchema) } } }
                    })
            }
        };
    }

    /// <summary>
    /// The entry of <c>webhooks</c> for one trigger: the POST the sender makes, its headers, its body and what the
    /// receiver's answer means.
    /// </summary>
    public static YamlMappingNode GetWebhook(WebhookTrigger trigger, string payloadSchema, IReadOnlyList<RaisingOperation> raisedBy)
    {
        var name = trigger.ToCustomString();
        var roles = string.Join(", ", _roles.Where(r => trigger.IsAvailableFor(r)).Select(r => $"`{r}`"));

        var operation = new YamlMappingNode
        {
            { "operationId", GetOperationId(name) },
            { "summary", ContractYamlWriter.Text(name) },
            {
                "description", ContractYamlWriter.Text(
                    $"Sent on the `{name}` event. The body is a {GetDeliveryName(payloadSchema)}, whose `payload` is a " +
                    $"{payloadSchema}. Roles that may subscribe to it: {roles}.\n\n" + GetSources(trigger, raisedBy))
            },
            { "tags", new YamlSequenceNode(new YamlScalarNode(trigger.GetTargetType())) },
            {
                "parameters", new YamlSequenceNode(
                    HeaderParameter(WebhookDelivery.SignatureHeader,
                        "`sha256=` and the uppercase hex HMAC-SHA256 of the raw body under the subscription's secret. " +
                        "The only header that is authenticated; compare it case-insensitively."),
                    HeaderParameter(WebhookDelivery.EventIdHeader,
                        "A copy of `event.id`, for dropping an already-seen delivery without parsing the body. Not " +
                        "covered by the signature."),
                    HeaderParameter(WebhookDelivery.EventTimestampHeader,
                        "A copy of `event.createOn`, in the same ISO-8601 UTC spelling. Not covered by the signature."))
            },
            {
                "requestBody", new YamlMappingNode
                {
                    { "required", new YamlScalarNode("true") { Style = ScalarStyle.Plain } },
                    {
                        "content", new YamlMappingNode
                        {
                            { "application/json", new YamlMappingNode { { "schema", Ref(GetDeliveryName(payloadSchema)) } } }
                        }
                    }
                }
            },
            {
                "responses", new YamlMappingNode
                {
                    { "2XX", Response("Acknowledges the delivery. The answer's body is kept in the delivery log, not interpreted.") },
                    { new YamlScalarNode("410") { Style = ScalarStyle.SingleQuoted }, Response("Deletes the subscription at once; nothing is retried or sent to it any more.") },
                    {
                        "default", Response(
                            "Any other status, a redirect, a timeout or a network error fails the attempt, which is " +
                            "retried on the schedule the description of this document gives.")
                    }
                }
            }
        };

        return new YamlMappingNode { { "post", operation } };
    }

    /// <summary>
    /// What raises a trigger: the REST API operations that declare it (<see cref="RaisingOperations"/>), then what the
    /// portal raises it on by itself.
    /// </summary>
    private static string GetSources(WebhookTrigger trigger, IReadOnlyList<RaisingOperation> raisedBy)
    {
        var text = new StringBuilder();

        if (raisedBy.Count > 0)
        {
            text.Append("Raised by these REST API operations:\n\n");

            foreach (var operation in raisedBy)
            {
                text.Append($"- `{operation.Method} {operation.Path}` ({operation.Summary})");
                text.Append(operation.When is null ? "\n" : $", {operation.When}\n");
            }
        }
        var hasOther = _otherSources.TryGetValue(trigger, out var other);

        if (raisedBy.Count == 0)
        {
            text.Append(hasOther ? $"No REST API operation raises it; it is raised by {other}.\n" : "No REST API operation raises it.\n");
        }
        else if (hasOther)
        {
            text.Append($"\nAlso raised by {other}.\n");
        }

        if (_notRaisedFor.TryGetValue(trigger, out var notRaised))
        {
            text.Append($"\n{notRaised}\n");
        }

        return text.ToString();
    }

    private static string GetOperationId(string trigger)
    {
        var parts = trigger.Split('.');

        return parts[0] + string.Concat(parts.Skip(1).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    /// <summary>
    /// A required header of the delivery request, as an entry of the operation's <c>parameters</c>.
    /// </summary>
    private static YamlMappingNode HeaderParameter(string name, string description)
    {
        return new YamlMappingNode
        {
            { "name", name },
            { "in", "header" },
            { "required", new YamlScalarNode("true") { Style = ScalarStyle.Plain } },
            { "description", ContractYamlWriter.Text(description) },
            { "schema", new YamlMappingNode { { "type", "string" } } }
        };
    }

    private static YamlMappingNode Response(string description)
    {
        return new YamlMappingNode { { "description", ContractYamlWriter.Text(description) } };
    }

    private static YamlMappingNode Ref(string schemaName)
    {
        return new YamlMappingNode { { "$ref", "#/components/schemas/" + schemaName } };
    }

    private static string GetDescription()
    {
        var intervals = WebhookDelivery.DefaultRetryIntervals;
        var hours = (int)Math.Round(intervals.Aggregate(TimeSpan.Zero, (sum, interval) => sum + interval).TotalHours);
        var jitter = (int)Math.Round(WebhookDelivery.RetryJitter * 100);
        var schedule = string.Join(", ", intervals.Take(intervals.Count - 1).Select(Short)) + " and " + Short(intervals[^1]);

        // CommonMark, as an OpenAPI description is, and published as the overview page of the webhooks reference:
        // no line may start with four spaces of indentation outside a list, or it turns into a code block there, and
        // the text speaks of what a receiver sees, never of the classes that produce it
        return $"""
            Wire contract for **outbound** DocSpace webhook deliveries.

            This document is not the REST API spec and shares no schemas with it. The payloads are models of their
            own (`UserPayload`, `GroupPayload`, `FilePayload`, `FolderPayload`, `RoomPayload`,
            `FormSubmitPayload`).

            Every trigger is an entry of `webhooks` below, and its body is the `*{DeliverySuffix}` schema of its
            payload: the envelope with `payload` typed. The same mapping, in machine-readable form, is
            `{TriggerMapKey}`.

            ## Serialization

            1. Property names are camelCase.
            2. A property that holds its default value - `null`, `false`, `0` or the default member of an enum - is
               left out of the body. Consequently almost nothing here is `required`, and an absent property never
               means "unset": it means the default value.
            3. A body carries the properties of its schema and no others; the computed ones among them are sent
               like the rest.
            4. Every timestamp, in the envelope and in the payload alike, is a UTC ISO-8601 value in whole seconds
               ending in `Z`, e.g. `2026-09-17T10:22:56Z`.
            5. An entry id is a number for an entry kept in the portal's own storage and a string for one in
               third-party storage, modelled here as `oneOf` integer and string.

            ## Transport

            Every delivery is a `POST` with `Content-Type: application/json; charset=utf-8`. Redirects are not
            followed. Each request waits {Short(WebhookDelivery.DefaultRequestTimeout)} for a response by default (an installation can change it with the
            `{WebhookDelivery.RequestTimeoutKey}` setting), so a receiver must acknowledge fast and process out of
            band. Any non-2xx answer, timeout or network error is a failed attempt. A failed delivery is retried
            after {schedule} (each +/-{jitter}% jitter), i.e. {intervals.Count + 1} attempts over about {hours} hours by
            default (an installation can change the schedule with the `{WebhookDelivery.RetryIntervalsKey}`
            setting), and then abandoned. A portal admin or the subscription's creator can retry it manually from
            the delivery log, which starts a fresh series under the same `event.id`. When a delivery is abandoned and
            the subscription has had no successful delivery for more than {WebhookDelivery.DefaultTrustedDays} days by default (an
            installation can change it with the `{WebhookDelivery.TrustedDaysKey}` setting), the subscription is
            disabled: nothing is sent to it until a portal admin or its creator enables it again.

            ## Headers

            - `{WebhookDelivery.SignatureHeader}` - `sha256=` followed by the uppercase hex HMAC-SHA256 of the raw
              body, keyed with the subscription's secret. GitHub emits lowercase hex, so compare case-insensitively.
              This is the only header that is authenticated.
            - `{WebhookDelivery.EventIdHeader}` - a copy of `event.id`.
            - `{WebhookDelivery.EventTimestampHeader}` - a copy of `event.createOn`, in the same ISO-8601 UTC
              spelling, e.g. `2026-09-17T10:22:56Z`.

            The last two exist so a receiver can drop an already-seen delivery without parsing the body. They are
            **not** covered by the signature, which is computed over the body alone, so anything in transit can
            rewrite them. Reject on them freely - dropping a delivery is fail-safe, and whoever can rewrite a header
            could drop the request instead - but never accept on them: after verifying, read `event.id` from the
            parsed body, it is the authoritative value.

            The signature covers the body only; it does not prove when the request was sent. `event.createOn` is the
            moment of the event, not of the request: it stays the same through the automatic retries (about {hours}
            hours on the default schedule) and through a manual retry at any later time, so a freshness window on
            it, or on the timestamp header, rejects legitimate retries. Deduplicate on `event.id` instead: keep the
            ids already processed and drop a delivery whose id is among them. That is the replay defence.

            """;
    }

    /// <summary>
    /// A duration as the description writes it: 30s, 5m, 2h.
    /// </summary>
    private static string Short(TimeSpan value)
    {
        if (value.Ticks % TimeSpan.TicksPerHour == 0)
        {
            return $"{(int)value.TotalHours}h";
        }

        return value.Ticks % TimeSpan.TicksPerMinute == 0 ? $"{(int)value.TotalMinutes}m" : $"{(int)value.TotalSeconds}s";
    }

    /// <summary>
    /// An OpenAPI object as the YAML nodes the contract writer takes, through the object's own 3.1 serialization.
    /// </summary>
    private static YamlNode ToYaml(Action<IOpenApiWriter> serialize)
    {
        using var text = new StringWriter();

        var writer = new OpenApiJsonWriter(text);
        serialize(writer);

        return ToYaml(JsonNode.Parse(text.ToString()));
    }

    private static YamlNode ToYaml(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                var mapping = new YamlMappingNode();

                foreach (var (key, value) in obj)
                {
                    mapping.Add(key, ToYaml(value));
                }

                return mapping;

            case JsonArray array:
                return new YamlSequenceNode(array.Select(ToYaml));

            case JsonValue value when value.TryGetValue<string>(out var text):
                return ContractYamlWriter.Text(text);

            default:
                return new YamlScalarNode(node?.ToJsonString() ?? "null") { Style = ScalarStyle.Plain };
        }
    }
}
