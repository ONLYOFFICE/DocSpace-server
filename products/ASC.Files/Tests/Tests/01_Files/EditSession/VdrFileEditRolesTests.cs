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

namespace ASC.Files.Tests.Tests._01_Files.EditSession;

/// <summary>
/// Editing a document of a Virtual Data Room by role. The file's security flags give Comment, Review and Edit to a
/// Room manager, a Content creator and an Editor and none of them to a Viewer; an edit session opens for the same
/// three roles and is refused with 403 for a Form Filler and a Viewer.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrFileEditRolesTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>VDR role and whether it comments, reviews and edits a document.</summary>
    public static TheoryData<FileShare, bool> EditFlagsByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, true },
        { FileShare.Editing, true },
        { FileShare.Read, false }
    };

    /// <summary>VDR role and whether it opens an edit session on a document.</summary>
    public static TheoryData<FileShare, bool> EditSessionByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, true },
        { FileShare.Editing, true },
        { FileShare.FillForms, false },
        { FileShare.Read, false }
    };

    [Theory]
    [MemberData(nameof(EditFlagsByRole))]
    public async Task GetFileInfo_VdrDocumentRole_CommentReviewAndEditFlagsMatchRole(FileShare access, bool allowed)
    {
        // Arrange
        var (member, file) = await CreateVdrFileWithMember(access);

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Security.Read.Should().BeTrue();
        info.Security.Comment.Should().Be(allowed);
        info.Security.Review.Should().Be(allowed);
        info.Security.Edit.Should().Be(allowed);
    }

    [Theory]
    [MemberData(nameof(EditSessionByRole))]
    public async Task CreateEditSession_VdrDocumentRole_OnlyEditingRolesOpenSession(FileShare access, bool allowed)
    {
        // Arrange
        var (member, file) = await CreateVdrFileWithMember(access);

        // Act
        await _filesClient.Authenticate(member);

        if (allowed)
        {
            var session = (await _filesApi.CreateEditSessionAsync(file.Id, 1024, TestContext.Current.CancellationToken)).Response;

            // Assert
            session.Success.Should().BeTrue();
        }
        else
        {
            var exception = await Assert.ThrowsAsync<ApiException>(
                async () => await _filesApi.CreateEditSessionAsync(file.Id, 1024, TestContext.Current.CancellationToken));

            // Assert
            exception.ErrorCode.Should().Be(403);
        }
    }

    private async Task<(User Member, FileDto File)> CreateVdrFileWithMember(FileShare access)
    {
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom($"Autotest VDR Edit {access}");
        var file = await CreateFile("Autotest VDR Edit.docx", room.Id);
        await InviteToRoom(room.Id, member, access);

        return (member, file);
    }
}
