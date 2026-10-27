using System.Net;
using System.Text;

using DocSpace.Webhooks.SDK;
using DocSpace.Webhooks.SDK.Model;

using Newtonsoft.Json;

// ---------------------------------------------------------------- arguments

var host = "localhost";
var port = 5555;
var path = "/webhook";
string? secret = Environment.GetEnvironmentVariable("DOCSPACE_WEBHOOK_SECRET");
string? file = null;
string? signature = null;

for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--host": host = args[i + 1]; break;
        case "--port": port = int.Parse(args[i + 1]); break;
        case "--path": path = args[i + 1]; break;
        case "--secret": secret = args[i + 1]; break;
        case "--file": file = args[i + 1]; break;
        case "--signature": signature = args[i + 1]; break;
    }
}

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("""
        Logs DocSpace webhook deliveries to the console.

          --host <h>       host to bind               (default localhost)
                           use "+" to accept any Host header, which is what
                           you need behind nginx unless nginx rewrites it
          --port <n>       port to listen on          (default 5555)
          --path <p>       path to accept POSTs on    (default /webhook)
          --secret <s>     subscription secret key    (or DOCSPACE_WEBHOOK_SECRET)
          --file <path>    log one saved body and exit, instead of listening
          --signature <s>  signature to check --file against (else unchecked)
        """);
    return 0;
}

// ------------------------------------------------------- one-shot file mode

if (file is not null)
{
    // Without a --signature there is nothing to check the body against, so
    // verification is skipped rather than failing a replay that never had a
    // header to begin with. A rejected signature or a body that does not parse
    // exits 1, so a script (the SDK's own verification among them) can tell.
    var replay = CreateRouter(signature is null ? null : secret);
    var replayed = await LogAsync(replay, await File.ReadAllBytesAsync(file), signature, null);

    return replayed.StatusCode is >= 200 and < 300 ? 0 : 1;
}

// ------------------------------------------------------------------ listener

if (!path.StartsWith('/'))
{
    path = "/" + path;
}

var prefix = $"http://{host}:{port}{path.TrimEnd('/')}/";

using var listener = new HttpListener();
listener.Prefixes.Add(prefix);

try
{
    listener.Start();
}
catch (HttpListenerException ex)
{
    Console.Error.WriteLine($"could not listen on {prefix}: {ex.Message}");

    // ERROR_ACCESS_DENIED. http.sys reserves non-localhost prefixes to
    // administrators unless the URL has been registered for this account.
    if (ex.ErrorCode == 5)
    {
        Console.Error.WriteLine();
        Console.Error.WriteLine("Binding anything other than localhost needs a one-off URL reservation.");
        Console.Error.WriteLine("Either run this elevated, or register it once as administrator:");
        Console.Error.WriteLine($@"  netsh http add urlacl url={prefix} user=%USERDOMAIN%\%USERNAME%");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Or leave --host alone and make nginx send a matching Host header:");
        Console.Error.WriteLine($"  proxy_set_header Host localhost;");
    }

    return 1;
}

Console.WriteLine($"listening on {prefix}");

if (string.IsNullOrEmpty(secret))
{
    Console.WriteLine("!! no secret given -- signatures will NOT be checked.");
    Console.WriteLine("!! pass --secret or set DOCSPACE_WEBHOOK_SECRET.");
}

// DocSpace validates the target URL before storing a subscription and rejects
// anything resolving into a private range, so a localhost URL cannot be
// registered directly -- put a tunnel or reverse proxy in front of this, or
// replay a captured body with --file.
Console.WriteLine("note: DocSpace refuses to register private/loopback URLs;");
Console.WriteLine("      expose this through a tunnel, or replay with --file.");

// http.sys routes by the Host header, not just the port: a request arriving
// with any other Host is answered 400 "Invalid Hostname" before this process
// sees it.
if (host is "localhost")
{
    Console.WriteLine("note: bound to Host 'localhost' only. Behind a reverse proxy either");
    Console.WriteLine("      add 'proxy_set_header Host localhost;', or rerun with --host +.");
}
Console.WriteLine(new string('-', 72));

