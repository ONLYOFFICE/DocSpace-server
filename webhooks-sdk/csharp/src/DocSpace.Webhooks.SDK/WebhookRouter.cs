using System.Collections.Concurrent;
using System.Text;

using DocSpace.Webhooks.SDK.Model;

namespace DocSpace.Webhooks.SDK;

/// <summary>What <see cref="WebhookRouter.HandleAsync(string, string?, string?, CancellationToken)"/> did with a delivery.</summary>
public enum WebhookOutcome
{
    /// <summary>At least one handler ran, and all of them completed.</summary>
    Handled,

    /// <summary>The event was processed before; nothing ran.</summary>
    Duplicate,

    /// <summary>No handler is registered for the trigger; the delivery is acknowledged all the same.</summary>
    Unhandled,

    /// <summary>The signature is missing or does not match.</summary>
    Rejected,

    /// <summary>The body is not a delivery, or a typed trigger arrived without its payload.</summary>
    Invalid,

    /// <summary>A handler threw; the remaining ones did not run.</summary>
    Failed,
}

/// <summary>
/// The result of one delivery: what happened and the HTTP status to answer DocSpace with. A 2xx acknowledges the
/// delivery; anything else is a failed attempt, which DocSpace retries on its schedule.
/// </summary>
public sealed record WebhookHandleResult(
    WebhookOutcome Outcome,
    int StatusCode,
    string? Trigger = null,
    int? EventId = null,
    string? Error = null,
    Exception? Exception = null);

/// <summary>
/// Remembers which events were processed. DocSpace delivers at least once: an automatic or manual retry sends the
/// same <c>event.id</c> again, and the signature does not prove when a request was sent, so the id is the duplicate
/// and replay defence. A real receiver persists the ids for as long as an event can still arrive -- the automatic
/// retries span about 18 hours on the default schedule, and a manual retry can come at any time later.
/// </summary>
public interface IWebhookEventStore
{
    /// <summary>True when the event was processed before.</summary>
    Task<bool> IsProcessedAsync(int eventId, CancellationToken cancellationToken);

    /// <summary>Records the event as processed; called only after every handler completed.</summary>
    Task MarkProcessedAsync(int eventId, CancellationToken cancellationToken);
}

/// <summary>An <see cref="IWebhookEventStore"/> in process memory: for samples and tests, lost on restart.</summary>
public sealed class InMemoryWebhookEventStore : IWebhookEventStore
{
    private readonly ConcurrentDictionary<int, byte> _ids = new();

    /// <inheritdoc/>
    public Task<bool> IsProcessedAsync(int eventId, CancellationToken cancellationToken) =>
        Task.FromResult(_ids.ContainsKey(eventId));

