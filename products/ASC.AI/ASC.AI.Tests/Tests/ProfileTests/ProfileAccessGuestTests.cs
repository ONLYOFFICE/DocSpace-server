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
/// A Guest is refused on every profile route, and refused before the portal reaches outward for them. A required
/// parameter that is missing or malformed is still a 400 first: that check opens no connection.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Profiles")]
public class ProfileAccessGuestTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    /// <summary>Bug 80713: the Guest was handed the default provider (now the catalogue); every other member route refuses a Guest.</summary>
    [Fact]
    [Trait("Bug", "80713")]
    public async Task List_Guest_Returns403()
    {
        await CreateProfileAsync();
        await ActAsAsync(EmployeeType.Guest);

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(403, outcome.Body);
    }

    [Fact]
    public async Task GetById_Guest_Returns403()
    {
        var profile = await CreateProfileAsync();
        await ActAsAsync(EmployeeType.Guest);

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(profile.Id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(403, outcome.Body);
    }

    [Fact]
    public async Task TestConnection_Guest_Returns403()
    {
        var profile = await CreateProfileAsync();
        await ActAsAsync(EmployeeType.Guest);

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync(profile.Id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(403, outcome.Body);
    }

    /// <summary>Bug 82971 (fixed): list-models raised the provider-key failure ahead of the role check - the one route that reached outward before refusing a Guest.</summary>
    [Fact]
    [Trait("Bug", "82971")]
    public async Task ListModels_Guest_IsRefusedBeforeTheProviderIsDialled()
    {
        // A provider of this test's own: the run-wide stub is dialled by tests running in parallel.
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);
        var requestsBefore = provider.Server.LogEntries.Count();
        await ActAsAsync(EmployeeType.Guest);

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(403, outcome.Body);
        outcome.Body.Should().Contain("Forbidden");
        provider.Server.LogEntries.Count().Should().Be(requestsBefore, $"the provider was not dialled for a Guest\n{provider.Describe()}");
    }

    /// <summary>
    /// Bug 82824 (fixed): provider discovery was answered for a Guest - the portal spent an outbound request on a
    /// Guest-supplied URL and key. Every other route of the family is 403 for a Guest.
    /// </summary>
    [Fact]
    [Trait("Bug", "82824")]
    public async Task ListProviderModels_Guest_IsRefusedAndNothingIsDialled()
    {
        using var provider = new StubModelProvider();
        await ActAsAsync(EmployeeType.Guest);

        var outcome = await ProviderModelsOutcomeAsync("openai", provider.BaseUrl, StubModelProvider.Key);

        outcome.Status.Should().Be(403, "a Guest must not reach the provider-discovery endpoint");
        outcome.Body.Should().Contain("Forbidden", "the refusal body, not a model catalogue");
        provider.Server.LogEntries.Should().BeEmpty("the provider was not dialled for a Guest");
    }

    [Theory]
    [InlineData("get-by-id")]
    [InlineData("list-models")]
    [InlineData("test-connection")]
    public async Task IdRoutes_GuestWithMalformedId_Returns400BeforeTheRoleCheck(string route)
    {
        await ActAsAsync(EmployeeType.Guest);
        var cancellationToken = TestContext.Current.CancellationToken;

        // The accepted convention of /ai/*: id-format checks run before the role check and open no outbound connection.
        var outcome = route switch
        {
            "get-by-id" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync("not-a-guid", cancellationToken)),
            "list-models" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync("not-a-guid", cancellationToken)),
            _ => await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync("not-a-guid", cancellationToken))
        };

        outcome.Status.Should().Be(400, outcome.Body);
    }

    [Fact]
    public async Task TestConnection_GuestWithoutAnId_Returns400BeforeTheRoleCheck()
    {
        await ActAsAsync(EmployeeType.Guest);

        // Raw on purpose: a body without a profile id cannot be built through the typed signature.
        var outcome = await RawPostAsync($"{ProfilesRoute}/test-connection", "{}");

        outcome.Status.Should().Be(400, outcome.Body);
    }
}
