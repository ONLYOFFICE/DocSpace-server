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
/// "Search can be switched on and off, and switching it off takes its tools out of the list": the model is offered
/// <c>web_search</c> and <c>web_crawling</c> exactly while the portal has a configuration.
/// </summary>
/// <remarks>
/// The TypeScript test asks a real model a question and reads the sources of its answer. Here the model is the stub,
/// and the claim is read from what the chat service sent it. Running the tool is not ported: a config named
/// <c>onlyoffice</c> is routed through the portal's AI gateway for the chat (a standalone portal has none), and any
/// other name makes the engine echo the arguments instead of dialling the provider, so a search result cannot be
/// produced on this stand.
/// </remarks>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/WebSearch")]
public class WebSearchChatToolTests(AspireAppFixture fixture) : WebSearchTestBase(fixture)
{
    [Fact]
    public async Task WebSearchTools_AreOfferedToTheModelOnlyWhileAConfigurationExists()
    {
        using var model = new StubModelProvider();
        using var stub = new WebSearchProviderStub();
        var profile = await CreateProfileOnAsync(model);
        var thread = await CreateThreadAsync(profileId: profile.Id);

        var without = await AskAndReadToolsAsync(model, thread.ThreadId, profile.Id);
        await StoreStubAsync(stub);
        var with = await AskAndReadToolsAsync(model, thread.ThreadId, profile.Id);
        var cleared = await ClearAsync();
        var after = await AskAndReadToolsAsync(model, thread.ThreadId, profile.Id);

        cleared.Status.Should().Be(200, cleared.ToString());
        with.Should().Contain("web_search").And.Contain("web_crawling", "the positive control: the tools are there when configured");
        without.Should().NotContain("web_search", "no provider is configured");
        after.Should().NotContain("web_search", "switching it off takes the tools away");
    }

    /// <summary>Sends one message and returns the request body of the model call it caused.</summary>
    private async Task<string> AskAndReadToolsAsync(StubModelProvider model, string threadId, string profileId)
    {
        var seen = model.CompletionRequestBodies.Count();

        var round = await SendWithStreamAsync(threadId, "Search the web: what is the latest version of ONLYOFFICE Docs?", profileId);

        round.StatusCode.Should().Be(HttpStatusCode.OK, round.ToString());
        round.StreamError.Should().BeNull(round.ToString());

        var bodies = model.CompletionRequestBodies.Skip(seen).Where(b => StubModelProvider.IsStreamed(b)).ToList();

        // Without a model call there is no tool list to look at, and the NotContain checks would pass on an empty string.
        bodies.Should().NotBeEmpty($"the message must have reached the model; {round}");

        return string.Join("\n", bodies);
    }
}
