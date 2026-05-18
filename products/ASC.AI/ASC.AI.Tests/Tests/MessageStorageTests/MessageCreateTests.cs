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

namespace ASC.AI.Tests.Tests.MessageStorageTests;

[Collection("Test Collection")]
[Trait("Category", "CRUD")]
[Trait("Feature", "AI/Messages")]
public class MessageCreateTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task Create_Owner_PersistsAndIsReadable()
    {
        var thread = await CreateThreadAsync();
        var contents = BuildMessageContents("hello world");

        var created = await CreateMessageAsync(thread.Id, contents);

        created.Id.Should().NotBe(Guid.Empty);
        created.ThreadId.Should().Be(thread.Id);
        JsonEquals(created.Contents, contents).Should().BeTrue();
        created.Timestamp.Should().BeGreaterThan(0);

        var stored = await ReadMessageAsync(created.Id);
        stored.Id.Should().Be(created.Id);
        stored.ThreadId.Should().Be(thread.Id);
        JsonEquals(stored.Contents, contents).Should().BeTrue();
    }

    [Fact]
    public async Task Create_MultipleMessages_AllPersisted()
    {
        var thread = await CreateThreadAsync();
        var firstContents = BuildMessageContents("first");
        var secondContents = BuildMessageContents("second");

        var first = await CreateMessageAsync(thread.Id, firstContents);
        var second = await CreateMessageAsync(thread.Id, secondContents);

        first.Id.Should().NotBe(second.Id);

        var messages = await ReadMessagesByThreadAsync(thread.Id);
        messages.Should().HaveCount(2);
        messages.Should().Contain(m => m.Id == first.Id && JsonEquals(m.Contents, firstContents));
        messages.Should().Contain(m => m.Id == second.Id && JsonEquals(m.Contents, secondContents));
    }

    [Fact]
    public async Task Create_NonExistentThread_Returns404()
    {
        using var response = await Ai.PostAsync(
            $"{ThreadsPath}/{Guid.NewGuid()}/messages",
            new { contents = BuildMessageContents() },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_InvalidJson_Returns400()
    {
        var thread = await CreateThreadAsync();
        const string invalidJson = """{ "foo": "bar" }""";

        using var response = await Ai.PostRawAsync(
            $"{ThreadsPath}/{thread.Id}/messages",
            invalidJson,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
