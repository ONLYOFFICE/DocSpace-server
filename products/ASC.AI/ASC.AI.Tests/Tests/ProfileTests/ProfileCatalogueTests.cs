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
/// What <c>profiles/list</c> and <c>profiles/get-by-id</c> publish about a profile: the capability metadata the
/// model picker draws its icons from, the reasoning description, and what must never be in the answer.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/Profiles")]
public class ProfileCatalogueTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    // Generation tuning is not part of a profile: it would be a hidden variable of every "the model answered X" test.
    private static readonly string[] _generationKnobs =
    [
        "temperature", "topP", "top_p", "topK", "top_k", "maxTokens", "max_tokens", "maxOutputTokens", "max_output_tokens",
        "frequencyPenalty", "frequency_penalty", "presencePenalty", "presence_penalty", "stop", "stopSequences",
        "stop_sequences", "seed"
    ];

    [Theory]
    [InlineData(CapabilityText | CapabilityVision | CapabilityTools)]
    [InlineData(CapabilityText | CapabilityTools)]
    [InlineData(CapabilityImageGeneration)]
    public async Task Capabilities_RoundTripThroughListAndGetById(int mask)
    {
        var created = await CreateConfiguredProfileAsync(input => input.Capabilities = mask);

        var listed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single(p => p.Id == created.Id);
        var single = await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken);

        listed.Capabilities.Should().Be(mask);
        single.Capabilities.Should().Be(mask);
        ((int)listed.Capabilities & ~KnownCapabilityBits).Should().Be(0, "the mask carries bits the client cannot draw an icon for");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReasoningSupport_RoundTripsAndAgreesWithTheReasoningFlag(bool thinks)
    {
        var support = thinks
            ? new AiReasoningSupport(true, false, [AiReasoningDepth.Low, AiReasoningDepth.High], AiReasoningDepth.High)
            : new AiReasoningSupport(false, true, [], null);
        var created = await CreateConfiguredProfileAsync(input =>
        {
            input.Reasoning = thinks;
            input.ReasoningSupport = support;
        });

        var listed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single(p => p.Id == created.Id);
        var single = await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken);

        foreach (var (reasoning, description) in new[] { (listed.Reasoning, listed.ReasoningSupport), (single.Reasoning, single.ReasoningSupport) })
        {
            reasoning.Should().Be(thinks, "the older boolean agrees with `thinks`");
            description.Thinks.Should().Be(thinks);
            description.CanDisable.Should().Be(!thinks);
            description.Depths.Should().Equal(support.Depths);
            description.DefaultDepth.Should().Be(support.DefaultDepth);
        }
    }

    [Fact]
    public async Task ListAndGetById_DescribeEveryProfileIdentically()
    {
        await CreateConfiguredProfileAsync(input => input.Capabilities = CapabilityText | CapabilityTools);
        await CreateConfiguredProfileAsync(input => input.Capabilities = CapabilityImageGeneration);

        foreach (var listed in await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken))
        {
            var single = await _aiProfiles.AiProfilesGetByIdAsync(listed.Id, TestContext.Current.CancellationToken);

            // `key` is deliberately absent from get-by-id, which has no such field.
            new
            {
                single.Id,
                single.Name,
                ProviderType = single.ProviderType.GetString(),
                single.BaseUrl,
                single.ModelId,
                single.Capabilities,
                single.Reasoning,
                single.CanUseTool,
                single.UseResponsesApi
            }.Should().BeEquivalentTo(new
            {
                listed.Id,
                listed.Name,
                ProviderType = listed.ProviderType.GetString(),
                listed.BaseUrl,
                listed.ModelId,
                listed.Capabilities,
                listed.Reasoning,
                listed.CanUseTool,
                listed.UseResponsesApi
            }, listed.ModelId);
        }
    }

    [Fact]
    public async Task List_EveryProfileHasAUniqueGuidAndACompleteDescription()
    {
        for (var i = 0; i < 3; i++)
        {
            await CreateProfileAsync();
        }

        var profiles = await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken);

        profiles.Should().HaveCount(3);
        profiles.Select(p => p.Id).Should().OnlyHaveUniqueItems();

        foreach (var profile in profiles)
        {
            Guid.TryParse(profile.Id, out _).Should().BeTrue(profile.Id);
            profile.Name.Should().NotBeNullOrEmpty();
            profile.ProviderType.GetString().Should().NotBeNullOrEmpty();
            profile.ModelId.Should().NotBeNullOrEmpty();
            Uri.TryCreate(profile.BaseUrl, UriKind.Absolute, out _).Should().BeTrue($"{profile.ModelId} baseUrl is an absolute URL");
        }
    }

    [Fact]
    public async Task List_IsStableBetweenReads_AndReadingChangesNothing()
    {
        var first = await CreateProfileAsync();
        await CreateProfileAsync();

        var before = await CatalogueSnapshotAsync();
        // Reading one profile by id and testing its connection are reads too.
        await _aiProfiles.AiProfilesGetByIdAsync(first.Id, TestContext.Current.CancellationToken);
        // Raw on purpose: the typed test-connection call cannot deserialize its bool-or-object answer (SDK defect).
        (await RawPostAsync($"{ProfilesRoute}/test-connection", $"\"{first.Id}\"")).Status.Should().Be(200);
        var after = await CatalogueSnapshotAsync();

        after.Should().Be(before);
    }

    /// <summary>
    /// Bug 82821 (fixed): get-by-id handed back the gateway's cluster-internal service address in place of the
    /// profile's address, which the list publishes. It reads the stored profile now, so both agree; the answer also
    /// drops the key and the headers.
    /// </summary>
    [Fact]
    [Trait("Bug", "82821")]
    public async Task GetById_AgreesWithTheListOnTheBaseUrl_AndCarriesNoKeyOrHeaders()
    {
        var created = await CreateProfileAsync();

        var listed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();
        var single = await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken);
        // Raw on purpose: the typed model has no `key` or `headers` property, so only the JSON shows they are absent.
        var raw = await RawGetAsync($"{ProfilesRoute}/get-by-id?id={created.Id}");

        listed.BaseUrl.Should().Be(ModelProvider.BaseUrl);
        single.BaseUrl.Should().Be(listed.BaseUrl, "get-by-id must not expose an internal service address");
        raw.Status.Should().Be(200);
        PropertyNamesOf(raw.Body).Should().NotContain(["key", "headers"]);
    }

    [Fact]
    public async Task ListAndGetById_CarryNoGenerationTuningFields()
    {
        // A mask is given: a profile created without one is stored without the field.
        var created = await CreateConfiguredProfileAsync(input => input.Capabilities = CapabilityText | CapabilityTools);

        // Raw on purpose: an unknown field is dropped by the typed model, which is exactly what is being looked for.
        var list = await RawGetAsync($"{ProfilesRoute}/list");
        var single = await RawGetAsync($"{ProfilesRoute}/get-by-id?id={created.Id}");

        list.Status.Should().Be(200);
        single.Status.Should().Be(200);

        foreach (var body in new[] { list.Body, single.Body })
        {
            var names = PropertyNamesOf(body);

            // Positive control: the same objects do carry the capability metadata, so an empty overlap is not an empty object.
            names.Should().Contain(["capabilities", "reasoning"]);
            names.Intersect(_generationKnobs).Should().BeEmpty("a profile exposes no generation tuning");
        }
    }
}
