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

namespace ASC.AI.Tests.Tests.ProfileTests;

/// <summary>
/// <c>profiles/list-provider-models</c> against providers that cannot be reached, and the local provider types that
/// need no key. A closed loopback port stands in for a local model server that is not running.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Profiles")]
public class ProfileProviderReachabilityTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Fact]
    public async Task ListProviderModels_ProviderThatDoesNotAnswer_Returns502()
    {
        // 502 "unreachable" is the tell that the connection was actually attempted.
        var outcome = await ProviderModelsOutcomeAsync("openai", CreateClosedPortUrl(), StubModelProvider.Key);

        outcome.Status.Should().Be(502, outcome.Body);
        outcome.Body.Should().Contain(UnreachableMessage);
    }

    /// <summary>Bug 83118 (fixed): gpt4all reported an unreachable server as an empty model list while ollama and lm-studio called it unreachable.</summary>
    [Theory]
    [Trait("Bug", "83118")]
    [InlineData("ollama")]
    [InlineData("lm-studio")]
    [InlineData("gpt4all")]
    public async Task ListProviderModels_LocalServerThatIsNotRunning_IsReportedAsUnreachable(string localProvider)
    {
        var outcome = await ProviderModelsOutcomeAsync(localProvider, CreateClosedPortUrl());

        outcome.Status.Should().Be(502, $"{localProvider}: {outcome.Body}");
        outcome.Body.Should().Contain(UnreachableMessage);
    }

    [Theory]
    [InlineData("ollama")]
    [InlineData("lm-studio")]
    [InlineData("gpt4all")]
    public async Task ListProviderModels_LocalProvider_NeverRefusesTheRequestOverTheKey(string localProvider)
    {
        var url = CreateClosedPortUrl();

        var noKey = await ProviderModelsOutcomeAsync(localProvider, url);
        var emptyKey = await ProviderModelsOutcomeAsync(localProvider, url, string.Empty);
        var bogusKey = await ProviderModelsOutcomeAsync(localProvider, url, "sk-not-a-real-key");

        // The key never enters the decision: all three get the same answer, and none of them is about a key.
        emptyKey.Should().Be(noKey, $"{localProvider} treats the key as irrelevant");
        bogusKey.Should().Be(noKey, $"{localProvider} treats the key as irrelevant");
        noKey.Body.Should().NotContain("API key");
    }

    [Fact]
    public async Task ListProviderModels_HostThatDoesNotResolve_IsRefusedBeforeAnyConnection()
    {
        var outcome = await ProviderModelsOutcomeAsync("openai", "http://ssrf-attacker.invalid:9999", StubModelProvider.Key);

        outcome.Status.Should().Be(400, outcome.Body);
        outcome.Body.Should().Contain("baseUrl host could not be resolved");
    }
}
