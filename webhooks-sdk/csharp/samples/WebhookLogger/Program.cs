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
    // header to begin with.
    Log(await File.ReadAllBytesAsync(file), signature, signature is null ? null : secret);
    return 0;
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

    // Acknowledge first. DocSpace gives a delivery about 31 seconds across all
    // its retries, so anything slow belongs after the response, not before it.
    response.StatusCode = 200;
    response.Close();

    Log(body, context.Request.Headers[WebhookSignature.HeaderName], secret);
}

return 0;

// ------------------------------------------------------------------ logging

static void Log(byte[] body, string? signature, string? secret)
{
    var now = DateTime.Now.ToString("HH:mm:ss");

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
        return;
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
        return;
    }

    var known = hook.IsKnownTrigger ? "" : "  (trigger unknown to this build)";
    Console.WriteLine($"[{now}] {hook.Trigger}{known}");
    Console.WriteLine($"         {signatureNote}");

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
}

// A typed line per payload kind, to show the models actually being used.
static string? Summarize(object? payload) => payload switch
{
    UserPayload u => $"user: {u.UserName} <{u.Email}>  status={u.Status}",
    GroupPayload g => $"group: {g.Name}  id={g.Id}",
    // Files and folders share one payload shape -- the publisher serializes
    // the FileEntry<T> base, so fileEntryType is the only discriminator and
    // no File/Folder-specific field is ever present.
    FileEntryPayload e =>
        $"{(e.FileEntryType == 2 ? "file" : "folder")}: {e.Title}  "
        + $"id={e.Id?.ActualInstance}  parent={e.ParentId?.ActualInstance}",
    FormSubmitPayload s => $"form: {s.SubmittedForm?.Title} "
                           + $"-> original {s.OriginalForm?.Id?.ActualInstance}",
    _ => null,
};

static string Indent(string text) =>
    string.Join(Environment.NewLine,
        text.Split('\n').Select(l => "         " + l.TrimEnd('\r')));

internal static class HttpMethods
{
    internal static bool IsPost(string method) =>
        string.Equals(method, "POST", StringComparison.OrdinalIgnoreCase);
}
