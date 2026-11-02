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
/// <c>POST /api/2.0/ai/text-to-docx</c> into a folder the caller cannot create files in: somebody else's My Documents
/// or a room they are not a member of. Neither the Owner nor a DocSpaceAdmin has any special standing here. Every
/// refusal is checked against the folder too, since a 403 that still wrote the file would be the same defect.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Messages")]
public class MessagesPermissionForeignFolderTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    [Fact]
    public async Task TextToDocx_Member_CannotExportIntoTheOwnersMyDocuments()
    {
        var ownerFolderId = await MyDocumentsOfAsync(Owner);
        var before = await StableFileIdsAsync(Owner, ownerFolderId);

        var member = await AddMemberAsync(EmployeeType.User);

        var exception = await ExportRefusedAsAsync(member, BuildExport(ownerFolderId, ExportTitle(), "hello"));

        await AssertRefusedAsync(exception, Owner, ownerFolderId, before);
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(null)]
    public async Task TextToDocx_OwnerOrAdmin_CannotExportIntoAMembersMyDocuments(EmployeeType? callerType)
    {
        var member = await AddMemberAsync(EmployeeType.User);
        var caller = callerType is null ? Owner : await AddMemberAsync(callerType.Value);

        var memberFolderId = await MyDocumentsOfAsync(member);
        var before = await StableFileIdsAsync(member, memberFolderId);

        var exception = await ExportRefusedAsAsync(caller, BuildExport(memberFolderId, ExportTitle(), "hello"));

        await AssertRefusedAsync(exception, member, memberFolderId, before);
    }

    // An invisible room is refused, not hidden behind a 404. It is read back as the Owner: the member cannot see it,
    // so only the Owner's listing can say whether anything was created.
    [Fact]
    public async Task TextToDocx_NonMember_CannotExportIntoARoom()
    {
        var roomId = await CreateCustomRoomAsync("Autotest TextToDocx Closed Room");
        var controlId = await CreateFileAsAsync(Owner, roomId, "Autotest Control");
        var member = await AddMemberAsync(EmployeeType.User);

        var exception = await ExportRefusedAsAsync(member, BuildExport(roomId, ExportTitle(), "hello"));

        await AssertRefusedAsync(exception, Owner, roomId, [controlId]);
    }

    private async Task AssertRefusedAsync(ApiException exception, User reader, int folderId, List<int> before)
    {
        await WaitForExportToSettleAsync();

        IdsOf(await ListFilesAsync(reader, folderId)).Should().Equal(before, "nothing was created");
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Forbidden");
    }
}
