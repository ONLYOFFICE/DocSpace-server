// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