    /// <inheritdoc/>
    public Task MarkProcessedAsync(int eventId, CancellationToken cancellationToken)
    {
        _ids.TryAdd(eventId, 0);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Dispatches verified deliveries to handlers registered per trigger (<c>OnFileCreated</c>, ...), per delivery type
/// (<see cref="On{TDelivery}(Func{TDelivery, CancellationToken, Task})"/>), or for anything else
/// (<see cref="OnOther"/>). Any number of handlers can be registered for one trigger; each registration returns an
/// <see cref="IDisposable"/> that removes it.
/// </summary>
/// <remarks>
/// <see cref="HandleAsync(string, string?, string?, CancellationToken)"/> checks the signature, drops a duplicate,
/// reads the body into the delivery model of its trigger and awaits every matching handler in registration order:
/// those of its delivery type first, then the trigger's own. Answer DocSpace with the result's
/// <see cref="WebhookHandleResult.StatusCode"/> only after it returns: a handler that throws makes it a failed
/// attempt, and DocSpace sends the delivery again, so handlers have to be idempotent.
/// </remarks>
public sealed partial class WebhookRouter
{
    private readonly string? _secret;
    private readonly IWebhookEventStore? _store;
    private readonly object _sync = new();

    private Dictionary<string, Registration[]> _byTrigger = new(StringComparer.Ordinal);
    private Dictionary<Type, Registration[]> _byType = [];
    private Registration[] _other = [];

    /// <summary>A router that accepts only deliveries signed with <paramref name="secret"/>.</summary>
    /// <param name="secret">The secret key of the subscription.</param>
    /// <param name="store">Where processed event ids are kept; without one, duplicates are not detected.</param>
    public WebhookRouter(string secret, IWebhookEventStore? store = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);

        _secret = secret;
        _store = store;
    }

    private WebhookRouter(IWebhookEventStore? store)
    {
        _store = store;
    }

    /// <summary>
    /// A router that does not check signatures, for replaying saved bodies during development. Never put it behind
    /// a reachable endpoint: anyone could send it a delivery.
    /// </summary>
    public static WebhookRouter ForUnsignedReplay(IWebhookEventStore? store = null) => new(store);

    /// <summary>
    /// Registers <paramref name="handler"/> for <paramref name="trigger"/>, which has to be delivered as
    /// <typeparamref name="TDelivery"/>. The generated <c>On&lt;Trigger&gt;</c> methods call this.
    /// </summary>
    /// <exception cref="ArgumentException">The trigger is not delivered as <typeparamref name="TDelivery"/>.</exception>
    public IDisposable On<TDelivery>(string trigger, Func<TDelivery, CancellationToken, Task> handler)
        where TDelivery : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!WebhookTriggers.DeliveryTypes.TryGetValue(trigger, out var type) || type != typeof(TDelivery))
        {
            throw new ArgumentException(
                $"'{trigger}' is delivered as {type?.Name ?? "an untyped body"}, not {typeof(TDelivery).Name}", nameof(trigger));
        }

        var registration = new Registration((d, _, ct) => handler((TDelivery)d!, ct));

        lock (_sync)
        {
            _byTrigger = With(_byTrigger, trigger, registration);
        }

