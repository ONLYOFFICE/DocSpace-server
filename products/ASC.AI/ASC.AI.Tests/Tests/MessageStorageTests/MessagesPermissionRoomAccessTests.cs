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


namespace ASC.AI.Tests.Tests.MessageStorageTests;

/// <summary>
/// <c>POST /api/2.0/ai/text-to-docx</c> into an Owner's Custom room, by the access level the caller holds there. The
/// route defers to the folder's "may create files here": it is refused for exactly the levels
/// <c>POST /files/{folderId}/file</c> refuses, and granted for the others.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Messages")]
public class MessagesPermissionRoomAccessTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Theory]
    [InlineData(DocSpace.API.SDK.Model.FileShare.Read)]
    [InlineData(DocSpace.API.SDK.Model.FileShare.Editing)]
    public async Task TextToDocx_RoomMemberWithoutCreateAccess_CannotExportIntoTheRoom(DocSpace.API.SDK.Model.FileShare access)
    {
        var roomId = await CreateCustomRoomAsync("Autotest TextToDocx Room");

        // A file the member may see, so the check at the end is "the room still holds exactly this" rather than
        // "the listing was empty", which is also what an unreadable room would look like.
        var controlId = await CreateFileAsAsync(Owner, roomId, "Autotest Control");

        var member = await AddMemberAsync(EmployeeType.User);
        await ShareRoomAsync(roomId, member, access);

        // The reference behaviour: this access level cannot create a file in the room through the Files API either.
        var direct = await Assert.ThrowsAsync<ApiException>(async () => await CreateFileAsAsync(member, roomId, "Autotest Direct Create"));
        direct.ErrorCode.Should().Be(403);

        var exception = await ExportRefusedAsAsync(member, BuildExport(roomId, ExportTitle(), "hello"));

        // Refused synchronously, so nothing was queued to land later.
        IdsOf(await ListFilesAsync(member, roomId)).Should().Equal(controlId);
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Forbidden");
    }

    // RoomManager goes to a RoomAdmin: a User or a Guest cannot hold it.
    [Theory]
    [InlineData(EmployeeType.User, DocSpace.API.SDK.Model.FileShare.ContentCreator)]
    [InlineData(EmployeeType.RoomAdmin, DocSpace.API.SDK.Model.FileShare.RoomManager)]
    public async Task TextToDocx_RoomMemberWithCreateAccess_ExportsIntoSomeoneElsesRoom(EmployeeType type, DocSpace.API.SDK.Model.FileShare access)
    {
        var roomId = await CreateCustomRoomAsync("Autotest TextToDocx Room");
        var member = await AddMemberAsync(type);
        await ShareRoomAsync(roomId, member, access);
        var title = ExportTitle();

        var response = await ExportAsAsync(member, BuildExport(roomId, title));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        // The room belongs to the Owner, so this also shows the export is not limited to folders the caller owns.
        (await WaitForExportedFileAsync(member, roomId, $"{title}.docx")).Should().NotBeNull($"no \"{title}.docx\" in the room");
    }

    /// <summary>
    /// The route used to check "may the caller create files in this folder" and nothing else, so a Guest holding
    /// Content Creator got a 202 and the document really landed in the room. A Guest is refused everywhere else on
    /// the AI surface (threads, messages, attachments, prompts — how BUG 83237 was resolved); fixed on 2026-08-20 by
    /// checking the user type too. The room listing comes before the status on purpose: a 403 that still wrote the
    /// file would be the same bug under a different status.
    /// </summary>
    [Fact]
    [Trait("Bug", "83256")]
    public async Task TextToDocx_GuestWithContentCreatorAccess_IsRefusedTheExport()
    {
        var roomId = await CreateCustomRoomAsync("Autotest TextToDocx Guest Room");
        var controlId = await CreateFileAsAsync(Owner, roomId, "Autotest Control");

        var guest = await AddMemberAsync(EmployeeType.Guest);
        await ShareRoomAsync(roomId, guest, DocSpace.API.SDK.Model.FileShare.ContentCreator);

        // The premise: this Guest really may create files here, so the refusal below can only be about the user type.
        var directId = await CreateFileAsAsync(guest, roomId, "Autotest Direct Create");

        var title = ExportTitle();
        var exception = await ExportRefusedAsAsync(guest, BuildExport(roomId, title));

        // Refused synchronously, so nothing was queued to land later.
        IdsOf(await ListFilesAsync(guest, roomId)).Should().Equal(
            new[] { controlId, directId }.Order(),
            $"a Guest must not have exported \"{title}.docx\" into the room");
        exception.ErrorCode.Should().Be(403);
    }

    // The author is what "My documents" filters on, what the feed shows and — where a user may touch only their own
    // files — whether they can open what they just saved. A background job that credited the room owner or a system
    // account would be invisible to every test that only asks whether the file exists.
    [Fact]
    public async Task TextToDocx_SavedDocument_IsAuthoredByTheCallerNotByTheRoomOwner()
    {
        var roomId = await CreateCustomRoomAsync("Autotest TextToDocx Author Room");
        var member = await AddMemberAsync(EmployeeType.User);
        await ShareRoomAsync(roomId, member, DocSpace.API.SDK.Model.FileShare.ContentCreator);
        var title = ExportTitle();

        var response = await ExportAsAsync(member, BuildExport(roomId, title));
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var exported = await WaitForExportedFileAsync(member, roomId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in the room");

        await FilesHttpClient.Authenticate(member);
        var info = (await FilesApi.GetFileInfoAsync(exported!.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        info.CreatedBy.Id.Should().Be(member.Id);
    }
}
