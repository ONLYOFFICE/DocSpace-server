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

namespace ASC.AI.Tests.Tests.ThreadStorageTests;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Threads")]
public class ThreadCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var created = await CreateThreadAsync("my-thread");

        created.Id.Should().NotBe(Guid.Empty);
        created.Title.Should().Be("my-thread");
        created.ProfileId.Should().BeNull();
        created.EntityId.Should().BeNull();

        var stored = await ReadThreadAsync(created.Id);
        stored.Id.Should().Be(created.Id);
        stored.Title.Should().Be("my-thread");
    }

    [Fact]
    public async Task Create_WithProfileId_PersistsProfile()
    {
        var profile = await CreateProfileAsync();

        var created = await CreateThreadAsync(profileId: profile.Id);

        created.ProfileId.Should().Be(profile.Id);
        (await ReadThreadAsync(created.Id)).ProfileId.Should().Be(profile.Id);
    }

    [Fact]
    public async Task Create_WithEntityId_PersistsForFolder()
    {
        var roomId = await CreateRoomAsync();

        var created = await CreateThreadAsync(entityId: roomId.ToString());

        created.EntityId.Should().Be(roomId.ToString());

        var scoped = await ReadAllThreadsAsync(roomId.ToString());
        scoped.Should().ContainSingle(t => t.Id == created.Id);

        var global = await ReadAllThreadsAsync();
        global.Should().NotContain(t => t.Id == created.Id);
    }

    [Fact]
    public async Task Create_NonExistentProfileId_Returns404()
    {
        using var response = await Ai.PostAsync(
            ThreadsPath,
            new { title = "x", profileId = Guid.NewGuid() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_NonExistentEntityId_Returns404()
    {
        using var response = await Ai.PostAsync(
            ThreadsPath,
            new { title = "x", entityId = "999999999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_InvalidJson_Returns400()
    {
        const string invalidJson = """{ "profileId": null }""";

        using var response = await Ai.PostRawAsync(ThreadsPath, invalidJson, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
