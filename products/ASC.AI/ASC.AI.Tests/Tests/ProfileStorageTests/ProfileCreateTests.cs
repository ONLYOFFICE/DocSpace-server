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

namespace ASC.AI.Tests.Tests.ProfileStorageTests;

[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Profiles")]
public class ProfileCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string CreatePath = "/api/2.0/ai/profiles/create";

    [Fact]
    public async Task Create_Owner_ReturnsCreatedProfile()
    {
        var input = BuildProfileInput("my-profile");

        var result = await _aiProfiles.AiProfilesCreateAsync(input, TestContext.Current.CancellationToken);

        result.Success.Should().BeTrue($"profiles/create rejected the profile: {result.Error}\n{ModelProvider.Describe()}");
        result.Error.Should().BeNull();

        var created = result.Profile;
        created.Should().NotBeNull();
        Guid.TryParse(created.Id, out var id).Should().BeTrue();
        id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be(input.Name);
        created.ProviderType.GetString().Should().Be("openai");
        created.BaseUrl.Should().Be(input.BaseUrl);
        created.ModelId.Should().Be(input.ModelId);
        created.Key.Should().Be(input.Key, "the owner is an administrator, whose answer carries the key");
        created.CreatedAt.Should().BeGreaterThan(0);

        var stored = await _aiProfiles.AiProfilesGetByIdAsync(created.Id, TestContext.Current.CancellationToken);
        stored.Name.Should().Be(input.Name);
    }

    [Fact]
    public async Task Create_InvalidJson_Returns400()
    {
        // Raw on purpose: `capabilities` is a number in the SDK model, so a typed call cannot send a string there.
        // The provider fields point at the stub, so the chat service's live key check passes and the request
        // reaches the storage, which refuses the value.
        using var response = await new RawApiClient(_aiChatClient).PostAsync(
            CreatePath,
            new
            {
                name = "x",
                providerType = "openai",
                baseUrl = ModelProvider.BaseUrl,
                key = StubModelProvider.Key,
                modelId = StubModelProvider.ModelId,
                capabilities = "INVALID_VALUE"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _aiProfiles.AiProfilesListAsync(TestContext.Current.CancellationToken)).Should().BeEmpty("a refused create stores nothing");
    }
}
