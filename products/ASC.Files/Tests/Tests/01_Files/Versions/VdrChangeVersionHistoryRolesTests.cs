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

namespace ASC.Files.Tests.Tests._01_Files.Versions;

/// <summary>
/// Managing the version history of a document in a Virtual Data Room by role: a Room manager and a Content creator
/// change it; an Editor, a Form Filler and a Viewer get 403.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrChangeVersionHistoryRolesTests(
    AspireAppFixture fixture)
    : VersionsTestBase(fixture)
{
    public static TheoryData<FileShare, bool> ManageHistoryByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, true },
        { FileShare.Editing, false },
        { FileShare.FillForms, false },
        { FileShare.Read, false }
    };

    [Theory]
    [MemberData(nameof(ManageHistoryByRole))]
    public async Task ChangeVersionHistory_VdrDocumentRole_OnlyContentCreatorAndAboveManage(FileShare access, bool allowed)
    {
        // Arrange
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom($"Autotest VDR Version History {access}");
        var file = await CreateFile("Autotest VDR Version History.docx", room.Id);
        await _filesApi.UpdateFileAsync(file.Id, new UpdateFileRequest { LastVersion = 2 }, TestContext.Current.CancellationToken);
        await InviteToRoom(room.Id, member, access);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            var response = (await _filesApi.ChangeVersionHistoryAsync(file.Id, new ChangeHistoryRequest(2, false), TestContext.Current.CancellationToken)).Response;

            // Assert
            response.Should().NotBeNull();
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesApi.ChangeVersionHistoryAsync(file.Id, new ChangeHistoryRequest(2, false), TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403);
        }
    }
}
