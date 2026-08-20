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

namespace ASC.Notify.Tests.Infrastructure;

/// <summary>
/// The MailPit instance that the Aspire AppHost starts for local development
/// (<c>ConnectionStringManager.AddMailPit</c>, present in the <c>development</c>, <c>test</c> and
/// <c>frontend-dev</c> launch profiles). Aspire publishes the container's SMTP (1025) and web (8025)
/// ports on random host ports, so they are discovered at run time instead of being hard-coded.
/// </summary>
internal sealed record MailPitEndpoint(string SmtpHost, int SmtpPort, Uri WebUi)
{
    /// <summary>Environment overrides for CI or a non-Aspire MailPit: <c>host:port</c> and a base URL.</summary>
    private const string SmtpEnvVariable = "MAILPIT_SMTP";
    private const string WebUiEnvVariable = "MAILPIT_HTTP";

    private const int ContainerSmtpPort = 1025;
    private const int ContainerWebPort = 8025;

    /// <summary>
    /// Returns the first candidate whose web API answers, or <c>null</c> when MailPit is not running.
    /// Candidates, in order: the environment overrides, the published ports of the running MailPit
    /// container (via <c>docker ps</c>), and finally MailPit's own defaults on localhost.
    /// </summary>
    public static async Task<MailPitEndpoint?> ResolveAsync(CancellationToken cancellationToken)
    {
        foreach (var candidate in GetCandidates())
        {
            if (await candidate.IsAliveAsync(cancellationToken))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<MailPitEndpoint> GetCandidates()
    {
        var fromEnvironment = FromEnvironment();
        if (fromEnvironment != null)
        {
            yield return fromEnvironment;
        }

        var fromDocker = FromDocker();
        if (fromDocker != null)
        {
            yield return fromDocker;
        }

        yield return new MailPitEndpoint("localhost", ContainerSmtpPort, new Uri($"http://localhost:{ContainerWebPort}"));
    }

    private static MailPitEndpoint? FromEnvironment()
    {
        var smtp = Environment.GetEnvironmentVariable(SmtpEnvVariable);
        var webUi = Environment.GetEnvironmentVariable(WebUiEnvVariable);

        if (string.IsNullOrEmpty(smtp) || string.IsNullOrEmpty(webUi))
        {
            return null;
        }

        var parts = smtp.Split(':');

        if (parts.Length != 2 || !int.TryParse(parts[1], out var port) || !Uri.TryCreate(webUi, UriKind.Absolute, out var webUiUri))
        {
            return null;
        }

        return new MailPitEndpoint(parts[0], port, webUiUri);
    }

    /// <summary>
    /// Reads the host ports Aspire published for the MailPit container. Any failure (no docker on
    /// PATH, docker not running, container gone) simply drops this candidate.
    /// </summary>
    private static MailPitEndpoint? FromDocker()
    {
        string output;

        try
        {
            var startInfo = new ProcessStartInfo("docker")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("ps");
            startInfo.ArgumentList.Add("--filter");
            startInfo.ArgumentList.Add("name=mailpit");
            startInfo.ArgumentList.Add("--format");
            startInfo.ArgumentList.Add("{{.Ports}}");

            using var process = Process.Start(startInfo);

            if (process == null)
            {
                return null;
            }

            output = process.StandardOutput.ReadToEnd();

            if (!process.WaitForExit(TimeSpan.FromSeconds(10)) || process.ExitCode != 0)
            {
                return null;
            }
        }
        catch (Exception)
        {
            return null;
        }

        var smtpPort = MatchPublishedPort(output, ContainerSmtpPort);
        var webPort = MatchPublishedPort(output, ContainerWebPort);

        if (smtpPort == null || webPort == null)
        {
            return null;
        }

        return new MailPitEndpoint("localhost", smtpPort.Value, new Uri($"http://localhost:{webPort}"));
    }

    /// <summary>Picks the host port out of a <c>docker ps</c> mapping such as <c>0.0.0.0:56162-&gt;8025/tcp</c>.</summary>
    private static int? MatchPublishedPort(string dockerPorts, int containerPort)
    {
        var match = Regex.Match(dockerPorts, $@":(?<port>\d+)->{containerPort}/tcp", RegexOptions.None, TimeSpan.FromSeconds(1));

        return match.Success && int.TryParse(match.Groups["port"].Value, out var port) ? port : null;
    }

    private async Task<bool> IsAliveAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient { BaseAddress = WebUi, Timeout = TimeSpan.FromSeconds(3) };
            using var response = await client.GetAsync("api/v1/info", cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            return false;
        }
    }
}

/// <summary>
/// Delivers a rendered letter to MailPit over SMTP and reads it back through MailPit's web API, so a
/// test can both drop a letter into the inbox for a human to look at and assert that it arrived.
/// </summary>
internal sealed class MailPitInbox(MailPitEndpoint endpoint)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("ONLYOFFICE letter preview", "noreply@onlyoffice.com"));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(endpoint.SmtpHost, endpoint.SmtpPort, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    /// <summary>Polls the inbox until a message addressed to <paramref name="toAddress"/> shows up.</summary>
    public async Task<MailPitMessage?> WaitForMessageAsync(string toAddress, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { BaseAddress = endpoint.WebUi, Timeout = TimeSpan.FromSeconds(10) };

        var deadline = DateTime.UtcNow + timeout;

        do
        {
            var body = await client.GetStringAsync("api/v1/messages?limit=100", cancellationToken);
            var messages = JsonSerializer.Deserialize<MailPitMessages>(body, _jsonOptions)?.Messages ?? [];

            var found = messages.FirstOrDefault(m => m.To != null
                && m.To.Any(t => string.Equals(t.Address, toAddress, StringComparison.OrdinalIgnoreCase)));

            if (found != null)
            {
                return found;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
        while (DateTime.UtcNow < deadline);

        return null;
    }

    public Uri GetMessageUrl(MailPitMessage message)
    {
        return new Uri(endpoint.WebUi, $"view/{message.Id}");
    }
}

internal sealed record MailPitMessages([property: JsonPropertyName("messages")] List<MailPitMessage>? Messages);

internal sealed record MailPitMessage(
    [property: JsonPropertyName("ID")] string Id,
    [property: JsonPropertyName("Subject")] string Subject,
    [property: JsonPropertyName("To")] List<MailPitAddress>? To);

internal sealed record MailPitAddress([property: JsonPropertyName("Address")] string Address);