        return new Unsubscriber(() =>
        {
            lock (_sync)
            {
                _byTrigger = Without(_byTrigger, trigger, registration);
            }
        });
    }

    /// <summary>
    /// Registers <paramref name="handler"/> for every trigger delivered as <typeparamref name="TDelivery"/>:
    /// <c>On&lt;FileDelivery&gt;</c> gets every file event, <c>event.trigger</c> tells which.
    /// </summary>
    /// <exception cref="ArgumentException">No trigger is delivered as <typeparamref name="TDelivery"/>.</exception>
    public IDisposable On<TDelivery>(Func<TDelivery, CancellationToken, Task> handler)
        where TDelivery : class
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!WebhookTriggers.DeliveryTypes.Values.Contains(typeof(TDelivery)))
        {
            throw new ArgumentException($"No trigger is delivered as {typeof(TDelivery).Name}", nameof(handler));
        }

        var registration = new Registration((d, _, ct) => handler((TDelivery)d!, ct));

        lock (_sync)
        {
            _byType = With(_byType, typeof(TDelivery), registration);
        }

        return new Unsubscriber(() =>
        {
            lock (_sync)
            {
                _byType = Without(_byType, typeof(TDelivery), registration);
            }
        });
    }

    /// <summary>
    /// Registers <paramref name="handler"/> for the deliveries no other handler takes: triggers without a
    /// registration, the untyped catch-all <c>*</c>, and triggers added after this SDK was built. It gets the
    /// envelope with the payload as <see cref="WebhookParser.Parse(string)"/> reads it.
    /// </summary>
    public IDisposable OnOther(Func<DocSpaceWebhook, CancellationToken, Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var registration = new Registration((_, hook, ct) => handler(hook, ct));

        lock (_sync)
        {
            _other = [.. _other, registration];
        }

        return new Unsubscriber(() =>
        {
            lock (_sync)
            {
                _other = _other.Where(r => r != registration).ToArray();
            }
        });
    }

    /// <summary>Handles one delivery; see the remarks of <see cref="WebhookRouter"/>.</summary>
    /// <param name="body">The request body, exactly as received.</param>
    /// <param name="signature">The <c>x-docspace-signature-256</c> header.</param>
    /// <param name="eventIdHeader">
    /// The <c>x-docspace-event-id</c> header, if at hand: an already processed id is dropped before any work. The
    /// header is not signed, so it is only ever used to drop a delivery, never to accept one.
    /// </param>
    /// <param name="cancellationToken">Passed on to the handlers and the store.</param>
    public async Task<WebhookHandleResult> HandleAsync(
        string body,
        string? signature,
        string? eventIdHeader = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        if (_store is not null && int.TryParse(eventIdHeader, out var headerId)
            && await _store.IsProcessedAsync(headerId, cancellationToken))
        {
            return new WebhookHandleResult(WebhookOutcome.Duplicate, 200, EventId: headerId);
        }

        if (_secret is not null && !WebhookSignature.Verify(body, _secret, signature))
        {
            return new WebhookHandleResult(WebhookOutcome.Rejected, 401, Error: "signature is missing or does not match");
        }

        DocSpaceWebhook hook;
        object? delivery;
        try
        {
            hook = WebhookParser.Parse(body);
            delivery = WebhookParser.ParseDelivery(body);
        }
        catch (WebhookParseException ex)
        {
            return new WebhookHandleResult(WebhookOutcome.Invalid, 400, Error: ex.Message, Exception: ex);
        }

        var eventId = hook.Event?.Id;

        if (_store is not null && eventId is { } id && await _store.IsProcessedAsync(id, cancellationToken))
        {
            return new WebhookHandleResult(WebhookOutcome.Duplicate, 200, hook.Trigger, id);
        }

        var handlers = GetHandlers(hook.Trigger, delivery);

        foreach (var handler in handlers)
        {
            try
            {
                await handler.Invoke(delivery, hook, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                return new WebhookHandleResult(WebhookOutcome.Failed, 500, hook.Trigger, eventId, ex.Message, ex);
            }
        }

        if (_store is not null && eventId is { } processed)
        {
            await _store.MarkProcessedAsync(processed, cancellationToken);
        }

        return new WebhookHandleResult(
            handlers.Length > 0 ? WebhookOutcome.Handled : WebhookOutcome.Unhandled, 200, hook.Trigger, eventId);
    }

    /// <inheritdoc cref="HandleAsync(string, string?, string?, CancellationToken)"/>
    public Task<WebhookHandleResult> HandleAsync(
        byte[] body,
        string? signature,
        string? eventIdHeader = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(body);

        return HandleAsync(Encoding.UTF8.GetString(body), signature, eventIdHeader, cancellationToken);
    }

    private Registration[] GetHandlers(string trigger, object? delivery)
    {
        // one consistent snapshot: registrations are replaced, never changed in place
        Dictionary<string, Registration[]> byTrigger;
        Dictionary<Type, Registration[]> byType;
        Registration[] other;

        lock (_sync)
        {
            (byTrigger, byType, other) = (_byTrigger, _byType, _other);
        }

        if (delivery is null)
        {
            return other;
        }

        // the general handlers of the delivery type first, then the trigger's own
        var typed = byType.GetValueOrDefault(delivery.GetType(), [])
            .Concat(byTrigger.GetValueOrDefault(trigger, []))
            .ToArray();

        return typed.Length > 0 ? typed : other;
    }

    private static Dictionary<TKey, Registration[]> With<TKey>(
        Dictionary<TKey, Registration[]> source, TKey key, Registration registration) where TKey : notnull
    {
        var copy = new Dictionary<TKey, Registration[]>(source, source.Comparer)
        {
            [key] = [.. source.GetValueOrDefault(key, []), registration]
        };

        return copy;
    }

    private static Dictionary<TKey, Registration[]> Without<TKey>(
        Dictionary<TKey, Registration[]> source, TKey key, Registration registration) where TKey : notnull
    {
        var copy = new Dictionary<TKey, Registration[]>(source, source.Comparer);
        var left = source.GetValueOrDefault(key, []).Where(r => r != registration).ToArray();

        if (left.Length > 0)
        {
            copy[key] = left;
        }
        else
        {
            copy.Remove(key);
        }

        return copy;
    }

    private sealed class Registration(Func<object?, DocSpaceWebhook, CancellationToken, Task> invoke)
    {
        public Task Invoke(object? delivery, DocSpaceWebhook hook, CancellationToken cancellationToken) =>
            invoke(delivery, hook, cancellationToken);
    }

    private sealed class Unsubscriber(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;

        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
