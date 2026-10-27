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
    /// <c>null</c> when the trigger is not one this build knows, or is the
    /// untyped legacy catch-all <c>*</c>. Cast with <see cref="PayloadAs{T}"/>.
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
        var (root, trigger) = ReadEnvelope(body);

        var payloadToken = root["payload"];

        object? payload = null;
        if (payloadToken is not null
            && payloadToken.Type != JTokenType.Null
            && WebhookTriggers.PayloadTypes.TryGetValue(trigger, out var payloadType))
        {
            payload = ToObject(payloadToken, payloadType, $"payload did not match the schema for '{trigger}'");
        }

        return new DocSpaceWebhook
        {
            Trigger = trigger,
            Event = (WebhookEventInfo?)ToObject(root["event"], typeof(WebhookEventInfo), "event did not match the schema"),
            Webhook = (WebhookConfigInfo?)ToObject(root["webhook"], typeof(WebhookConfigInfo), "webhook did not match the schema"),
            Payload = payload,
            RawPayloadJson = payloadToken?.ToString(Formatting.Indented) ?? "null",
        };
    }

    /// <summary>
    /// Parse a delivery body into the model of the whole delivery its trigger
    /// carries -- <see cref="FileDelivery"/> for <c>file.created</c>, and so on,
    /// as <see cref="WebhookTriggers.DeliveryTypes"/> pairs them -- for a
    /// <c>switch</c> on the type.
    /// </summary>
    /// <returns>
    /// The delivery, or <c>null</c> when the trigger has no typed body: the
    /// untyped catch-all <c>*</c>, or a trigger added after this SDK was built.
    /// As with <see cref="Parse(string)"/>, that is not an error.
    /// </returns>
    /// <exception cref="WebhookParseException">The body is not an envelope, or a
    /// typed trigger arrived without its payload object.</exception>
    public static object? ParseDelivery(string body)
    {
        var (root, trigger) = ReadEnvelope(body);

        return WebhookTriggers.DeliveryTypes.TryGetValue(trigger, out var type)
            ? ToDelivery(root, trigger, type)
            : null;
    }

    /// <summary>
    /// Parse a delivery body into <typeparamref name="T"/>, for a handler that
    /// expects one kind of delivery, such as <see cref="FileDelivery"/>.
    /// </summary>
    /// <exception cref="WebhookParseException">The body is not an envelope, its
    /// trigger is not delivered as <typeparamref name="T"/>, or the payload is
    /// missing.</exception>
    public static T ParseDelivery<T>(string body) where T : class
    {
        var (root, trigger) = ReadEnvelope(body);

        if (!WebhookTriggers.DeliveryTypes.TryGetValue(trigger, out var type) || type != typeof(T))
        {
            throw new WebhookParseException(
                $"'{trigger}' is delivered as {type?.Name ?? "an untyped body"}, not {typeof(T).Name}");
        }

        return (T)ToDelivery(root, trigger, type)!;
    }

    /// <inheritdoc cref="ParseDelivery(string)"/>
    public static object? ParseDelivery(ReadOnlySpan<byte> body) =>
        ParseDelivery(Encoding.UTF8.GetString(body));

    /// <inheritdoc cref="ParseDelivery{T}(string)"/>
    public static T ParseDelivery<T>(ReadOnlySpan<byte> body) where T : class =>
        ParseDelivery<T>(Encoding.UTF8.GetString(body));

    private static (JObject Root, string Trigger) ReadEnvelope(string body)
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

        return (root, trigger);
    }

    private static object? ToDelivery(JObject root, string trigger, Type type)
    {
        // a typed trigger always carries its payload object; without it the model
        // would come back with a null payload the caller has no reason to expect
        if (root["payload"] is not JObject)
        {
            throw new WebhookParseException($"no payload object for '{trigger}', delivered as {type.Name}");
        }

        return ToObject(root, type, $"body did not match {type.Name} for '{trigger}'");
    }

    private static object? ToObject(JToken? token, Type type, string message)
    {
        try
        {
            return token?.ToObject(type);
        }
        catch (JsonException ex)
        {
            throw new WebhookParseException(message, ex);
        }
    }

    /// <inheritdoc cref="Parse(string)"/>
    public static DocSpaceWebhook Parse(ReadOnlySpan<byte> body) =>
        Parse(Encoding.UTF8.GetString(body));
}
