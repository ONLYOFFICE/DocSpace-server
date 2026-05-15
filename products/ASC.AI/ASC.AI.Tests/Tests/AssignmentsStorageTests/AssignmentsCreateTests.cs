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

namespace ASC.AI.Tests.Tests.AssignmentsStorageTests;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Assignments")]
public class AssignmentsCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var profile = await CreateProfileAsync();

        await CreateAssignmentAsync("Chat", profile.Id);

        var stored = await ReadAssignmentAsync("Chat");
        stored.Should().Be(profile.Id);
    }

    [Fact]
    public async Task Create_WithEntityId_PersistsForFolder()
    {
        var profile = await CreateProfileAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentAsync("Chat", profile.Id, roomId.ToString());

        (await ReadAssignmentAsync("Chat", roomId.ToString())).Should().Be(profile.Id);
        (await ReadAssignmentAsync("Chat")).Should().BeNull();
    }

    [Fact]
    public async Task Create_DuplicateActionType_Fails()
    {
        var profile = await CreateProfileAsync();
        await CreateAssignmentAsync("Chat", profile.Id);

        using var response = await Ai.PostAsync(
            AssignmentsPath,
            new { actionType = "Chat", profileId = profile.Id },
            TestContext.Current.CancellationToken);

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Create_NonExistentEntityId_Returns404()
    {
        var profile = await CreateProfileAsync();

        using var response = await Ai.PostAsync(
            AssignmentsPath,
            new { actionType = "Chat", profileId = profile.Id, entityId = "999999999" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_InvalidJson_Returns400()
    {
        const string invalidJson = """{ "actionType": "Chat" }""";

        using var response = await Ai.PostRawAsync(AssignmentsPath, invalidJson, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
