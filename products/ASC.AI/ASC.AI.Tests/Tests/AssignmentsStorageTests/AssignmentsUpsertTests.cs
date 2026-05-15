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
public class AssignmentsUpsertTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task UpsertMany_InsertsNewRows()
    {
        var chatProfile = await CreateProfileAsync();
        var codeProfile = await CreateProfileAsync();

        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = chatProfile.Id,
                    ["Code"] = codeProfile.Id
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var all = await ReadAllAssignmentsAsync();
        all.Should().HaveCount(2);
        all["Chat"].Should().Be(chatProfile.Id);
        all["Code"].Should().Be(codeProfile.Id);
    }

    [Fact]
    public async Task UpsertMany_UpdatesExistingRows()
    {
        var initialChat = await CreateProfileAsync();
        var newChat = await CreateProfileAsync();
        var newCode = await CreateProfileAsync();
        await CreateAssignmentAsync("Chat", initialChat.Id);

        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = newChat.Id,
                    ["Code"] = newCode.Id
                }
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        var all = await ReadAllAssignmentsAsync();
        all["Chat"].Should().Be(newChat.Id);
        all["Code"].Should().Be(newCode.Id);
    }

    [Fact]
    public async Task UpsertMany_WithEntityId_IsolatedFromGlobal()
    {
        var globalProfile = await CreateProfileAsync();
        var scopedProfile = await CreateProfileAsync();
        var roomId = await CreateRoomAsync();

        await CreateAssignmentAsync("Chat", globalProfile.Id);

        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = scopedProfile.Id
                },
                entityId = roomId.ToString()
            },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        (await ReadAssignmentAsync("Chat")).Should().Be(globalProfile.Id);
        (await ReadAssignmentAsync("Chat", roomId.ToString())).Should().Be(scopedProfile.Id);
    }

    [Fact]
    public async Task UpsertMany_EmptyDict_NoOp()
    {
        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new { assignments = new Dictionary<string, Guid>() },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        (await ReadAllAssignmentsAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertMany_NonExistentEntityId_Returns404()
    {
        var profile = await CreateProfileAsync();

        using var response = await Ai.PutAsync(
            AssignmentsPath,
            new
            {
                assignments = new Dictionary<string, Guid>
                {
                    ["Chat"] = profile.Id
                },
                entityId = "999999999"
            },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
