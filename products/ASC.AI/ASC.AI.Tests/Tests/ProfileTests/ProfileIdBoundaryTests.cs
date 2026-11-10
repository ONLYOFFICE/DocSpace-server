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
/// The read routes that take a profile id (<c>get-by-id</c>, <c>list-models</c>): a malformed or missing id is a 400,
/// a well-formed id nobody owns is a 404 on both.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Profiles")]
public class ProfileIdBoundaryTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Theory]
    [MemberData(nameof(MalformedProfileIds), MemberType = typeof(ProfileTestBase))]
    public async Task GetById_MalformedId_Returns400(string id)
    {
        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, outcome.Body);
    }

    [Fact]
    public async Task GetById_EmptyOrMissingId_Returns400()
    {
        var empty = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(string.Empty, TestContext.Current.CancellationToken));
        // Raw on purpose: the typed signature always sends the parameter.
        var missing = await RawGetAsync($"{ProfilesRoute}/get-by-id");

        empty.Status.Should().Be(400, empty.Body);
        missing.Status.Should().Be(400);
        missing.Body.Should().Contain("id required");
    }

    /// <summary>
    /// Bug 82818 (fixed): an unknown profile id answered 200 with a null body, which a caller could not tell from a
    /// profile with no fields. It is a 404 that says so.
    /// </summary>
    [Theory]
    [Trait("Bug", "82818")]
    [InlineData(UnknownProfileId)]
    [InlineData(ZeroGuid)]
    public async Task GetById_UnknownId_Returns404WithAMessage(string id)
    {
        await CreateProfileAsync();

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(404, outcome.Body);
        outcome.Body.Should().Contain(ProfileNotFoundMessage);
    }

    [Theory]
    [MemberData(nameof(MalformedProfileIds), MemberType = typeof(ProfileTestBase))]
    public async Task ListModels_MalformedId_Returns400(string id)
    {
        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync(id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, outcome.Body);
    }

    [Fact]
    public async Task ListModels_EmptyOrMissingProfileId_Returns400()
    {
        var empty = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync(string.Empty, TestContext.Current.CancellationToken));
        // Raw on purpose: the typed signature always sends the parameter.
        var missing = await RawGetAsync($"{ProfilesRoute}/list-models");

        empty.Status.Should().Be(400, empty.Body);
        empty.Body.Should().Contain("profileId required");
        missing.Status.Should().Be(400);
        missing.Body.Should().Contain("profileId required");
    }

    /// <summary>
    /// Bug 84426: an unknown profile id on list-models was a 502 "Failed to list provider models" - a gateway error
    /// for a profile that does not exist, which a caller cannot tell from an outage. get-by-id already answers 404.
    /// </summary>
    [Theory]
    [Trait("Bug", "84426")]
    [InlineData(UnknownProfileId)]
    [InlineData(ZeroGuid)]
    public async Task ListModels_UnknownId_Returns404(string id)
    {
        var sibling = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(id, TestContext.Current.CancellationToken));
        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync(id, TestContext.Current.CancellationToken));

        sibling.Status.Should().Be(404, "get-by-id says the profile does not exist, so the contract exists on this controller");
        outcome.Status.Should().Be(404, outcome.Body);
    }

    /// <summary>
    /// Bug 82823 (fixed): list-models crashed with a 500 for any real profile id. On a portal without the AI gateway
    /// it reaches the provider the profile points at and lists its models.
    /// </summary>
    [Fact]
    [Trait("Bug", "82823")]
    public async Task ListModels_StoredProfile_ListsTheModelsOfItsProvider()
    {
        var profile = await CreateProfileAsync();

        var models = await _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken);

        var model = models.Should().ContainSingle().Subject;
        model.Id.Should().Be(StubModelProvider.ModelId);
        model.Name.Should().NotBeNullOrEmpty();
        model.Provider.GetString().Should().Be("openai");
        model.Capabilities.Should().BeGreaterThan(0);
        ((int)model.Capabilities & ~KnownCapabilityBits).Should().Be(0);
    }
}
