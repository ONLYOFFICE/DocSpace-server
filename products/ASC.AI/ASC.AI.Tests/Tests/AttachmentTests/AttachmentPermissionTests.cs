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

namespace ASC.AI.Tests.Tests.AttachmentTests;

/// <summary>
/// Who may create a draft, and what an anonymous caller gets: <c>save-file</c> is not gated by user type, a Guest has no
/// access to the AI stack at all (how BUG 83237 was resolved), and every route answers 401 before anything else when nobody
/// is signed in.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Attachments")]
public class AttachmentPermissionTests(AspireAppFixture fixture) : AttachmentTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task SaveFile_Member_SavesAFileDraft(EmployeeType type)
    {
        // Not gated by user type, and not gated by room membership either. The backing file is uploaded AS THE MEMBER,
        // into their own Documents — `save-file` resolves `path` against the caller's access, so an owner's file would
        // make this a test of the access check instead. A Guest has no Documents of their own and is covered below.
        var member = await AddMemberAsync(type);

        var id = await SaveDraftAsync(member, $"Autotest {type}.docx", $"saved by {type}");

        await ExpectStoredAsync(id, $"{type}'s own draft");
    }

    [Fact]
    [Trait("Bug", "83237")]
    public async Task SaveFile_GuestCannotAttachAFileSharedWithThem()
    {
        // A Guest has no Documents of their own, and a file draft has to reference a file the caller can read, so the
        // only file a Guest could attach is one shared with them. Even that is a bare `403 {"error":"Forbidden"}`, and
        // the refusal is about the user type, not about access to the file. Filed as BUG 83237 and resolved as the
        // intended behaviour: a Guest has no access to the AI stack.
        //
        // The two premises are the load-bearing part: the Guest reads the room and the file itself, and the owner
        // attaches that same file id in the same portal, so the 403 cannot be a lost invite or an unattachable file.
        var roomId = await CreateCustomRoomAsync("Autotest Guest Share Room");
        var shared = await UploadTextAsync(Owner, roomId.ToString(), "Autotest shared.docx", "shared with a guest");

        await ActAsAsync(Owner);
        var asOwner = await AttachByIdAsync(shared.Id);
        asOwner.Status.Should().Be(200, "the owner attaches the same file id");
        asOwner.Data!.Content.Should().Be("shared with a guest", "the owner gets the file's text");

        var guest = await AddMemberAsync(EmployeeType.Guest);
        await ShareRoomAsync(roomId, guest, DocSpace.API.SDK.Model.FileShare.Read);
        await ActAsAsync(guest);

        var fileRead = await FilesApi.GetFileInfoWithHttpInfoAsync(shared.Id, cancellationToken: TestContext.Current.CancellationToken);
        fileRead.StatusCode.Should().Be(HttpStatusCode.OK, "the Guest can read the shared file itself");

        var refused = await AttachByIdAsync(shared.Id);

        // No draft came back, so there is nothing to poll for: the refusal happens before anything is stored.
        refused.Data.Should().BeNull("a refused Guest gets no draft");
        refused.Error.Should().Be("Forbidden");
        refused.Status.Should().Be(403, "a Guest may not attach a file they can read");
    }

    [Theory]
    [InlineData("save-file")]
    [InlineData("save-files-many")]
    [InlineData("get")]
    [InlineData("get-many")]
    [InlineData("link-to-message")]
    [InlineData("delete")]
    [InlineData("delete-many")]
    public async Task Route_Anonymous_Gets401Unauthorized(string route)
    {
        // save-image / save-images-many are not in the sweep: the routes are gone by design, so every caller gets a bare
        // 404 there, which says nothing about anonymous access.
        var ownerDraft = await SaveDraftAsync(Owner, "Autotest anon target.docx");

        await ActAsAsync(null);
        var status = route switch
        {
            "save-file" => (await SaveFileAsync(SaveRequest("1"))).Status,
            "save-files-many" => (await SaveFilesManyAsync([1])).Status,
            "get" => (await GetAsync(ownerDraft)).Status,
            "get-many" => (await GetManyAsync(ownerDraft)).Status,
            "link-to-message" => (await LinkAsync([ownerDraft], MissingId, MissingId)).Status,
            "delete" => (await DeleteAsync(ownerDraft)).Status,
            _ => (await DeleteManyAsync(ownerDraft)).Status
        };

        status.Should().Be(401, route);

        // None of the refused calls touched the draft.
        await ActAsAsync(Owner);
        await ExpectStoredAsync(ownerDraft, "the owner's draft after an anonymous call");
    }
}
