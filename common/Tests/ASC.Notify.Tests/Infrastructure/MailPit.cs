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
/// Delivers a rendered letter to MailPit over SMTP and reads it back through MailPit's web API, so a
/// test can drop a letter into the inbox for a human to look at, assert that it arrived, and ask
/// MailPit how well real mail clients would cope with its markup.
///
/// Both endpoints come from <see cref="MailPitFixture"/>, i.e. from the Aspire host the test run
/// started — nothing here discovers or guesses where MailPit is.
/// </summary>
/// <param name="smtpHost">Host of the container's SMTP endpoint.</param>
/// <param name="smtpPort">Port of the container's SMTP endpoint.</param>
/// <param name="api">Client bound to the web API, shared by every test — <see cref="HttpClient"/> is
/// thread-safe, and test classes run in parallel.</param>
internal sealed class MailPitInbox(string smtpHost, int smtpPort, HttpClient api) : IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// HTML checks run one at a time, and this is not a tuning choice. MailPit loads its caniemail
    /// support matrix lazily on the first check, guarded by nothing but a "have I loaded it yet"
    /// field (<c>internal/htmlcheck/caniemail.go</c>). Two checks arriving together both start
    /// filling the same map and the Go runtime kills the process outright — the container dies with
    /// <c>fatal error: concurrent map writes</c> and every later test fails against a corpse.
    /// </summary>
    private readonly SemaphoreSlim _htmlCheck = new(1, 1);

    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("ONLYOFFICE letter preview", "noreply@onlyoffice.com"));
        message.To.Add(MailboxAddress.Parse(toAddress));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        // A client per letter: MailKit's SmtpClient is not thread-safe and the tests are parallel.
        // MailPit itself takes the whole assembly delivering at once without complaint, so deliveries
        // are deliberately not throttled — only the HTML check below has to be.
        using var client = new SmtpClient();

        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    /// <summary>
    /// Polls until the message addressed to <paramref name="toAddress"/> shows up. The lookup goes
    /// through the search API rather than a page of the inbox: a full sweep delivers a couple of
    /// thousand letters from parallel test classes, so by the time this runs the letter it is waiting
    /// for is nowhere near the newest page.
    /// </summary>
    public async Task<MailPitMessage?> WaitForMessageAsync(string toAddress, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var query = Uri.EscapeDataString($"to:{toAddress}");
        var deadline = DateTime.UtcNow + timeout;

        do
        {
            var found = await TryFindAsync(query, cancellationToken);

            if (found != null)
            {
                return found;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
        while (DateTime.UtcNow < deadline);

        return null;
    }

    /// <summary>
    /// MailPit's HTML check: it walks the letter's markup and scores every construct against the
    /// caniemail support matrix, returning the share of mail clients that would render it
    /// (<see cref="HtmlCheckTotal.Supported"/>), render it partially, or not render it at all.
    /// </summary>
    public async Task<HtmlCheck> CheckHtmlAsync(string messageId, CancellationToken cancellationToken)
    {
        await _htmlCheck.WaitAsync(cancellationToken);

        try
        {
            var body = await api.GetStringAsync($"api/v1/message/{messageId}/html-check", cancellationToken);

            return JsonSerializer.Deserialize<HtmlCheck>(body, _jsonOptions)
                ?? throw new InvalidOperationException($"MailPit returned no HTML check for message '{messageId}'.");
        }
        finally
        {
            _htmlCheck.Release();
        }
    }

    public Uri GetMessageUrl(MailPitMessage message)
    {
        return new Uri(api.BaseAddress!, $"view/{message.Id}");
    }

    /// <summary>Releases the gate. The client belongs to the fixture, which disposes it itself.</summary>
    public void Dispose()
    {
        _htmlCheck.Dispose();
    }

    /// <summary>
    /// One search attempt. A request that fails outright counts as "not delivered yet": under a
    /// parallel sweep the busy container occasionally cuts one short, and it is the caller's deadline
    /// that decides the verdict, not a single unlucky response.
    /// </summary>
    private async Task<MailPitMessage?> TryFindAsync(string query, CancellationToken cancellationToken)
    {
        try
        {
            var body = await api.GetStringAsync($"api/v1/search?query={query}&limit=1", cancellationToken);

            return JsonSerializer.Deserialize<MailPitMessages>(body, _jsonOptions)?.Messages?.FirstOrDefault();
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }
}

internal sealed record MailPitMessages([property: JsonPropertyName("messages")] List<MailPitMessage>? Messages);

internal sealed record MailPitMessage(
    [property: JsonPropertyName("ID")] string Id,
    [property: JsonPropertyName("Subject")] string Subject,
    [property: JsonPropertyName("To")] List<MailPitAddress>? To);

internal sealed record MailPitAddress([property: JsonPropertyName("Address")] string Address);

/// <summary>The <c>html-check</c> response.</summary>
internal sealed record HtmlCheck(
    [property: JsonPropertyName("Total")] HtmlCheckTotal Total,
    [property: JsonPropertyName("Warnings")] List<HtmlCheckWarning>? Warnings);

/// <summary>
/// The verdict on the letter as a whole. The three shares are percentages and add up to 100:
/// <see cref="Supported"/> is what MailPit reports as the letter's compatibility.
/// </summary>
internal sealed record HtmlCheckTotal(
    [property: JsonPropertyName("Tests")] int Tests,
    [property: JsonPropertyName("Nodes")] int Nodes,
    [property: JsonPropertyName("Supported")] double Supported,
    [property: JsonPropertyName("Partial")] double Partial,
    [property: JsonPropertyName("Unsupported")] double Unsupported);

/// <summary>One construct the letter uses that some mail clients do not handle.</summary>
internal sealed record HtmlCheckWarning(
    [property: JsonPropertyName("Slug")] string Slug,
    [property: JsonPropertyName("Title")] string Title,
    [property: JsonPropertyName("Category")] string Category,
    [property: JsonPropertyName("Score")] HtmlCheckScore Score);

/// <summary>
/// How that construct fares: the same three percentages, plus <see cref="Found"/> — how many nodes of
/// the letter use it, which is what decides how much the construct drags the total down.
/// </summary>
internal sealed record HtmlCheckScore(
    [property: JsonPropertyName("Found")] int Found,
    [property: JsonPropertyName("Supported")] double Supported,
    [property: JsonPropertyName("Partial")] double Partial,
    [property: JsonPropertyName("Unsupported")] double Unsupported);
