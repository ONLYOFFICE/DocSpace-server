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
/// <c>profiles/list-provider-models</c>, the call the provider form makes to pull the model list in: what a working
/// provider returns, and which field a failing one is blamed on. The provider is the stub of the test process.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/Profiles")]
public class ProfileProviderModelsTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Fact]
    public async Task ListProviderModels_ValidKey_ReturnsTheModelsWithEverythingTheCapabilityIconsAreDrawnFrom()
    {
        var models = await ListProviderModelsAsync("openai", ModelProvider.BaseUrl, StubModelProvider.Key);

        var model = models.Should().ContainSingle().Subject;
        model.Id.Should().Be(StubModelProvider.ModelId);
        model.Name.Should().NotBeNullOrEmpty();
        model.Provider.GetString().Should().Be("openai");
        // An all-zero mask leaves the picker with no icons, and an unknown bit with an icon it cannot draw.
        model.Capabilities.Should().BeGreaterThan(0);
        ((int)model.Capabilities & ~KnownCapabilityBits).Should().Be(0);
        ((int)model.Capabilities & CapabilityText).Should().Be(CapabilityText, "a discovered chat model is a text model");
    }

    /// <summary>
    /// Bug 83113: the catalogue and the discovery described the same model with different capabilities, so one
    /// screen showed a vision icon and no reasoning icon on a model and the next showed the opposite. A profile
    /// saved from what discovery reported has to be listed with that very description.
    /// </summary>
    [Fact]
    [Trait("Bug", "83113")]
    public async Task ListProviderModels_AndTheCatalogue_DescribeTheSameModelAlike()
    {
        var discovered = (await ListProviderModelsAsync("openai", ModelProvider.BaseUrl, StubModelProvider.Key)).Single();

        await CreateConfiguredProfileAsync(input =>
        {
            input.Capabilities = discovered.Capabilities;
            input.Reasoning = discovered.Reasoning;
            input.ReasoningSupport = discovered.ReasoningSupport;
        });
        var listed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();

        listed.ModelId.Should().Be(discovered.Id);
        listed.Capabilities.Should().Be(discovered.Capabilities);
        listed.Reasoning.Should().Be(discovered.Reasoning);
    }

    /// <summary>
    /// The transport is not checked against the host: three transports pointed at one host are all answered, and
    /// each model's <c>provider</c> echoes what the caller asked for, so nothing in the answer reveals a mismatch.
    /// </summary>
    [Theory]
    [InlineData("deepseek")]
    [InlineData("openai")]
    [InlineData("anthropic")]
    public async Task ListProviderModels_TransportPointedAtAnotherHost_IsAnsweredAndEchoesTheRequestedProvider(string transport)
    {
        using var provider = CreateDualShapeProvider();

        var models = await ListProviderModelsAsync(transport, provider.BaseUrl, StubModelProvider.Key);

        models.Should().NotBeEmpty();
        models.Select(m => m.Provider.GetString()).Should().OnlyContain(name => name == transport);
    }

    /// <summary>Bug 83119 (fixed): the anthropic transport returned models with no display name, while the other transports labelled every row.</summary>
    [Fact]
    [Trait("Bug", "83119")]
    public async Task ListProviderModels_EveryTransportNamesEveryModel()
    {
        using var provider = CreateDualShapeProvider();
        var unnamed = new List<string>();

        foreach (var transport in new[] { "deepseek", "openai", "anthropic" })
        {
            var models = await ListProviderModelsAsync(transport, provider.BaseUrl, StubModelProvider.Key);

            unnamed.AddRange(models.Where(m => string.IsNullOrEmpty(m.Name)).Select(m => $"{transport}: {m.Id}"));
        }

        unnamed.Should().BeEmpty("these models came back without a display name");
    }

    /// <summary>The model list of a gateway-less portal comes from the provider, also for the `onlyoffice` provider type.</summary>
    [Fact]
    public async Task ListProviderModels_OnlyofficeTypeWithoutTheGateway_FallsBackToTheProviderListing()
    {
        var models = await ListProviderModelsAsync("onlyoffice", ModelProvider.BaseUrl, StubModelProvider.Key);

        models.Should().Contain(m => m.Id == StubModelProvider.ModelId);
    }

    [Theory]
    [InlineData(401, 400, InvalidKeyMessage)]
    [InlineData(403, 400, InvalidKeyMessage)]
    [InlineData(404, 400, "Invalid base URL")]
    [InlineData(400, 400, "The AI provider rejected the request (400)")]
    [InlineData(500, 502, "The AI provider returned an error")]
    public async Task ListProviderModels_ProviderFailure_IsReportedAgainstTheInputThatCausedIt(int providerStatus, int expectedStatus, string expectedMessage)
    {
        using var provider = new StubModelProvider();
        RejectEverything(provider, providerStatus);

        var outcome = await ProviderModelsOutcomeAsync("openai", provider.BaseUrl, StubModelProvider.Key);

        outcome.Status.Should().Be(expectedStatus, $"{outcome.Body}\n{provider.Describe()}");
        outcome.Body.Should().Contain(expectedMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ListProviderModels_CloudProviderWithoutAUsableKey_IsReportedAgainstTheKey(string? apiKey)
    {
        using var provider = CreateKeyCheckingProvider();

        // Positive control: this host and this key do produce a model list, so the refusal below is caused by the key.
        (await ListProviderModelsAsync("openai", provider.BaseUrl, StubModelProvider.Key)).Should().NotBeEmpty();

        var outcome = await ProviderModelsOutcomeAsync("openai", provider.BaseUrl, apiKey);

        outcome.Status.Should().Be(400, $"{outcome.Body}\n{provider.Describe()}");
        outcome.Body.Should().Contain(InvalidKeyMessage);
    }
}
