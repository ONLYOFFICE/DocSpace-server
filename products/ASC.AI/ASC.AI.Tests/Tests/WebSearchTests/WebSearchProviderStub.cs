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

namespace ASC.AI.Tests.Tests.WebSearchTests;

/// <summary>
/// A web-search provider standing in for the real one, run in the test process: it answers
/// <c>POST {baseUrl}/search</c> and <c>POST {baseUrl}/contents</c> the way an ONLYOFFICE-shaped provider does and
/// records what it was sent. The chat service dials it because the integration-test profile sets
/// <c>AI__ALLOW_PRIVATE_BASE_URL</c>.
/// </summary>
/// <remarks>
/// The provider name a test stores decides who dials it. A config named <c>onlyoffice</c> is rewritten by the chat
/// service to the portal's own AI gateway whenever the caller's headers are forwarded (passthrough, editor tools,
/// chat), which a standalone portal does not have; any other non-Exa name keeps the stored <c>baseUrl</c>, so the
/// passthrough tests store <see cref="WebSearchTestBase.StubProvider"/>. The connection probe has no such rewrite and
/// is the one place <c>onlyoffice</c> is dialled as stored.
/// </remarks>
public sealed class WebSearchProviderStub : IDisposable
{
    public const string SearchPath = "/v1/search";
    public const string ContentsPath = "/v1/contents";

    private const string DefaultSearchBody = """{"requestId":"stub-search-1","results":[{"title":"Stub result","url":"https://example.com/stub","text":"Stub text"}]}""";
    private const string DefaultContentsBody = """{"requestId":"stub-contents-1","results":[{"title":"Stub page","url":"https://example.com/stub","text":"Stub page text"}]}""";

    private readonly WireMockServer _server = WireMockServer.Start();
    private int _priority = 1000;

    public WebSearchProviderStub()
    {
        RespondTo(SearchPath, 200, DefaultSearchBody);
        RespondTo(ContentsPath, 200, DefaultContentsBody);
    }

    /// <summary>One request the provider received.</summary>
    public sealed record Call(string Path, string Body, string? Authorization);

    /// <summary>The <c>baseUrl</c> a config is given to reach this provider.</summary>
    public string BaseUrl => $"http://127.0.0.1:{_server.Port}/v1";

    /// <summary>A base URL nothing listens on: connecting is refused at once.</summary>
    public static string DeadBaseUrl => "http://127.0.0.1:1/v1";

    /// <summary>Replaces what the provider answers on a path; the latest call wins.</summary>
    public void RespondTo(string path, int status, string body, string contentType = "application/json")
    {
        _server
            .Given(Request.Create().WithPath(path).UsingPost())
            .AtPriority(--_priority)
            .RespondWith(Response.Create().WithStatusCode(status).WithHeader("Content-Type", contentType).WithBody(body));
    }

    /// <summary>The requests received on a path, oldest first.</summary>
    public IReadOnlyList<Call> CallsTo(string path) =>
        _server.LogEntries
            .Where(e => e.RequestMessage?.Path == path)
            .Select(e => new Call(
                path,
                e.RequestMessage!.Body ?? string.Empty,
                e.RequestMessage.Headers?
                    .FirstOrDefault(h => string.Equals(h.Key, "Authorization", StringComparison.OrdinalIgnoreCase)).Value?
                    .FirstOrDefault()))
            .ToList();

    /// <summary>Every request the provider saw, for an assertion message.</summary>
    public string Describe() =>
        "provider saw:\n" + string.Join("\n", _server.LogEntries.Select(e =>
            $"  {e.RequestMessage?.Method} {e.RequestMessage?.Path} -> {e.ResponseMessage?.StatusCode}"));

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
