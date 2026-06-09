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

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Profiles")]
public class ProfileCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Create_Owner_ReturnsCreatedProfile()
    {
        var dto = BuildCreateDto("my-profile");

        var created = await CreateProfileAsync(dto);

        created.Should().NotBeNull();
        created.Id.Should().NotBe(Guid.Empty);
        created.Name.Should().Be(dto.Name);
        created.ProviderType.Should().Be(dto.ProviderType);
        created.BaseUrl.Should().Be(dto.BaseUrl);
        created.ModelId.Should().Be(dto.ModelId);
        created.Reasoning.Should().Be(dto.Reasoning);
        created.Capabilities.Should().Be(dto.Capabilities);
        created.UseResponsesApi.Should().Be(dto.UseResponsesApi);
        created.CanUseTool.Should().Be(dto.CanUseTool);
        created.Key.Should().Be(dto.Key);
        created.CreatedAt.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidJson_Returns400()
    {
        const string invalidJson = """{ "name": "x", "providerType": "openai", "baseUrl": "https://api.openai.com", "modelId": "gpt", "capabilities": "INVALID_VALUE" }""";

        using var response = await Ai.PostRawAsync(ProfilesPath, invalidJson, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
