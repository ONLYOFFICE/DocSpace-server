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
/// Who may read the profiles: every member except a Guest, and anonymous callers are refused on every route.
/// Provider discovery is open to every member, since it needs nothing but a key and a URL.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Profiles")]
public class ProfileAccessReadTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Theory]
    [InlineData("list")]
    [InlineData("get-by-id")]
    [InlineData("list-models")]
    [InlineData("list-provider-models")]
    [InlineData("test-connection")]
    [InlineData("create")]
    [InlineData("update")]
    public async Task EveryRoute_Anonymous_Returns401(string route)
    {
        var profile = await CreateProfileAsync();
        await _aiChatClient.Authenticate(null);
        var cancellationToken = TestContext.Current.CancellationToken;

        var outcome = route switch
        {
            "list" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesListAsync(cancellationToken)),
            "get-by-id" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(profile.Id, cancellationToken)),
            "list-models" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync(profile.Id, cancellationToken)),
            "list-provider-models" => await ProviderModelsOutcomeAsync("openai", ModelProvider.BaseUrl, StubModelProvider.Key),
            "test-connection" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync(profile.Id, cancellationToken)),
            "create" => await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(BuildProfileInput(), cancellationToken)),
            _ => await OutcomeOfAsync(() => _aiProfiles.AiProfilesUpdateAsync(profile, cancellationToken))
        };

        outcome.Status.Should().Be(401, $"{route} as anonymous: {outcome.Body}");
    }

    [Fact]
    public async Task Update_AnonymousWithABodyThatWouldFailValidation_StillReturns401()
    {
        await CreateProfileAsync();
        var before = await CatalogueSnapshotAsync();
        await _aiChatClient.Authenticate(null);

        // Raw on purpose: an empty body cannot be built through the typed update.
        var outcome = await RawPutAsync($"{ProfilesRoute}/update", "{}");

        outcome.Status.Should().Be(401, "authentication comes before validation");
        await ActAsOwnerAsync();
        (await CatalogueSnapshotAsync()).Should().Be(before);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task ListAndGetById_Member_ReadsTheCatalogue(EmployeeType role)
    {
        var profile = await CreateProfileAsync();
        await ActAsAsync(role);

        var listed = await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken);
        var single = await _aiProfiles.AiProfilesGetByIdAsync(profile.Id, TestContext.Current.CancellationToken);
        var raw = await RawGetAsync($"{ProfilesRoute}/get-by-id?id={profile.Id}");

        listed.Select(p => p.Id).Should().Contain(profile.Id);
        single.ModelId.Should().Be(profile.ModelId);
        // get-by-id omits the key entirely rather than masking it.
        PropertyNamesOf(raw.Body).Should().NotContain("key");
    }

    /// <summary>The list carries the key to an administrator, whose settings cards edit it, and to nobody else.</summary>
    [Fact]
    public async Task List_AdministratorSeesTheKey()
    {
        await CreateProfileAsync();
        await ActAsAsync(EmployeeType.DocSpaceAdmin);

        var listed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();

        listed.Key.Should().Be(StubModelProvider.Key);
    }

    /// <summary>
    /// Open, no bug number: the Node service asks the AI service for the list with <c>forClient=true</c> so that it
    /// strips <c>key</c> and <c>headers</c> for everyone but an administrator, but ASC.AI's ProfileStorageController
    /// does not implement that flag - the provider secret reaches every member through the public list.
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task List_MemberDoesNotSeeTheKey()
    {
        await CreateProfileAsync();
        await ActAsAsync(EmployeeType.User);

        var listed = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();

        listed.Key.Should().BeNullOrEmpty("a member sees the catalogue but not a credential in it");
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task ListProviderModels_Member_MayValidateAProviderKey(EmployeeType role)
    {
        // Recorded as the current contract: a User can create neither a profile nor anything else with the result.
        await ActAsAsync(role);

        var models = await ListProviderModelsAsync("openai", ModelProvider.BaseUrl, StubModelProvider.Key);

        models.Should().Contain(m => m.Id == StubModelProvider.ModelId);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task TestConnectionAndListModels_Member_GetTheSameVerdictsAsTheOwner(EmployeeType role)
    {
        var profile = await CreateProfileAsync();

        var ownerConnection = await _aiProfiles.AiProfilesTestConnectionAsync(profile.Id, TestContext.Current.CancellationToken);
        var ownerModels = (await _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken)).Select(m => m.Id).ToList();

        await ActAsAsync(role);
        var connection = await _aiProfiles.AiProfilesTestConnectionAsync(profile.Id, TestContext.Current.CancellationToken);
        var models = (await _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken)).Select(m => m.Id).ToList();

        connection.ToJson().Should().Be(ownerConnection.ToJson(), $"{role} test-connection");
        models.Should().Equal(ownerModels, $"{role} list-models");
    }
}
