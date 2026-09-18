using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using DocSpace.Webhooks.SDK.Model;

namespace DocSpace.Webhooks.SDK;

/// <summary>Thrown when a body is not a DocSpace webhook envelope.</summary>
public sealed class WebhookParseException : Exception
{
    public WebhookParseException(string message, Exception? inner = null)
        : base(message, inner) { }
}

/// <summary>One delivery, with its payload typed where the trigger is known.</summary>
public sealed class DocSpaceWebhook
{
    /// <summary>e.g. <c>file.created</c>.</summary>
    public required string Trigger { get; init; }

    public WebhookEventInfo? Event { get; init; }

    public WebhookConfigInfo? Webhook { get; init; }

    /// <summary>
    /// The payload, deserialized to the type this trigger carries -- or
    /// <c>null</c> when the trigger is not one this build knows. Cast with
    /// <see cref="PayloadAs{T}"/>.
    /// </summary>
    public object? Payload { get; init; }

    /// <summary>The payload exactly as received, always populated.</summary>
    public required string RawPayloadJson { get; init; }

    /// <summary>
    /// False when DocSpace sent a trigger added after this SDK was built. The
    /// payload is then available only through <see cref="RawPayloadJson"/>.
    /// </summary>
    public bool IsKnownTrigger => WebhookTriggers.IsKnown(Trigger);

    /// <summary>The payload as <typeparamref name="T"/>, or null if it is not one.</summary>
    public T? PayloadAs<T>() where T : class => Payload as T;
}

/// <summary>Turns a verified request body into a <see cref="DocSpaceWebhook"/>.</summary>
public static class WebhookParser
{
    /// <summary>
    /// Parse a delivery body.
    /// </summary>
    /// <remarks>
    /// Call this only on a body that has already passed
    /// <see cref="WebhookSignature.Verify(string, string, string?)"/> --
    /// parsing first and verifying afterwards defeats the signature.
    ///
    /// An unrecognised trigger is deliberately not an error: DocSpace gains
    /// triggers over time, and a receiver that throws on an unfamiliar one
    /// breaks the moment the server is upgraded. Check
    /// <see cref="DocSpaceWebhook.IsKnownTrigger"/> if you need to tell.
    /// </remarks>
    public static DocSpaceWebhook Parse(string body)
    {
        ArgumentNullException.ThrowIfNull(body);

        JObject root;
        try
        {
            root = JObject.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new WebhookParseException("body is not a JSON object", ex);
        }

        var trigger = root["event"]?["trigger"]?.Value<string>();
        if (string.IsNullOrEmpty(trigger))
        {
            throw new WebhookParseException("envelope has no event.trigger");
        }

        var payloadToken = root["payload"];

        object? payload = null;
        if (payloadToken is not null
            && payloadToken.Type != JTokenType.Null
            && WebhookTriggers.PayloadTypes.TryGetValue(trigger, out var payloadType))
        {
            try
            {
                payload = payloadToken.ToObject(payloadType);
            }
            catch (JsonException ex)
            {
                throw new WebhookParseException(
                    $"payload did not match the schema for '{trigger}'", ex);
            }
        }

        return new DocSpaceWebhook
        {
            Trigger = trigger,
            Event = root["event"]?.ToObject<WebhookEventInfo>(),
            Webhook = root["webhook"]?.ToObject<WebhookConfigInfo>(),
            Payload = payload,
            RawPayloadJson = payloadToken?.ToString(Formatting.Indented) ?? "null",
        };
    }

    /// <inheritdoc cref="Parse(string)"/>
    public static DocSpaceWebhook Parse(ReadOnlySpan<byte> body) =>
        Parse(Encoding.UTF8.GetString(body));
}
