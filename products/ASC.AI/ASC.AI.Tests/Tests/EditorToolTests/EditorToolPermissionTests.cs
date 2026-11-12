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

namespace ASC.AI.Tests.Tests.EditorToolTests;

/// <summary>
/// Who may use <c>list</c> and <c>call</c>, and that <c>call</c> runs as the caller. Every authenticated type gets
/// <c>list</c>, a Guest without the document generators; <c>call</c> is closed to a Guest by role before any tool
/// runs; Anonymous is 401.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/EditorTools")]
public class EditorToolPermissionTests(AspireAppFixture fixture) : EditorToolTestBase(fixture)
{
    private const string Forbidden = "Forbidden";

    [Theory]
    [MemberData(nameof(NonGuestRoles))]
    public async Task List_ANonGuestGetsTheSameCatalogueAsTheOwner(EmployeeType type)
    {
        await RequireGeneratorsAsync();
        var ownerNames = (await ToolsAsync()).Select(t => t.Name).ToList();
        ownerNames.Should().NotBeEmpty();
        await ActAsAsync(type);

        var reply = await ListAsync();

        reply.Status.Should().Be(200, reply.ToString());
        reply.Json.GetProperty("tools").EnumerateArray().Select(t => t.GetProperty("name").GetString()).Should().Equal(ownerNames);
    }

    [Fact]
    public async Task List_AGuestGetsTheCatalogueWithoutTheDocumentGenerators()
    {
        await RequireGeneratorsAsync();
        var ownerNames = (await ToolsAsync()).Select(t => t.Name).ToList();
        ownerNames.Should().Contain(GeneratorTools, "the owner is offered the generators");
        await ActAsAsync(EmployeeType.Guest);

        var reply = await ListAsync();

        reply.Status.Should().Be(200, reply.ToString());
        reply.Json.GetProperty("tools").EnumerateArray().Select(t => t.GetProperty("name").GetString())
            .Should().Equal(ownerNames.Where(n => !GeneratorTools.Contains(n)));
    }

    /// <summary>The tool runs as the caller: the generated file lands in the member's own My documents, not the owner's.</summary>
    [Theory]
    [MemberData(nameof(NonGuestRoles))]
    public async Task Generator_ActsInTheCallersOwnMyDocuments(EmployeeType type)
    {
        await RequireGeneratorsAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == DocxTool);
        await ActAsAsync(type);
        var folderId = await MyFolderIdAsync();
        var fileName = RandomName();

        var outcome = await RunAsync(DocxTool, GeneratorArguments(tool, fileName));

        outcome.IsError.Should().BeFalse(outcome.Raw);
        outcome.File!.ParentId.Should().Be(folderId);
        (await TitlesInAsync(folderId)).Should().Contain(fileName + ".docx");
    }

    /// <summary>
    /// A Guest used to reach the read tools like every other member; the whole route is closed to them by role, the
    /// same way <c>/ai/profiles</c> and <c>/ai/web-search</c> are. The name need not be a published tool: the role
    /// gate runs before the name is looked up.
    /// </summary>
    [Fact]
    public async Task Call_AGuestCannotCallEvenAReadTool()
    {
        await ActAsAsync(EmployeeType.Guest);

        var reply = await CallToolAsync("get_room_types");

        reply.Error.Should().Be(Forbidden, "the refusal body");
        reply.Status.Should().Be(403, reply.ToString());
    }

    [Theory]
    [MemberData(nameof(Generators))]
    public async Task Call_AGuestNamingAGeneratorWithoutARoom_GetsNothing(string toolName, string extension)
    {
        await RequireGeneratorsAsync();
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == toolName);
        var ownerMy = await MyFolderIdAsync();
        await ActAsAsync(EmployeeType.Guest);
        var fileName = RandomName("Autotest guest");

        var reply = await CallToolAsync(toolName, GeneratorArguments(tool, fileName));

        await ActAsOwnerAsync();
        (await TitlesInAsync(ownerMy)).Should().NotContain(fileName + extension);
        reply.Error.Should().Be(Forbidden, reply.ToString());
        reply.Status.Should().Be(403, reply.ToString());
    }

    /// <summary>
    /// The Guest's list has no generators, and the same Guest naming one with the id of a room where they hold Content
    /// Creator used to get a document written into it. Nothing may be written, and the call is refused by role.
    /// </summary>
    [Theory]
    [MemberData(nameof(Generators))]
    [Trait("Bug", "84392")]
    public async Task Call_AGuestNamingAGeneratorAndARoom_WritesNothingIntoTheRoom(string toolName, string extension)
    {
        await RequireGeneratorsAsync();
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == toolName);
        var roomId = await CreateCustomRoomAsync("Autotest guest room");
        await FilesApi.CreateFileAsync(roomId, new CreateFileRequest("Autotest Control"), TestContext.Current.CancellationToken);
        var guest = await InviteGuest(TestContext.Current.CancellationToken);
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation(guest.Id, FileShare.ContentCreator)], notify: false),
            TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(guest);
        await FilesHttpClient.Authenticate(guest);
        var before = await TitlesInAsync(roomId);
        var fileName = RandomName("Autotest guest");

        var reply = await CallToolAsync(toolName, GeneratorArguments(tool, fileName), roomId.ToString());

        var after = await TitlesInAsync(roomId);
        before.Should().NotBeEmpty("the premise: the Guest can open the room and sees its file");
        after.Should().Equal(before, $"no generator may write a {extension} document into the room");
        reply.Status.Should().Be(403, reply.ToString());
    }

    [Fact]
    public async Task List_Anonymous_Returns401()
    {
        await _aiChatClient.Authenticate(null);

        var reply = await ListAsync();

        reply.Error.Should().Be("Unauthorized");
        reply.Status.Should().Be(401, reply.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Call_Anonymous_Returns401_ForAValidAndForAnInvalidBody(bool valid)
    {
        await _aiChatClient.Authenticate(null);

        // Raw only for the body without a name, which the typed request cannot carry.
        var reply = valid ? await CallToolAsync("get_room_types") : await CallRawAsync("{}");

        reply.Error.Should().Be("Unauthorized");
        reply.Status.Should().Be(401, reply.ToString());
    }
}
