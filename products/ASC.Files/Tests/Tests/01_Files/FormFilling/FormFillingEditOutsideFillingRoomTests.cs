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

namespace ASC.Files.Tests.Tests._01_Files.FormFilling;

/// <summary>
/// <c>PUT files/file/{fileId}/manageformfilling</c> with <c>Edit</c> outside a form-filling room. <c>Edit</c> saves the properties of the form and announces the form to the
/// room in any room, so it requires editing access to the form everywhere.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class FormFillingEditOutsideFillingRoomTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    [Theory]
    [InlineData(RoomType.VirtualDataRoom)]
    [InlineData(RoomType.CustomRoom)]
    public async Task Edit_MemberWithReadAccessOutsideAFormFillingRoom_Returns403(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = roomType == RoomType.VirtualDataRoom
            ? await CreateVDRRoom("Autotest Edit Form Read Room " + Guid.NewGuid().ToString()[..8])
            : await CreateCustomRoom("Autotest Edit Form Read Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        var reader = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, reader, FileShare.Read);
        await _filesClient.Authenticate(reader);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await Manage(form.Id, FormFillingManageAction.Edit));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    /// <summary>
    /// The allowed cell of the same matrix: a member with editing access gets 200 where a reader gets 403, so the 403
    /// above comes from the access check and not from a refusal of every caller.
    /// </summary>
    [Theory]
    [InlineData(RoomType.VirtualDataRoom)]
    [InlineData(RoomType.CustomRoom)]
    public async Task Edit_MemberWithEditingAccessOutsideAFormFillingRoom_IsAccepted(RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = roomType == RoomType.VirtualDataRoom
            ? await CreateVDRRoom("Autotest Edit Form Editor Room " + Guid.NewGuid().ToString()[..8])
            : await CreateCustomRoom("Autotest Edit Form Editor Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFormInRoom(room.Id);

        var editor = await InviteContact(EmployeeType.User);
        await InviteToRoom(room.Id, editor, FileShare.Editing);
        await _filesClient.Authenticate(editor);

        // Act
        var status = (await _filesApi.ManageFormFillingWithHttpInfoAsync(
            form.Id.ToString(),
            new ManageFormFillingDto(form.Id, FormFillingManageAction.Edit),
            TestContext.Current.CancellationToken)).StatusCode;

        // Assert
        ((int)status).Should().Be(200);
    }

    private async Task Manage(int formId, FormFillingManageAction action)
    {
        await _filesApi.ManageFormFillingAsync(
            formId.ToString(),
            new ManageFormFillingDto(formId, action),
            TestContext.Current.CancellationToken);
    }
}
