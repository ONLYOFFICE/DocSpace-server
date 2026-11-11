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
/// PDF forms of a Virtual Data Room by role. A Viewer and an Editor list a form nobody has configured; a Viewer also
/// lists a configured form it takes no part in. A Content creator configures (assigns participants to) a form the
/// owner created. An Editor, a Viewer and a Form Filler can neither configure a form nor stop its filling (403).
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "VirtualDataRoom")]
public class VdrFormRolesTests(
    AspireAppFixture fixture)
    : FormFillingTestBase(fixture)
{
    public static TheoryData<FileShare> RolesWithoutConfigureRight => new()
    {
        FileShare.Editing,
        FileShare.Read,
        FileShare.FillForms
    };

    [Theory]
    [InlineData(FileShare.Read)]
    [InlineData(FileShare.Editing)]
    public async Task GetFolder_VdrViewerOrEditor_ListsNotConfiguredForm(FileShare access)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access);
        var form = await CreateFormInRoom(room.Id, "Autotest VDR Not Configured.pdf");

        // Act
        await _filesClient.Authenticate(member);
        var titles = await GetFileTitles(room.Id);

        // Assert
        titles.Should().Contain(form.Title);
    }

    [Fact]
    public async Task GetFolder_VdrViewer_ListsConfiguredFormWithoutTakingPart()
    {
        // Arrange
        var (room, viewer) = await CreateVdrWithMember(FileShare.Read);
        var filler = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);
        var form = await CreateFormInRoom(room.Id, "Autotest VDR Configured.pdf");
        await SaveRoleMapping(room.Id, form.Id, filler);

        // Act
        await _filesClient.Authenticate(viewer);
        var titles = await GetFileTitles(room.Id);

        // Assert
        titles.Should().Contain(form.Title);
    }

    [Fact]
    public async Task SaveFormRoleMapping_VdrContentCreator_ConfiguresOwnersForm()
    {
        // Arrange
        var (room, contentCreator) = await CreateVdrWithMember(FileShare.ContentCreator);
        var filler = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);
        var form = await CreateFormInRoom(room.Id, "Autotest VDR CC Configures.pdf");

        // Act
        await _filesClient.Authenticate(contentCreator);
        await SaveRoleMapping(room.Id, form.Id, filler);

        // Assert
        await _filesClient.Authenticate(Owner);
        var roles = (await _filesApi.GetAllFormRolesAsync(form.Id, TestContext.Current.CancellationToken)).Response;
        roles.Should().Contain(r => r.User != null && r.User.Id == filler.Id);
    }

    [Theory]
    [MemberData(nameof(RolesWithoutConfigureRight))]
    public async Task SaveFormRoleMapping_VdrRoleWithoutConfigureRight_Returns403AndFormStaysUnconfigured(FileShare access)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access);
        var filler = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        await InviteToRoom(room.Id, filler, FileShare.FillForms);
        var form = await CreateFormInRoom(room.Id, "Autotest VDR No Configure.pdf");

        // Act
        await _filesClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await SaveRoleMapping(room.Id, form.Id, filler));

        // Assert
        exception.ErrorCode.Should().Be(403);

        await _filesClient.Authenticate(Owner);
        var roles = (await _filesApi.GetAllFormRolesAsync(form.Id, TestContext.Current.CancellationToken)).Response;
        roles.Should().NotContain(r => r.User != null && r.User.Id == filler.Id);
    }

    /// <summary>
    /// The owner configures the form; a Form Filler acting here is the form's participant, so the refusal is about the
    /// stop right and not about seeing the form.
    /// </summary>
    [Theory]
    [MemberData(nameof(RolesWithoutConfigureRight))]
    public async Task StopFormFilling_VdrRoleWithoutStopRight_Returns403(FileShare access)
    {
        // Arrange
        var (room, member) = await CreateVdrWithMember(access);
        var participant = member;

        if (access != FileShare.FillForms)
        {
            participant = await InviteMember(EmployeeType.User);
            await _filesClient.Authenticate(Owner);
            await InviteToRoom(room.Id, participant, FileShare.FillForms);
        }

        await _filesClient.Authenticate(Owner);
        var form = await CreateFormInRoom(room.Id, "Autotest VDR No Stop.pdf");
        await SaveRoleMapping(room.Id, form.Id, participant);

        // Act
        await _filesClient.Authenticate(member);
        var exception = await Assert.ThrowsAsync<ApiException>(async () => await StopFormFilling(form.Id));

        // Assert
        exception.ErrorCode.Should().Be(403);
    }

    private async Task SaveRoleMapping(int roomId, int formId, User participant)
    {
        var roles = new List<FormRoleRequest>
        {
            new()
            {
                UserId = participant.Id,
                RoleName = "Anyone",
                RoomId = roomId,
                RoleColor = "ffefbf",
                Sequence = 1
            }
        };

        await _filesApi.SaveFormRoleMappingAsync(formId.ToString(), new SaveFormRoleMappingDto(formId, roles), TestContext.Current.CancellationToken);
    }

    private async Task<List<string>> GetFileTitles(int folderId)
    {
        var content = (await _foldersApi.GetFolderByFolderIdAsync(folderId, cancellationToken: TestContext.Current.CancellationToken)).Response;

        return (content.Files ?? []).Select(f => f.Title).ToList();
    }

    private async Task<(FolderDto Room, User Member)> CreateVdrWithMember(FileShare access)
    {
        var member = await InviteMember(EmployeeType.User);

        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom($"Autotest VDR Forms {access}");
        await InviteToRoom(room.Id, member, access);

        return (room, member);
    }
}