var router = CreateRouter(secret);

using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); listener.Stop(); };

while (!cancel.IsCancellationRequested)
{
    HttpListenerContext context;
    try
    {
        context = await listener.GetContextAsync();
    }
    catch (Exception) when (cancel.IsCancellationRequested)
    {
        break;
    }

    using var response = context.Response;

    if (!HttpMethods.IsPost(context.Request.HttpMethod))
    {
        response.StatusCode = 200;
        continue;
    }

    using var ms = new MemoryStream();
    await context.Request.InputStream.CopyToAsync(ms);
    var body = ms.ToArray();

    // Answer only once the handlers are done: a 2xx tells DocSpace the delivery is
    // handled, anything else makes it retry. It waits 30 seconds for the answer and
    // treats a timeout as a failed attempt, so a slow handler belongs in a queue.
    var result = await LogAsync(
        router, body, context.Request.Headers[WebhookSignature.HeaderName], context.Request.Headers[Receiver.EventIdHeader]);

    response.StatusCode = result.StatusCode;
    response.Close();
}

return 0;

// ------------------------------------------------------------------ logging

// The router this sample hands every delivery to. On<TDelivery> takes every
// trigger delivered as that type; OnFileCreated, one of the generated
// On<Trigger> methods, adds a handler for one trigger next to it; OnOther takes
// what has no handler: the catch-all and triggers newer than this SDK.
static WebhookRouter CreateRouter(string? secret)
{
    var router = string.IsNullOrEmpty(secret)
        ? WebhookRouter.ForUnsignedReplay(Receiver.Store)
        : new WebhookRouter(secret, Receiver.Store);

    var signatureNote = string.IsNullOrEmpty(secret) ? "signature NOT checked (no secret)" : "signature ok";

    router.On<UserDelivery>((d, _) => Print(d.Event, d.Webhook, d.Payload, nameof(UserDelivery), signatureNote));
    router.On<GroupDelivery>((d, _) => Print(d.Event, d.Webhook, d.Payload, nameof(GroupDelivery), signatureNote));
    router.On<FileDelivery>((d, _) => Print(d.Event, d.Webhook, d.Payload, nameof(FileDelivery), signatureNote));
    router.On<FolderDelivery>((d, _) => Print(d.Event, d.Webhook, d.Payload, nameof(FolderDelivery), signatureNote));
    router.On<RoomDelivery>((d, _) => Print(d.Event, d.Webhook, d.Payload, nameof(RoomDelivery), signatureNote));
    router.On<FormSubmitDelivery>((d, _) => Print(d.Event, d.Webhook, d.Payload, nameof(FormSubmitDelivery), signatureNote));

    router.OnFileCreated((d, _) =>
    {
        Console.WriteLine($"         OnFileCreated: \"{d.Payload?.Title}\" in folder {d.Payload?.ParentId?.ActualInstance}");
        return Task.CompletedTask;
    });

    router.OnOther((hook, _) =>
    {
        var known = hook.IsKnownTrigger ? "" : "  (trigger unknown to this build)";
        Console.WriteLine($"[{Now()}] {hook.Trigger}{known}");
        Console.WriteLine($"         {signatureNote}");
        Console.WriteLine($"         model: {ModelName(hook.Trigger)}");
        Console.WriteLine("         delivery: none");
        PrintEnvelope(hook.Event, hook.Webhook);
        Console.WriteLine();
        Console.WriteLine(Indent(hook.RawPayloadJson));
        return Task.CompletedTask;
    });

    return router;
}

// Hands the delivery to the router and logs what it did; the result carries the
// status to answer DocSpace with.
static async Task<WebhookHandleResult> LogAsync(WebhookRouter router, byte[] body, string? signature, string? eventIdHeader)
{
    var result = await router.HandleAsync(body, signature, eventIdHeader);

    switch (result.Outcome)
    {
        case WebhookOutcome.Rejected:
            Console.WriteLine($"[{Now()}] REJECTED: bad or missing signature ({body.Length} bytes)");
            break;
        case WebhookOutcome.Invalid:
            Console.WriteLine($"[{Now()}] unparseable body: {result.Error}");
            break;
        case WebhookOutcome.Failed:
            Console.WriteLine($"[{Now()}] a handler failed: {result.Error}");
            break;
        case WebhookOutcome.Duplicate:
            Console.WriteLine($"[{Now()}] duplicate of event #{result.EventId}, already processed; skipped");
            break;
    }

    Console.WriteLine(new string('-', 72));

    return result;
}

