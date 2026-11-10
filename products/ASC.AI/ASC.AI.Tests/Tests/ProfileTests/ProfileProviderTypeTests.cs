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
/// The provider types the profile routes resolve: every built-in transport is known to the backend, a name that is
/// not an identifier is a validation error, and the `external` type (a transport delegated to the browser) is refused.
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Profiles")]
public class ProfileProviderTypeTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Fact]
    public void BuiltInProviderTypes_AreSeventeen()
    {
        // The count the product documentation quotes, pinned so the prose and the API cannot drift apart in silence.
        Enum.GetValues<AiBuiltinProviderType>().Should().HaveCount(17);
    }

    [Fact]
    public async Task ListProviderModels_EveryBuiltInTransport_IsResolvedByTheBackend()
    {
        var unresolved = new List<AiBuiltinProviderType>();

        // `external` has no transport of its own (see the create/update tests below), so it is not asked here.
        foreach (var type in Enum.GetValues<AiBuiltinProviderType>().Where(t => t != AiBuiltinProviderType.External))
        {
            var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListProviderModelsAsync(
                new AiProfilesListProviderModelsRequest(new AiProviderType(type), ModelProvider.BaseUrl, StubModelProvider.Key),
                TestContext.Current.CancellationToken));

            // What the transport makes of the stub's answer is not the point; that the name is known is.
            if (outcome.Body.Contains("Unknown provider type"))
            {
                unresolved.Add(type);
            }
        }

        unresolved.Should().BeEmpty("these provider types are no longer known to the backend");
    }

    /// <summary>
    /// Bug 83117 (fixed): an unknown provider type was a 502 "Failed to list provider models" - a gateway error for a
    /// name that is not a provider at all, indistinguishable from an outage. The names include the human-readable ones
    /// (Google is `genai`, LM Studio is `lm-studio`) and near misses of real identifiers.
    /// </summary>
    [Theory]
    [Trait("Bug", "83117")]
    [InlineData("not-a-provider")]
    [InlineData("google")]
    [InlineData("gemini")]
    [InlineData("lmstudio")]
    [InlineData("open-ai")]
    [InlineData("gpt-4all")]
    [InlineData("local")]
    [InlineData("   ")]
    [InlineData("DEEPSEEK")]
    public async Task ListProviderModels_NameThatIsNotAnIdentifier_IsAValidationError(string providerType)
    {
        var outcome = await ProviderModelsOutcomeAsync(providerType, ModelProvider.BaseUrl, StubModelProvider.Key);

        outcome.Status.Should().Be(400, outcome.Body);
        outcome.Body.Should().Contain("Unknown provider type");
    }

    [Fact]
    public async Task ListProviderModels_ProviderTypeThatIsANumber_IsAValidationError()
    {
        // Raw on purpose: a wrong-typed value the typed request cannot carry.
        var outcome = await RawPostAsync(
            $"{ProfilesRoute}/list-provider-models",
            $$"""{"providerType":5,"baseUrl":"{{ModelProvider.BaseUrl}}","apiKey":"{{StubModelProvider.Key}}"}""");

        outcome.Status.Should().Be(400, outcome.Body);
        outcome.Body.Should().Contain("Unknown provider type");
    }

    [Fact]
    public async Task Create_UnknownProviderType_IsRefusedAndNothingIsStored()
    {
        var input = BuildProfileInput();
        input.ProviderType = new AiProviderType("totally-unknown");

        // The refusal is either a soft `{success:false, error}` answer or a 400; the message and the empty catalogue are the contract.
        string message;
        try
        {
            var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);
            result.Success.Should().BeFalse();
            message = result.Error?.Message ?? string.Empty;
        }
        catch (ApiException exception)
        {
            message = exception.ErrorContent?.ToString() ?? string.Empty;
        }

        message.Should().Contain("Unknown provider type");
        (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    /// <summary>
    /// Bug 83114 (fixed): the `external` provider type crashed create with a 500 - it delegates every request to the
    /// host application, which a server-created profile does not have. It is a 400 that says so.
    /// </summary>
    [Fact]
    [Trait("Bug", "83114")]
    public async Task Create_ExternalProviderType_Returns400()
    {
        var input = BuildProfileInput();
        input.ProviderType = new AiProviderType(AiBuiltinProviderType.External);

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, "a built-in provider type must not crash");
        outcome.Body.Should().Contain("external");
        (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty();
    }

    [Fact]
    [Trait("Bug", "83114")]
    public async Task Update_ExternalProviderType_Returns400_AndTheProfileIsUnchanged()
    {
        var profile = await CreateProfileAsync();
        var update = (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Single();
        update.ProviderType = new AiProviderType(AiBuiltinProviderType.External);

        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesUpdateAsync(update, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, outcome.Body);
        var stored = await _aiProfiles.AiProfilesGetByIdAsync(profile.Id, TestContext.Current.CancellationToken);
        stored.ProviderType.GetString().Should().Be("openai");
    }
}
