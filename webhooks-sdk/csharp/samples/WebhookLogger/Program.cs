using System.Net;
using System.Text;

using DocSpace.Webhooks.SDK;
using DocSpace.Webhooks.SDK.Model;

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
    return Log(await File.ReadAllBytesAsync(file), signature, signature is null ? null : secret, null) ? 0 : 1;
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

    // Acknowledge first. DocSpace waits 30 seconds for a response and treats a
    // timeout as a failed attempt, so anything slow belongs after the response.
    response.StatusCode = 200;
    response.Close();

    Log(body, context.Request.Headers[WebhookSignature.HeaderName], secret, context.Request.Headers[ProcessedEvents.EventIdHeader]);
}

return 0;

// ------------------------------------------------------------------ logging

// True when the delivery was accepted (logged, or dropped as a duplicate); false
// when the signature was rejected or the body did not parse.
static bool Log(byte[] body, string? signature, string? secret, string? eventIdHeader)
{
    var now = DateTime.Now.ToString("HH:mm:ss");

    // A cheap pre-check before any work: the header is not signed, so it may
    // only ever make us DROP a delivery (which is fail-safe -- whoever can
    // rewrite a header can drop the request anyway), never accept one.
    if (ProcessedEvents.WasSeen(eventIdHeader))
    {
        Console.WriteLine($"[{now}] duplicate of event #{eventIdHeader} (header pre-check), skipped");
        Console.WriteLine(new string('-', 72));
        return true;
    }

    string signatureNote;
    if (string.IsNullOrEmpty(secret))
    {
        signatureNote = "signature UNCHECKED (no secret)";
    }
    else if (WebhookSignature.Verify(body, secret, signature))
    {
        signatureNote = "signature ok";
    }
    else
    {
        Console.WriteLine($"[{now}] REJECTED: bad or missing signature ({body.Length} bytes)");
        Console.WriteLine(new string('-', 72));
        return false;
    }

    DocSpaceWebhook hook;
    try
    {
        hook = WebhookParser.Parse(body);
    }
    catch (WebhookParseException ex)
    {
        Console.WriteLine($"[{now}] unparseable body: {ex.Message}");
        Console.WriteLine(new string('-', 72));
        return false;
    }

    // The model the generated table names for this trigger: "untyped" for the
    // legacy catch-all, "unknown" for a trigger newer than this build.
    var model = WebhookTriggers.PayloadTypes.TryGetValue(hook.Trigger, out var payloadType)
        ? payloadType.Name
        : WebhookTriggers.UntypedTriggers.Contains(hook.Trigger) ? "untyped" : "unknown";

    if (payloadType is not null && hook.Payload is null)
    {
        Console.WriteLine($"[{now}] unparseable body: no {model} payload for '{hook.Trigger}'");
        Console.WriteLine(new string('-', 72));
        return false;
    }

    // The authoritative duplicate check, on the id from the verified body.
    if (hook.Event is { } seen && !ProcessedEvents.Add(seen.Id))
    {
        Console.WriteLine($"[{now}] duplicate of event #{seen.Id}, already processed; skipped");
        Console.WriteLine(new string('-', 72));
        return true;
    }

    var known = hook.IsKnownTrigger ? "" : "  (trigger unknown to this build)";
    Console.WriteLine($"[{now}] {hook.Trigger}{known}");
    Console.WriteLine($"         {signatureNote}");
    Console.WriteLine($"         model: {model}");

    if (hook.Event is { } ev)
    {
        Console.WriteLine($"         event #{ev.Id}  at {ev.CreateOn:u}  by {ev.CreateBy}");
    }

    if (hook.Webhook is { } cfg)
    {
        var retry = cfg.RetryCount > 0 ? $"  retry #{cfg.RetryCount}" : "";
        Console.WriteLine($"         subscription #{cfg.Id} \"{cfg.Name}\"{retry}");
    }

    var summary = Summarize(hook.Payload);
    if (summary is not null)
    {
        Console.WriteLine($"         {summary}");
    }

    Console.WriteLine();
    Console.WriteLine(Indent(hook.RawPayloadJson));
    Console.WriteLine(new string('-', 72));

    return true;
}

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
/// The events already handled. DocSpace delivers at least once: an automatic
/// retry, or a manual retry from the delivery log, sends the same event.id
/// again, and the signature does not prove when a request was sent. So the
/// id is the replay and duplicate defence. This set lives in memory for the
/// sake of the sample; a real receiver persists the ids for as long as an event
/// can still arrive -- the automatic retries span about 18 hours on the default
/// schedule, and a manual retry can come at any time later.
/// </summary>
internal static class ProcessedEvents
{
    internal const string EventIdHeader = "x-docspace-event-id";

    private static readonly HashSet<int> _ids = [];

    internal static bool Add(int id) => _ids.Add(id);

    internal static bool WasSeen(string? header) =>
        int.TryParse(header, out var id) && _ids.Contains(id);
}

internal static class HttpMethods
{
    internal static bool IsPost(string method) =>
        string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
}
