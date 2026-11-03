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
/// <c>profiles/test-connection</c>: the body is a bare profile id (a <c>{profileId}</c> wrapper binds too), the
/// verdict is a flat <c>{field, message}</c> pair and never a stack trace or a credential.
/// </summary>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/Profiles")]
public class ProfileTestConnectionTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    private const string TestConnectionPath = $"{ProfilesRoute}/test-connection";

    [Fact]
    public async Task TestConnection_ReachableProvider_ReportsNoProblem()
    {
        var profile = await CreateProfileAsync();

        var verdict = await _aiProfiles.AiProfilesTestConnectionAsync(profile.Id, TestContext.Current.CancellationToken);

        // The engine answers `true | TErrorData`: anything but `true` is a complaint about the provider.
        verdict.ActualInstance.Should().Be(true, $"a working provider is not complained about: {verdict.ToJson()}");
    }

    [Fact]
    public async Task TestConnection_BareIdAndProfileIdWrapper_BindToTheSameProfile()
    {
        var profile = await CreateProfileAsync();

        // Raw on purpose: the typed signature takes the bare id only; the wrapper is the other accepted body.
        var bare = await RawPostAsync(TestConnectionPath, $"\"{profile.Id}\"");
        var wrapped = await RawPostAsync(TestConnectionPath, $$"""{"profileId":"{{profile.Id}}"}""");

        bare.Status.Should().Be(200, bare.Body);
        wrapped.Status.Should().Be(200, wrapped.Body);
        wrapped.Body.Should().Be(bare.Body);
    }

    [Theory]
    [InlineData(CapabilityText | CapabilityTools)]
    [InlineData(CapabilityImageGeneration)]
    public async Task TestConnection_ProviderRefusesTheKey_ReportsTheKeyInAFlatVerdictThatLeaksNothing(int capabilities)
    {
        using var provider = new StubModelProvider();
        var created = await _aiProfiles.AiProfilesCreateAsync(
            new AiCreateProfileInput(
                name: $"profile-{Guid.NewGuid():N}",
                providerType: new AiProviderType("openai"),
                baseUrl: provider.BaseUrl,
                key: StubModelProvider.Key,
                modelId: StubModelProvider.ModelId,
                capabilities: capabilities),
            TestContext.Current.CancellationToken);
        created.Success.Should().BeTrue($"{created.Error}\n{provider.Describe()}");

        // The provider turns the key down after the profile was saved.
        RejectEverything(provider, 401);

        // Raw on purpose: the generated verdict model drops `field`, the very thing a client highlights.
        var raw = await RawPostAsync(TestConnectionPath, $"\"{created.Profile.Id}\"");

        raw.Status.Should().Be(200, raw.Body);
        using var document = JsonDocument.Parse(raw.Body);
        document.RootElement.GetProperty("field").GetString().Should().Be("key");
        document.RootElement.GetProperty("message").GetString().Should().NotBeNullOrEmpty();
        raw.Body.Should().NotContain(StubModelProvider.Key).And.NotContain("Exception").And.NotContain("StackTrace");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("\"\"")]
    [InlineData("5")]
    [InlineData("""{"name":"Autotest","modelId":"m"}""")]
    public async Task TestConnection_BodyWithoutAProfileId_Returns400(string body)
    {
        // Raw on purpose: bodies the typed signature (a string) cannot carry.
        var outcome = await RawPostAsync(TestConnectionPath, body);

        outcome.Status.Should().Be(400, outcome.Body);
        outcome.Body.Should().Contain("profileId required");
    }

    [Theory]
    [MemberData(nameof(MalformedProfileIds), MemberType = typeof(ProfileTestBase))]
    public async Task TestConnection_MalformedId_Returns400(string id)
    {
        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync(id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, outcome.Body);
    }

    /// <summary>Bug 84427: an unknown profile id on test-connection was an unhandled 500; get-by-id already answers 404.</summary>
    [Fact]
    [Trait("Bug", "84427")]
    public async Task TestConnection_UnknownId_Returns404()
    {
        var sibling = await OutcomeOfAsync(() => _aiProfiles.AiProfilesGetByIdAsync(UnknownProfileId, TestContext.Current.CancellationToken));
        var bare = await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync(UnknownProfileId, TestContext.Current.CancellationToken));
        // Raw on purpose: the wrapper is not expressible through the typed signature.
        var wrapped = await RawPostAsync(TestConnectionPath, $$"""{"profileId":"{{UnknownProfileId}}"}""");
        var zero = await OutcomeOfAsync(() => _aiProfiles.AiProfilesTestConnectionAsync(ZeroGuid, TestContext.Current.CancellationToken));

        sibling.Status.Should().Be(404, "get-by-id says the profile does not exist");
        new[] { bare.Status, wrapped.Status, zero.Status }.Should().OnlyContain(status => status == 404, "a profile that does not exist is a 404");
    }
}
