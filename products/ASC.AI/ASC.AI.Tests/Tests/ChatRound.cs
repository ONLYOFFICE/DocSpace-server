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

namespace ASC.AI.Tests.Tests;

/// <summary>
/// One chat round as the client receives it from <c>ai/send-with-stream</c>: the HTTP status, the raw body and the
/// ndjson frames. Every frame carries a <c>type</c>; a failure the stream reports shows up as a frame of type
/// <c>error</c> on an HTTP 200.
/// </summary>
public sealed record ChatRound(HttpStatusCode StatusCode, string Raw, IReadOnlyList<JsonElement> Frames)
{
    public static ChatRound Parse(HttpStatusCode statusCode, string raw)
    {
        var frames = new List<JsonElement>();

        foreach (var line in raw.Split('\n'))
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith('{'))
            {
                continue;
            }

            try
            {
                frames.Add(JsonDocument.Parse(trimmed).RootElement.Clone());
            }
            catch (JsonException)
            {
                // A partial line of a cut stream: not a frame.
            }
        }

        return new ChatRound(statusCode, raw, frames);
    }

    /// <summary>The frames of one <c>type</c>.</summary>
    public IEnumerable<JsonElement> OfType(string type) =>
        Frames.Where(f => f.TryGetProperty("type", out var t) && t.GetString() == type);

    /// <summary>The message of the first <c>error</c> frame, or null when the stream reported none.</summary>
    public string? StreamError =>
        OfType("error").Select(f => f.TryGetProperty("message", out var m) ? m.ToString() : f.ToString()).FirstOrDefault();

    /// <summary>
    /// The status of a reply that did not finish (a <c>message-incomplete</c> frame): <c>reason</c> is <c>error</c>
    /// for a provider failure, <c>cancelled</c> for a stop; <c>error.message</c> carries the provider's text. Null
    /// when the reply finished.
    /// </summary>
    public JsonElement? IncompleteStatus =>
        OfType("message-incomplete")
            .Select(f => f.TryGetProperty("message", out var m) && m.TryGetProperty("status", out var s) ? s : (JsonElement?)null)
            .FirstOrDefault(s => s is not null);

    /// <summary>The text the assistant reply ended with (the last <c>message-end</c> or <c>message-delta</c>).</summary>
    public string? AssistantText =>
        Frames.Where(f => f.TryGetProperty("type", out var t) && t.GetString() is "message-end" or "message-delta")
            .Select(TextOfFrame)
            .LastOrDefault(t => t is not null);

    /// <summary>The text of one frame's message: a plain string, or its text blocks joined.</summary>
    public static string? TextOfFrame(JsonElement frame)
    {
        if (!frame.TryGetProperty("message", out var message) || !message.TryGetProperty("content", out var content))
        {
            return null;
        }

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString(),
            JsonValueKind.Array => string.Concat(content.EnumerateArray()
                .Where(b => b.TryGetProperty("type", out var t) && t.GetString() == "text")
                .Select(b => b.TryGetProperty("text", out var x) ? x.GetString() : null)),
            _ => null
        };
    }

    /// <summary>The id of the thread the round ran in, from the first frame that names one.</summary>
    public string? ThreadId =>
        Frames.Select(f => f.TryGetProperty("threadId", out var t) ? t.GetString() : null).FirstOrDefault(t => t is not null);

    /// <summary>
    /// Asserts a round that really answered: HTTP 200, no error frame, no incomplete reply, and some text. A refused
    /// inference still ends in a stored assistant message (with an error status), so "an assistant message exists"
    /// is not enough.
    /// </summary>
    public void ShouldBeHealthy()
    {
        StatusCode.Should().Be(HttpStatusCode.OK, ToString());
        StreamError.Should().BeNull(ToString());
        IncompleteStatus.Should().BeNull(ToString());
        AssistantText.Should().NotBeNullOrEmpty(ToString());
    }

    /// <summary>The raw stream, for an assertion message.</summary>
    public override string ToString() => $"{(int)StatusCode}: {Raw}";
}
