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
public class ThreadReadTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task ReadById_Existing_ReturnsThread()
    {
        var created = await CreateThreadAsync("my-thread");

        var stored = await ReadThreadAsync(created.Id);

        stored.Id.Should().Be(created.Id);
        stored.Title.Should().Be("my-thread");
    }

    [Fact]
    public async Task ReadById_NonExisting_Returns404()
    {
        using var response = await Ai.GetAsync(
            $"{ThreadsPath}/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReadAll_Owner_ReturnsAllCreated()
    {
        var first = await CreateThreadAsync("first");
        var second = await CreateThreadAsync("second");

        var all = await ReadAllThreadsAsync();

        all.Should().Contain(t => t.Id == first.Id);
        all.Should().Contain(t => t.Id == second.Id);
    }

    [Fact]
    public async Task ReadAll_Empty_ReturnsEmpty()
    {
        var all = await ReadAllThreadsAsync();

        all.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAll_WithEntityId_ReturnsOnlyScopedThreads()
    {
        var roomId = await CreateRoomAsync();
        var globalThread = await CreateThreadAsync("global");
        var scopedThread = await CreateThreadAsync("scoped", entityId: roomId.ToString());

        var scoped = await ReadAllThreadsAsync(roomId.ToString());

        scoped.Should().ContainSingle(t => t.Id == scopedThread.Id);
        scoped.Should().NotContain(t => t.Id == globalThread.Id);
    }
}
