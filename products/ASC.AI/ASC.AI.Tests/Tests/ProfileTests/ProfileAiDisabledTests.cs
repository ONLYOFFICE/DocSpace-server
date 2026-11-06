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
/// The profile routes with the portal AI switch off: every well-formed request is a 403, ahead of provider
/// resolution and of the provider being dialled; a missing required parameter is still a 400.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Profiles")]
public class ProfileAiDisabledTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Theory]
    [InlineData("list")]
    [InlineData("get-by-id")]
    [InlineData("test-connection")]
    [InlineData("create")]
    [InlineData("update")]
    [InlineData("delete")]
    public async Task EveryRoute_AiAccessDisabled_Returns403(string route)
    {
        var profile = await CreateProfileAsync();
        await SetPortalAiAccessAsync(false);
        var cancellationToken = TestContext.Current.CancellationToken;

        var outcome = route switch
        {
            "list" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesListAsync(cancellationToken)),
            "get-by-id" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(profile.Id, cancellationToken)),
            "test-connection" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync(profile.Id, cancellationToken)),
            "create" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(BuildProfileInput(), cancellationToken)),
            "update" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesUpdateAsync(profile, cancellationToken)),
            _ => await OutcomeOfAsync(() => _aiProfiles.AiProfilesDeleteAsync(profile.Id, cancellationToken))
        };

        outcome.Status.Should().Be(403, $"{route} with AI access disabled: {outcome.Body}");
    }

    /// <summary>Bug 82971 (fixed): list-models answered the provider-key failure ahead of the AI switch - the one route that reached outward before refusing a disabled portal.</summary>
    [Fact]
    [Trait("Bug", "82971")]
    public async Task ListModels_AiAccessDisabled_IsRefusedBeforeTheProviderIsDialled()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var requestsBefore = provider.Server.LogEntries.Count();
        await SetPortalAiAccessAsync(false);

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(403, "the AI switch is checked first");
        outcome.Body.Should().Contain("Forbidden");
        provider.Server.LogEntries.Count().Should().Be(requestsBefore, provider.Describe());
    }

    /// <summary>Bug 82810: provider discovery ignored the AI switch and was answered with a model catalogue while AI access was disabled.</summary>
    [Fact]
    [Trait("Bug", "82810")]
    public async Task ListProviderModels_AiAccessDisabled_IsRefused()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        await SetPortalAiAccessAsync(false);

        // The switch is being enforced on the neighbouring route, so this is not "the disable did not take effect".
        var gated = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(profile.Id, TestContext.Current.CancellationToken));
        var requestsBefore = provider.Server.LogEntries.Count();
        var outcome = await ProviderModelsOutcomeAsync("openai", provider.BaseUrl, StubModelProvider.Key);

        gated.Status.Should().Be(403, "get-by-id is gated");
        outcome.Status.Should().Be(403, "provider discovery must be refused when AI access is disabled");
        outcome.Body.Should().Contain("Forbidden", "the refusal body, not a model catalogue");
        provider.Server.LogEntries.Count().Should().Be(requestsBefore, "the provider was not dialled");
    }

    [Theory]
    [InlineData("get-by-id")]
    [InlineData("list-models")]
    [InlineData("test-connection")]
    public async Task IdRoutes_AiAccessDisabled_RefuseBeforeAMalformedIdIsRejected(string route)
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var control = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync("not-a-guid", cancellationToken));
        await SetPortalAiAccessAsync(false);

        var outcome = route switch
        {
            "get-by-id" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync("not-a-guid", cancellationToken)),
            "list-models" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync("not-a-guid", cancellationToken)),
            _ => await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync("not-a-guid", cancellationToken))
        };

        // Control, with AI on: the same id is a 400, so the 403 below can only come from the switch.
        control.Status.Should().Be(400);
        outcome.Status.Should().Be(403, $"{route} with AI access disabled: {outcome.Body}");
    }

    [Fact]
    public async Task MissingRequiredParameter_AiAccessDisabled_IsStillA400_WhileAWellFormedRequestIsA403()
    {
        var profile = await CreateProfileAsync();
        // Control: with AI on, the missing id is the same 400, so the 400s below are the validation answer.
        var control = await RawGetAsync($"{ProfilesRoute}/get-by-id");
        await SetPortalAiAccessAsync(false);

        // Raw on purpose: the typed signatures always send their parameter.
        var invalid = new[]
        {
            await RawGetAsync($"{ProfilesRoute}/get-by-id"),
            await RawGetAsync($"{ProfilesRoute}/list-models"),
            await RawPostAsync($"{ProfilesRoute}/test-connection", "{}")
        };
        var refused = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(profile.Id, TestContext.Current.CancellationToken));

        control.Status.Should().Be(400, "AI is on");
        invalid.Select(o => o.Status).Should().OnlyContain(status => status == 400);
        refused.Status.Should().Be(403);
    }

    [Fact]
    public async Task CreateUpdateDelete_AiAccessDisabled_LeaveTheCatalogueExactlyAsItWas()
    {
        var profile = await CreateProfileAsync();
        var before = await CatalogueSnapshotAsync();
        var renamed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();
        renamed.Name = "Autotest renamed while off";
        await SetPortalAiAccessAsync(false);

        var created = await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(BuildProfileInput("Autotest while off"), TestContext.Current.CancellationToken));
        var updated = await OutcomeOfAsync(() => _aiProfiles.AiProfilesUpdateAsync(renamed, TestContext.Current.CancellationToken));
        var deleted = await OutcomeOfAsync(() => _aiProfiles.AiProfilesDeleteAsync(profile.Id, TestContext.Current.CancellationToken));

        // The catalogue can only be read with the switch back on, so look afterwards.
        await SetPortalAiAccessAsync(true);
        (await CatalogueSnapshotAsync()).Should().Be(before, "nothing changed while AI was off");
        created.Status.Should().Be(403, "create");
        updated.Status.Should().Be(403, "update");
        deleted.Status.Should().Be(403, "delete");
    }

    [Fact]
    public async Task Create_AiSwitchIsItsOwnGate_AheadOfProviderTypeResolution()
    {
        var input = BuildProfileInput();
        input.ProviderType = new AiProviderType("totally-unknown");

        await SetPortalAiAccessAsync(false);
        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken));

        // With AI on this body is refused for its unknown provider type (ProfileProviderTypeTests); with AI off it never gets that far.
        outcome.Status.Should().Be(403, outcome.Body);
        outcome.Body.Should().Contain("Forbidden");
    }
}
