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
/// <c>profiles/list-models</c> on a saved profile whose provider later refuses the stored key or stops answering: the
/// refusal is reported against the key (400), the silence against the provider (502).
/// </summary>
[Trait("Category", "Validation")]
[Trait("Feature", "AI/Profiles")]
public class ProfileListModelsFailureTests(AspireAppFixture fixture) : ProfileTestBase(fixture)
{
    [Fact]
    public async Task ListModels_ProviderNowRefusesTheStoredKey_Returns400NamingTheKey()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);

        // Positive control: the same profile lists its models while the provider still accepts the key.
        (await _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken)).Should().NotBeEmpty();

        RejectEverything(provider, 401);
        var outcome = await OutcomeOfAsync(() => _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(400, $"{outcome.Body}\n{provider.Describe()}");
        outcome.Body.Should().Contain(InvalidKeyMessage);
    }

    /// <summary>
    /// Open, no bug number: a provider that accepts the connection and closes it without an answer is reported as a
    /// provider with no models (200 and an empty list) instead of 502, so the profile form shows an empty model list
    /// rather than the failure (the same class as Bug 83118, fixed for an unreachable local server).
    /// </summary>
    [Fact]
    [Trait("Category", "Bug")]
    public async Task ListModels_ProviderDropsTheConnection_Returns502()
    {
        using var provider = new StubModelProvider();
        var profile = await CreateProfileOnAsync(provider);

        // The connection is accepted and closed without an answer, so the port stays the provider's own.
        provider.Server
            .Given(Request.Create().WithPath("/*"))
            .AtPriority(-10)
            .RespondWith(Response.Create().WithFault(FaultType.EMPTY_RESPONSE));

        List<AiModel>? listed = null;
        var outcome = await OutcomeOfAsync(async () => listed = await _aiProfiles.AiProfilesListModelsAsync(profile.Id, TestContext.Current.CancellationToken));

        outcome.Status.Should().Be(502, $"listed {JsonSerializer.Serialize(listed)} {outcome.Body}\n{provider.Describe()}");
    }
}
