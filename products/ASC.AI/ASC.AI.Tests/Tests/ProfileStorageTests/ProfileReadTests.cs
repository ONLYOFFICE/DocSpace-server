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
public class ProfileReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ReadById_Existing_ReturnsProfile()
    {
        var created = await CreateProfileAsync();

        using var response = await Ai.GetAsync($"{ProfilesPath}/{created.Id}", TestContext.Current.CancellationToken);
        var profile = await Ai.ReadAsync<ProfileDto>(response, TestContext.Current.CancellationToken);

        profile.Id.Should().Be(created.Id);
        profile.Name.Should().Be(created.Name);
        profile.ProviderType.Should().Be(created.ProviderType);
        profile.BaseUrl.Should().Be(created.BaseUrl);
        profile.ModelId.Should().Be(created.ModelId);
        profile.Key.Should().Be(created.Key);
    }

    [Fact]
    public async Task ReadById_NonExisting_Returns404()
    {
        using var response = await Ai.GetAsync($"{ProfilesPath}/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadById_RoomAdmin_Allowed()
    {
        var created = await CreateProfileAsync();

        var roomAdmin = await Initializer.InviteContactAsync(EmployeeType.RoomAdmin, TestContext.Current.CancellationToken);
        await AiClient.Authenticate(roomAdmin);

        using var response = await Ai.GetAsync($"{ProfilesPath}/{created.Id}", TestContext.Current.CancellationToken);
        var profile = await Ai.ReadAsync<ProfileDto>(response, TestContext.Current.CancellationToken);

        profile.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task ReadById_RegularUser_Returns403()
    {
        var created = await CreateProfileAsync();

        var regularUser = await Initializer.InviteContactAsync(EmployeeType.User, TestContext.Current.CancellationToken);
        await AiClient.Authenticate(regularUser);

        using var response = await Ai.GetAsync($"{ProfilesPath}/{created.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReadAll_Owner_ReturnsAllCreated()
    {
        var first = await CreateProfileAsync(BuildCreateDto("a"));
        var second = await CreateProfileAsync(BuildCreateDto("b"));

        using var response = await Ai.GetAsync(ProfilesPath, TestContext.Current.CancellationToken);
        var all = await Ai.ReadAsync<List<ProfileDto>>(response, TestContext.Current.CancellationToken);

        all.Should().HaveCount(2);
        all.Select(p => p.Id).Should().BeEquivalentTo([first.Id, second.Id]);
    }

    [Fact]
    public async Task ReadAll_Empty_ReturnsEmpty()
    {
        using var response = await Ai.GetAsync(ProfilesPath, TestContext.Current.CancellationToken);
        var all = await Ai.ReadAsync<List<ProfileDto>>(response, TestContext.Current.CancellationToken);

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAll_RegularUser_Returns403()
    {
        await CreateProfileAsync();

        var regularUser = await Initializer.InviteContactAsync(EmployeeType.User, TestContext.Current.CancellationToken);
        await AiClient.Authenticate(regularUser);

        using var response = await Ai.GetAsync(ProfilesPath, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
