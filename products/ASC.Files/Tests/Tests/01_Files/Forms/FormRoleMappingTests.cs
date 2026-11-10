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

namespace ASC.Files.Tests.Tests._01_Files.Forms;

/// <summary>
/// Role-based filling of a PDF form in a virtual data room: <c>formrolemapping</c> stores the roles in
/// the order sent and starts the filling, <c>formroles</c> reports the state of each role along the
/// queue, Stop and Resume mark and clear an interruption, an empty list resets the form, and a member
/// who still holds a role cannot be removed from the room without <c>force</c>.
/// </summary>
/// <remarks>
/// A role counts as submitted when it is stored so (<c>submitted</c> is stored as sent), which puts the
/// queue in any state without a submission from the editor. The current step opens when its holder opens
/// the form (<c>openedit</c>), which records the opening.
/// </remarks>
[Trait("Category", "Features")]
[Trait("Feature", "FormFilling")]
public class FormRoleMappingTests(
    AspireAppFixture fixture)
    : FormsTestBase(fixture)
{
    private sealed record Setup(int RoomId, int FormId, User First, User Second, User Third);

    private async Task<Setup> CreateFormInVdrWithFillers()
    {
        await _filesClient.Authenticate(Owner);
        var room = await CreateVDRRoom("Autotest Role Mapping Room " + Guid.NewGuid().ToString()[..8]);
        var form = await CreateFile("Autotest Role Form.pdf", room.Id);

        var users = new List<User>();

        for (var i = 0; i < 3; i++)
        {
            var user = await InviteMember(EmployeeType.User);
            await InviteToRoom(room.Id, user, FileShare.FillForms);
            users.Add(user);
        }

        return new Setup(room.Id, form.Id, users[0], users[1], users[2]);
    }

    private async Task SaveRoles(Setup setup, params (string Name, User User, bool Submitted)[] roles)
    {
        await _filesApi.SaveFormRoleMappingAsync(
            setup.FormId.ToString(),
            new SaveFormRoleMappingDto(
                setup.FormId,
                roles.Select(r => new FormRoleRequest(roomId: setup.RoomId, roleName: r.Name, roleColor: "ff0000", userId: r.User.Id, submitted: r.Submitted)).ToList()),
            TestContext.Current.CancellationToken);
    }

    private async Task<List<FormRoleDto>> GetRoles(int formId)
    {
        return (await _filesApi.GetAllFormRolesAsync(formId, TestContext.Current.CancellationToken)).Response;
    }

    private async Task Manage(int formId, FormFillingManageAction action)
    {
        await _filesApi.ManageFormFillingAsync(
            formId.ToString(),
            new ManageFormFillingDto(formId, action),
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SaveFormRoleMapping_StartsFillingAndListsTheRolesInTheOrderSent()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();

        // Act
        await SaveRoles(setup, ("Alpha", setup.First, false), ("Beta", setup.Second, false));

        // Assert
        var roles = await GetRoles(setup.FormId);
        roles.Select(r => r.RoleName).Should().Equal("Alpha", "Beta");
        roles.Select(r => r.Sequence).Should().Equal(1, 2);
        roles.Select(r => r.User.Id).Should().Equal(setup.First.Id, setup.Second.Id);
        roles.Should().OnlyContain(r => !r.Submitted);
        roles.Select(r => r.RoleStatus).Should().Equal(FormFillingStatus.YouTurn, FormFillingStatus.Draft);

        var form = (await _filesApi.GetFileInfoAsync(setup.FormId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        form.StartFilling.Should().BeTrue();
    }

    [Fact]
    public async Task SaveFormRoleMapping_WhileFillingRuns_IsRefusedForANewSetAndForAReset()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();
        await SaveRoles(setup, ("Alpha", setup.First, false), ("Beta", setup.Second, false));

        // Act
        var replace = await Assert.ThrowsAsync<ApiException>(async () => await SaveRoles(setup, ("Gamma", setup.Third, false)));
        var reset = await Assert.ThrowsAsync<ApiException>(async () => await SaveRoles(setup));

        // Assert
        replace.ErrorCode.Should().Be(403, "a started form gets new roles only after it was stopped and reset");
        reset.ErrorCode.Should().Be(403, "a running filling is stopped before it is reset");
        (await GetRoles(setup.FormId)).Select(r => r.RoleName).Should().Equal("Alpha", "Beta");
    }

    [Fact]
    public async Task GetAllFormRoles_QueueAfterTheFirstStep_ReportsCompleteYourTurnAndDraft()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();

        // Act
        await SaveRoles(setup, ("Alpha", setup.First, true), ("Beta", setup.Second, false), ("Gamma", setup.Third, false));

        // Assert
        (await GetRoles(setup.FormId)).Select(r => r.RoleStatus).Should().Equal(
            FormFillingStatus.Complete, FormFillingStatus.YouTurn, FormFillingStatus.Draft);
    }

    [Fact]
    public async Task GetAllFormRoles_EveryRoleSubmitted_ReportsCompleteForAll()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();

        // Act
        await SaveRoles(setup, ("Alpha", setup.First, true), ("Beta", setup.Second, true));

        // Assert
        (await GetRoles(setup.FormId)).Should().OnlyContain(r => r.RoleStatus == FormFillingStatus.Complete);
    }

    [Fact]
    public async Task OpenEditFile_ByHolderOfTheCurrentStep_TurnsTheRoleInProgress()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();
        await SaveRoles(setup, ("Alpha", setup.First, true), ("Beta", setup.Second, false), ("Gamma", setup.Third, false));

        // Act
        await _filesClient.Authenticate(setup.Second);
        await _filesApi.OpenEditFileAsync(setup.FormId, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        await _filesClient.Authenticate(Owner);
        (await GetRoles(setup.FormId)).Select(r => r.RoleStatus).Should().Equal(
            FormFillingStatus.Complete, FormFillingStatus.InProgress, FormFillingStatus.Draft);
    }

    [Fact]
    public async Task Stop_MarksTheCurrentRoleStoppedAndResumeClearsTheInterruption()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();
        await SaveRoles(setup, ("Alpha", setup.First, true), ("Beta", setup.Second, false));

        // Act
        await Manage(setup.FormId, FormFillingManageAction.Stop);
        var stopped = await GetRoles(setup.FormId);
        await Manage(setup.FormId, FormFillingManageAction.Resume);
        var resumed = await GetRoles(setup.FormId);

        // Assert
        stopped.Select(r => r.RoleStatus).Should().Equal(FormFillingStatus.Complete, FormFillingStatus.Stoped);
        stopped[1].StopedBy.Should().NotBeNull();
        stopped[1].StopedBy.Id.Should().Be(Owner.Id);
        resumed.Select(r => r.RoleStatus).Should().Equal(FormFillingStatus.Complete, FormFillingStatus.YouTurn);
    }

    [Fact]
    public async Task SaveFormRoleMapping_EmptyListAfterStop_ResetsTheFormAndANewSetReplacesTheOldOne()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();
        await SaveRoles(setup, ("Alpha", setup.First, false), ("Beta", setup.Second, false));
        await Manage(setup.FormId, FormFillingManageAction.Stop);

        // Act
        await SaveRoles(setup);
        var afterReset = await GetRoles(setup.FormId);
        var form = (await _filesApi.GetFileInfoAsync(setup.FormId, cancellationToken: TestContext.Current.CancellationToken)).Response;
        await SaveRoles(setup, ("Gamma", setup.Third, false));

        // Assert
        afterReset.Should().BeEmpty();
        form.StartFilling.Should().NotBe(true);
        var roles = await GetRoles(setup.FormId);
        roles.Should().ContainSingle();
        roles[0].RoleName.Should().Be("Gamma");
        roles[0].User.Id.Should().Be(setup.Third.Id);
        roles[0].RoleStatus.Should().Be(FormFillingStatus.YouTurn);
    }

    [Fact]
    public async Task SetRoomSecurity_RemovingHolderOfARole_IsRefusedWithoutForce()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();
        await SaveRoles(setup, ("Alpha", setup.First, false));

        // Act
        var refused = (await _roomsApi.SetRoomSecurityAsync(
            setup.RoomId,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = setup.First.Id, Access = FileShare.None }], Notify = false },
            TestContext.Current.CancellationToken)).Response;
        var forced = (await _roomsApi.SetRoomSecurityAsync(
            setup.RoomId,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = setup.First.Id, Access = FileShare.None }], Notify = false, Force = true },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        refused.Error.Should().Be(RoomSecurityError.FormRoleBlockingDeletion);
        forced.Error.Should().NotBe(RoomSecurityError.FormRoleBlockingDeletion);

        await _filesClient.Authenticate(setup.First);
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.GetFileInfoAsync(setup.FormId, cancellationToken: TestContext.Current.CancellationToken));
        exception.ErrorCode.Should().Be(403, "the member was removed with force");
    }

    [Fact]
    public async Task SetRoomSecurity_RemovingMemberWithoutARole_IsNotRefused()
    {
        // Arrange
        var setup = await CreateFormInVdrWithFillers();
        await SaveRoles(setup, ("Alpha", setup.First, false));

        // Act
        var result = (await _roomsApi.SetRoomSecurityAsync(
            setup.RoomId,
            new RoomInvitationRequest { Invitations = [new RoomInvitation { Id = setup.Third.Id, Access = FileShare.None }], Notify = false },
            TestContext.Current.CancellationToken)).Response;

        // Assert
        result.Error.Should().NotBe(RoomSecurityError.FormRoleBlockingDeletion);
    }
}
