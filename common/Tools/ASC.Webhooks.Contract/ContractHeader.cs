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

    // the column, inside the Headers block of the description, where the text of each header starts
    private const int HeaderColumn = 26;

    /// <summary>
    /// The root of the contract with everything but the entries of the trigger map and the generated schemas.
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
            { "paths", new YamlMappingNode() },
            { TriggerMapKey, new YamlMappingNode { { "description", ContractYamlWriter.Text(TriggerMapDescription) } } },
            {
                "components", new YamlMappingNode
                {
                    { "schemas", new YamlMappingNode { { SchemaBuilder.EntryIdSchema, ToYaml(entryId.SerializeAsV31) } } }
                }
            }
        };
    }

    private static string GetDescription()
    {
        var intervals = WebhookDelivery.DefaultRetryIntervals;
        var hours = (int)Math.Round(intervals.Aggregate(TimeSpan.Zero, (sum, interval) => sum + interval).TotalHours);
        var jitter = (int)Math.Round(WebhookDelivery.RetryJitter * 100);
        var schedule = string.Join(", ", intervals.Take(intervals.Count - 1).Select(Short)) + " and " + Short(intervals[^1]);

        return $"""
            Wire contract for OUTBOUND DocSpace webhook deliveries.

            This document is NOT the REST API spec and shares no schemas with it. Webhook
            payloads are DTOs owned by the webhook contract (UserWebhookDto, GroupWebhookDto,
            FileWebhookDto, FolderWebhookDto, RoomWebhookDto, FormSubmitWebhookDto).

            Which trigger sends which payload is the map below, {TriggerMapKey}.

            SERIALIZATION RULES (ASC.Webhooks.Core/WebhookPublisher.cs)
              1. PropertyNamingPolicy = CamelCase.
              2. DefaultIgnoreCondition = WhenWritingDefault. Every null, false, 0 and
                 default-valued property is OMITTED. Consequently almost nothing here is
                 `required`, and absence never means "unset" -- it means "default".
              3. [JsonIgnore] on the domain type is honoured.
              4. Computed read-only getters ARE serialized. IgnoreReadOnlyProperties is
                 set only on the sender's options, by which point `payload` is an opaque
                 JsonElement, so it never applies to payload bodies.
              5. Every timestamp, in the envelope and in the payload alike, is UTC
                 ISO-8601 in whole seconds with a Z, e.g. 2026-09-17T10:22:56Z.
              6. Entry ids are generic T: int for internal storage, string for
                 third-party providers. Modelled here as oneOf[integer, string].

            TRANSPORT
              POST, Content-Type: application/json; charset=utf-8
              Redirects are not followed. Each request waits {Short(WebhookDelivery.DefaultRequestTimeout)} for a response by
              default (configurable per installation as {WebhookDelivery.RequestTimeoutKey}), so
              receivers must acknowledge fast and process out of band. Any non-2xx
              answer, timeout or network error is a failed attempt; a failed delivery
              is retried after {schedule} (each +-{jitter}% jitter), i.e.
              {intervals.Count + 1} attempts over ~{hours}h by default (ASC.Webhooks.Core/WebhookDelivery.cs,
              configurable per installation as {WebhookDelivery.RetryIntervalsKey}), and then abandoned.
              A portal admin or the subscription's creator can retry it manually from the
              delivery log, which starts a fresh series under the same event.id.

              Headers:
            {Header(WebhookDelivery.SignatureHeader,
                "sha256=<UPPERCASE hex HMAC-SHA256 of the raw",
                "body>. GitHub emits lowercase hex, so compare",
                "case-insensitively. This is the only header",
                "that is authenticated.")}
            {Header(WebhookDelivery.EventIdHeader, "Copy of event.id.")}
            {Header(WebhookDelivery.EventTimestampHeader,
                "Copy of event.createOn, same ISO-8601 UTC",
                "spelling, e.g. 2026-09-17T10:22:56Z.")}

              The last two exist so a receiver can drop an already-seen delivery
              without parsing the body. They are NOT covered by the signature, which is
              computed over the body alone, so anything in transit can rewrite them.
              Reject on them freely -- dropping a delivery is fail-safe, and whoever can
              rewrite a header could drop the request instead -- but never ACCEPT on
              them: after verifying, read event.id from the parsed body, it is the
              authoritative value.

              The signature covers the body only; it does not prove when the request was
              sent. event.createOn is the moment of the event, not of the request: it
              stays the same through the automatic retries (about {hours} hours on the
              default schedule) and through a manual retry at any later time, so a
              freshness window on it, or on the timestamp header, rejects legitimate
              retries. Deduplicate on event.id instead: keep the ids already processed
              and drop a delivery whose id is among them. That is the replay defence.

            """;
    }

    /// <summary>
    /// One entry of the Headers block: the name, then its text from <see cref="HeaderColumn"/> on, starting on the
    /// next line when the name leaves no room for it.
    /// </summary>
    private static string Header(string name, params string[] lines)
    {
        var text = new StringBuilder("    ").Append(name);

        for (var i = 0; i < lines.Length; i++)
        {
            if (i == 0 && name.Length + 2 <= HeaderColumn)
            {
                text.Append(' ', HeaderColumn - name.Length).Append(lines[i]);
            }
            else
            {
                text.Append('\n').Append(' ', HeaderColumn + 4).Append(lines[i]);
            }
        }

        return text.ToString();
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
