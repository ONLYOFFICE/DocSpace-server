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

namespace ASC.Files.Tests.Tests._01_Files.CopySave;

/// <summary>
/// <c>POST /files/file/{id}/saveaspdf</c> — who may create the PDF. Reading the source and creating
/// in the destination are both checked before the conversion starts, so every refusal below is
/// decided without a document server. The positive "can create" controls of the TS suite are not
/// ported: they need the conversion itself (see <see cref="SaveFileAsPdfTests"/>).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class SaveFileAsPdfPermissionsTests(
    AspireAppFixture fixture)
    : SaveFileAsPdfTestBase(fixture)
{
    [Theory]
    [InlineData(EmployeeType.User, FileShare.Editing)]
    [InlineData(EmployeeType.User, FileShare.Read)]
    [InlineData(EmployeeType.Guest, FileShare.Read)]
    public async Task SaveFileAsPdf_MemberWithoutCreateAccess_Returns403AndCreatesNothing(EmployeeType employeeType, FileShare access)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Save PDF Permissions Room");
        var source = await CreateFile("Autotest Source.docx", room.Id);

        var member = await InviteMember(employeeType);
        await InviteToRoom(room.Id, member, access);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                source.Id,
                new SaveAsPdfRequest(room.Id, "Member"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await GetFileTitles(room.Id)).Should().Equal("Autotest Source.docx");
    }

    [Theory]
    [InlineData(EmployeeType.User)]
    [InlineData(EmployeeType.RoomAdmin)]
    public async Task SaveFileAsPdf_MemberWithoutRoomAccess_Returns403AndCreatesNothing(EmployeeType employeeType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Save PDF No Access Room");
        var source = await CreateFile("Autotest Source.docx", room.Id);

        var member = await InviteMember(employeeType);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                source.Id,
                new SaveAsPdfRequest(room.Id, "Member"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await GetFileTitles(room.Id)).Should().Equal("Autotest Source.docx");
    }

    [Fact]
    public async Task SaveFileAsPdf_Anonymous_Returns401AndCreatesNothing()
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = await CreateCustomRoom("Autotest Save PDF Anonymous Room");
        var source = await CreateFile("Autotest Source.docx", room.Id);

        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                source.Id,
                new SaveAsPdfRequest(room.Id, "Anon"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);

        await _filesClient.Authenticate(Owner);
        (await GetFileTitles(room.Id)).Should().Equal("Autotest Source.docx");
    }

    [Fact]
    public async Task SaveFileAsPdf_FileOfAnotherRoom_Returns403AndCreatesNothing()
    {
        // Arrange - a ContentCreator of room A has no access to the file of room B
        await _filesClient.Authenticate(Owner);
        var roomA = await CreateCustomRoom("Autotest Isolation Room A");
        var roomB = await CreateCustomRoom("Autotest Isolation Room B");
        var sourceB = await CreateFile("Autotest Source B.docx", roomB.Id);

        var member = await InviteMember(EmployeeType.User);
        await InviteToRoom(roomA.Id, member, FileShare.ContentCreator);

        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.SaveFileAsPdfAsync(
                sourceB.Id,
                new SaveAsPdfRequest(roomA.Id, "Stolen"),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        (await GetFileTitles(roomA.Id)).Should().BeEmpty();
        (await GetFileTitles(roomB.Id)).Should().Equal("Autotest Source B.docx");
    }
}
