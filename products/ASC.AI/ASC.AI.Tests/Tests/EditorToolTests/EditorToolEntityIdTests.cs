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
/// <c>entityId</c> of <c>POST /api/2.0/ai/editor-tools/call</c>: the room a generator writes into. A value that does
/// not name an entity at all is not an error, the call falls back to the portal-wide scope (My documents); a room
/// that exists but the caller may not create files in is a different case and never falls back.
/// </summary>
/// <remarks>
/// The TypeScript suite expects a refusal of the second case as 403 <c>Forbidden</c> from the route. The chat service
/// in this repo only gates the route on the portal AI switch and the user type, and passes the room on: the tool
/// catalogue the caller is then offered for that room has no generator in it (the caller cannot create files there),
/// so the name is unknown and the answer is the 400 <c>Unknown tool name</c>. The tests assert that refusal and, above
/// all, that nothing was created anywhere.
/// </remarks>
[Trait("Category", "Functional")]
[Trait("Feature", "AI/EditorTools")]
public class EditorToolEntityIdTests(AspireAppFixture fixture) : EditorToolTestBase(fixture)
{
    /// <summary>JSON literals of an <c>entityId</c> that names no entity.</summary>
    public static TheoryData<string> NonEntities => new()
    {
        "\"999999999\"",
        "\"abc\"",
        "\"\"",
        "\"0\"",
        "\"-1\"",
        "null",
        """{"a":1}"""
    };

    private async Task<(Tool Tool, int FolderId)> StartAsync()
    {
        await ActAsOwnerAsync();
        var tool = (await ToolsAsync()).Single(t => t.Name == DocxTool);

        return (tool, await MyFolderIdAsync());
    }

    [Fact]
    public async Task AccessibleRoom_ScopesTheGeneratorIntoThatRoom()
    {
        await RequireGeneratorsAsync();
        var (tool, _) = await StartAsync();
        var roomId = await CreateCustomRoomAsync("Autotest entity room");
        var fileName = RandomName();

        var outcome = await RunAsync(DocxTool, GeneratorArguments(tool, fileName), roomId.ToString());

        outcome.IsError.Should().BeFalse(outcome.Raw);
        outcome.File!.ParentId.Should().Be(roomId);
        (await TitlesInAsync(roomId)).Should().Contain(fileName + ".docx");
    }

    [Fact]
    public async Task WithoutEntityId_TheCallIsPortalWide_AndLandsInMyDocuments()
    {
        await RequireGeneratorsAsync();
        var (tool, folderId) = await StartAsync();
        var roomId = await CreateCustomRoomAsync("Autotest entity room");
        var fileName = RandomName();

        var outcome = await RunAsync(DocxTool, GeneratorArguments(tool, fileName));

        outcome.IsError.Should().BeFalse(outcome.Raw);
        outcome.File!.ParentId.Should().Be(folderId);
        (await TitlesInAsync(folderId)).Should().Contain(fileName + ".docx");
        (await TitlesInAsync(roomId)).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(NonEntities))]
    public async Task EntityIdThatNamesNothing_FallsBackToThePortalScope(string entityIdJson)
    {
        await RequireGeneratorsAsync();
        var (tool, folderId) = await StartAsync();
        var roomId = await CreateCustomRoomAsync("Autotest entity room");
        var fileName = RandomName();

        // Raw: an entityId that is null or an object, which the typed (string) request cannot carry.
        var reply = await CallRawAsync(BodyJson(
            JsonSerializer.Serialize(DocxTool),
            JsonSerializer.Serialize(GeneratorArguments(tool, fileName)),
            entityIdJson));

        reply.Status.Should().Be(200, reply.ToString());
        var outcome = OutcomeOf(reply);
        outcome.IsError.Should().BeFalse(outcome.Raw);
        outcome.File!.ParentId.Should().Be(folderId);
        (await TitlesInAsync(folderId)).Should().Contain(fileName + ".docx");
        (await TitlesInAsync(roomId)).Should().BeEmpty();
    }

    /// <summary>
    /// A room the caller may not create files in is a denial, not a value to fall back from: nothing lands in the
    /// room, in the caller's My documents or in the owner's. A DocSpaceAdmin outside the room is refused like the
    /// Files API refuses them.
    /// </summary>
    [Theory]
    [MemberData(nameof(NonGuestRoles))]
    public async Task RoomTheCallerCannotCreateFilesIn_IsRefusedAndNothingIsCreatedAnywhere(EmployeeType type)
    {
        await RequireGeneratorsAsync();
        var (tool, ownerMy) = await StartAsync();
        var roomId = await CreateCustomRoomAsync("Autotest owner room");
        await ActAsAsync(type);
        var memberMy = await MyFolderIdAsync();

        // Control: a value that names no room is a fallback, so the generator works for this role. Without it
        // "nothing was created" could just mean the generator is broken for them.
        var control = RandomName("Autotest control");
        var fallback = await RunAsync(DocxTool, GeneratorArguments(tool, control), MissingEntityId);
        fallback.File!.ParentId.Should().Be(memberMy, fallback.Raw);

        var blocked = RandomName("Autotest blocked");
        var denied = await CallToolAsync(DocxTool, GeneratorArguments(tool, blocked), roomId.ToString());

        (await TitlesInAsync(memberMy)).Should().NotContain(blocked + ".docx", "the refused call did not fall back to the member's My documents");
        await ActAsOwnerAsync();
        (await TitlesInAsync(ownerMy)).Should().NotContain(blocked + ".docx", "nor to the owner's");
        (await TitlesInAsync(roomId)).Should().BeEmpty();
        denied.Error.Should().Be(UnknownToolName, denied.ToString());
        denied.Status.Should().Be(400, denied.ToString());
    }

    [Fact]
    public async Task MemberWithContentCreatorAccess_MayScopeAGeneratorIntoTheRoom()
    {
        await RequireGeneratorsAsync();
        var (tool, _) = await StartAsync();
        var roomId = await CreateCustomRoomAsync("Autotest shared room");
        var member = await InviteContact(EmployeeType.User, TestContext.Current.CancellationToken);
        await RoomsApi.SetRoomSecurityAsync(
            roomId,
            new RoomInvitationRequest([new RoomInvitation(member.Id, FileShare.ContentCreator)], notify: false),
            TestContext.Current.CancellationToken);
        await _aiChatClient.Authenticate(member);
        await FilesHttpClient.Authenticate(member);
        var fileName = RandomName();

        var outcome = await RunAsync(DocxTool, GeneratorArguments(tool, fileName), roomId.ToString());

        outcome.IsError.Should().BeFalse(outcome.Raw);
        outcome.File!.ParentId.Should().Be(roomId);
        (await TitlesInAsync(roomId)).Should().Contain(fileName + ".docx");
    }
}