static Task Print(WebhookEventInfo? ev, WebhookConfigInfo? cfg, object? payload, string delivery, string signatureNote)
{
    var trigger = ev?.Trigger ?? "";

    Console.WriteLine($"[{Now()}] {trigger}");
    Console.WriteLine($"         {signatureNote}");
    Console.WriteLine($"         model: {ModelName(trigger)}");
    Console.WriteLine($"         delivery: {delivery}");
    PrintEnvelope(ev, cfg);

    var summary = Summarize(payload);
    if (summary is not null)
    {
        Console.WriteLine($"         {summary}");
    }

    Console.WriteLine();
    Console.WriteLine(Indent(JsonConvert.SerializeObject(payload, Formatting.Indented)));

    return Task.CompletedTask;
}

static void PrintEnvelope(WebhookEventInfo? ev, WebhookConfigInfo? cfg)
{
    if (ev is not null)
    {
        Console.WriteLine($"         event #{ev.Id}  at {ev.CreateOn:u}  by {ev.CreateBy}");
    }

    if (cfg is not null)
    {
        var retry = cfg.RetryCount > 0 ? $"  retry #{cfg.RetryCount}" : "";
        Console.WriteLine($"         subscription #{cfg.Id} \"{cfg.Name}\"{retry}");
    }
}

// The model the generated table names for this trigger: "untyped" for the
// legacy catch-all, "unknown" for a trigger newer than this build.
static string ModelName(string trigger) =>
    WebhookTriggers.PayloadTypes.TryGetValue(trigger, out var payloadType)
        ? payloadType.Name
        : WebhookTriggers.UntypedTriggers.Contains(trigger) ? "untyped" : "unknown";

static string Now() => DateTime.Now.ToString("HH:mm:ss");

// A typed line per payload kind, to show the models actually being used.
static string? Summarize(object? payload) => payload switch
{
    UserPayload u => $"user: {u.UserName} <{u.Email}>  status={u.Status}",
    GroupPayload g => $"group: {g.Name}  id={g.Id}",
    // Files, folders and rooms each have their own schema. The generator
    // flattens allOf, so these are unrelated classes -- there is no
    // FileEntryPayload case that would catch all three.
    FilePayload f =>
        $"file: {f.Title}  id={f.Id?.ActualInstance}  "
        + $"v{f.VarVersion}  {f.ContentLength} bytes",
    FolderPayload d =>
        $"folder: {d.Title}  id={d.Id?.ActualInstance}  "
        + $"{d.FilesCount} files / {d.FoldersCount} folders",
    RoomPayload r =>
        $"room: {r.Title}  id={r.Id?.ActualInstance}  "
        + $"roomType={r.RoomType}",
    FormSubmitPayload s => $"form: {s.SubmittedForm?.Title} "
                           + $"-> original {s.OriginalForm?.Id?.ActualInstance}",
    _ => null,
};

static string Indent(string text) =>
    string.Join(Environment.NewLine,
        text.Split('\n').Select(l => "         " + l.TrimEnd('\r')));

/// <summary>
/// The events already handled, for the router's duplicate check. DocSpace
/// delivers at least once: an automatic retry, or a manual retry from the
/// delivery log, sends the same event.id again. The store lives in memory for
/// the sake of the sample; a real receiver persists the ids for as long as an
/// event can still arrive (see IWebhookEventStore).
/// </summary>
internal static class Receiver
{
    internal const string EventIdHeader = "x-docspace-event-id";

    internal static readonly InMemoryWebhookEventStore Store = new();
}

internal static class HttpMethods
{
    internal static bool IsPost(string method) =>
        string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
}
