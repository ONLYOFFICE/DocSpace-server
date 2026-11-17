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

namespace ASC.Files.Tests.Tests._04_Security;

/// <summary>
/// The review and form-filling rights a room role gives on a file, as the file's security flags report them: Review
/// for a Reviewer and every role above it, not for a Commentator or a Viewer. On a PDF form only a Form Filler gets
/// FillForms; the roles with editing (Room manager, Content creator, Editor) get Edit instead, and a Reviewer, a
/// Commentator or a Viewer gets neither. A Room manager is a Room admin.
/// </summary>
[Trait("Category", "Security")]
public class RoomRoleFileSecurityFlagsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<FileShare, bool> ReviewByRole => new()
    {
        { FileShare.RoomManager, true },
        { FileShare.ContentCreator, true },
        { FileShare.Editing, true },
        { FileShare.Review, true },
        { FileShare.Comment, false },
        { FileShare.Read, false }
    };

    /// <summary>Room type, role, whether the role fills the PDF form, whether it edits it.</summary>
    public static TheoryData<RoomType, FileShare, bool, bool> FillFormsByRole => new()
    {
        { RoomType.VirtualDataRoom, FileShare.RoomManager, false, true },
        { RoomType.VirtualDataRoom, FileShare.ContentCreator, false, true },
        { RoomType.VirtualDataRoom, FileShare.Editing, false, true },
        { RoomType.VirtualDataRoom, FileShare.FillForms, true, false },
        { RoomType.VirtualDataRoom, FileShare.Read, false, false },
        { RoomType.CustomRoom, FileShare.Editing, false, true },
        { RoomType.CustomRoom, FileShare.Review, false, false },
        { RoomType.CustomRoom, FileShare.Comment, false, false },
        { RoomType.CustomRoom, FileShare.Read, false, false }
    };

    [Theory]
    [MemberData(nameof(ReviewByRole))]
    public async Task GetFileInfo_CustomRoomRole_ReviewFlagMatchesRole(FileShare access, bool canReview)
    {
        // Arrange
        var (member, file) = await CreateRoomFileWithMember(RoomType.CustomRoom, access, "Autotest Review Flag.docx");

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Security.Review.Should().Be(canReview);
    }

    [Theory]
    [MemberData(nameof(FillFormsByRole))]
    public async Task GetFileInfo_PdfFormRole_OnlyFormFillerFillsEditingRolesEdit(RoomType roomType, FileShare access, bool canFill, bool canEdit)
    {
        // Arrange
        var (member, file) = await CreateRoomFileWithMember(roomType, access, "Autotest Fill Flag.pdf");

        // Act
        await _filesClient.Authenticate(member);
        var info = (await _filesApi.GetFileInfoAsync(file.Id, cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        info.Security.FillForms.Should().Be(canFill);
        info.Security.Edit.Should().Be(canEdit);
    }

    private async Task<(User Member, FileDto File)> CreateRoomFileWithMember(RoomType roomType, FileShare access, string fileName)
    {
        var member = await InviteMember(access == FileShare.RoomManager ? EmployeeType.RoomAdmin : EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateRoom(new CreateRoomRequestDto($"Autotest Role Flags {roomType} {access}", roomType: roomType));
        var file = await CreateFile(fileName, room.Id);
        await InviteToRoom(room.Id, member, access);

        return (member, file);
    }
}
