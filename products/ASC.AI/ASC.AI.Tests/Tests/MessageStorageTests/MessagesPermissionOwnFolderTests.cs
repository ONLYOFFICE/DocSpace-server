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
/// Permissions of <c>POST /api/2.0/ai/text-to-docx</c> (which replaced the removed per-message export) for a
/// caller's own folder. The one rule the route enforces is that the caller may create files in the target folder;
/// the user type matters only for a Guest, who has no access to the AI stack at all.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "AI/Messages")]
public class MessagesPermissionOwnFolderTests(AspireAppFixture fixture) : MessagesPart3TestBase(fixture)
{
    // The member's own folder, not the Owner's: writing into somebody else's My Documents is refused for everyone, so
    // it could not show whether the user type may call the route at all.
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task TextToDocx_Member_ExportsIntoTheirOwnMyDocuments(EmployeeType type)
    {
        var member = await AddMemberAsync(type);
        var folderId = await MyDocumentsOfAsync(member);
        var title = ExportTitle();

        var response = await ExportAsAsync(member, BuildExport(folderId, title));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        response.Data.Success.Should().BeTrue();

        var exported = await WaitForExportedFileAsync(member, folderId, $"{title}.docx");
        exported.Should().NotBeNull($"no \"{title}.docx\" in the {type}'s My Documents");
        exported!.FileExst.Should().Be(".docx");
    }

    // Pinned because it is why the Guest case of the target-folder suite has to go through a room.
    [Fact]
    public async Task GetMyFolder_Guest_HasNoMyDocumentsToExportInto()
    {
        var guest = await AddMemberAsync(EmployeeType.Guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await MyDocumentsOfAsync(guest));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task TextToDocx_Anonymous_Returns401()
    {
        var folderId = await MyDocumentsOfAsync(Owner);

        var exception = await ExportRefusedAsAsync(null, BuildExport(folderId, ExportTitle(), "hello"));

        exception.ErrorCode.Should().Be(401);
        exception.ErrorContent?.ToString().Should().Contain("Unauthorized");
    }
}
