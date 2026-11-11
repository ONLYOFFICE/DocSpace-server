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

namespace ASC.People.Tests.Tests._04_Email;

/// <summary>
/// POST /people/email — what the call does to the stored address (a letter only for a non-admin,
/// an immediate change for an administrator) and the target and address checks: a disabled or
/// pending target, an address already taken, the address the account already has.
/// </summary>
public class EmailChangeInstructionsRulesTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    [Fact]
    public async Task SendEmailChangeInstructions_NonAdminOnSelf_LeavesStoredAddressUnchanged()
    {
        var member = await InviteContact(EmployeeType.User);
        var newEmail = Initializer.FakerMember.Generate().Email;

        await _peopleClient.Authenticate(member);
        await _emailApi.SendEmailChangeInstructionsAsync(
            new UpdateMemberRequestDto(userId: member.Id.ToString(), email: newEmail),
            TestContext.Current.CancellationToken);

        var self = (await _profilesApi.GetSelfProfileAsync(TestContext.Current.CancellationToken)).Response;
        self.Email.Should().BeEquivalentTo(member.Email);
    }

    [Fact]
    public async Task SendEmailChangeInstructions_AdminOnMember_WritesAddressAndMarksNotActivated()
    {
        var member = await InviteContact(EmployeeType.User);
        var newEmail = Initializer.FakerMember.Generate().Email.ToLowerInvariant();

        await _peopleClient.Authenticate(Owner);
        await _emailApi.SendEmailChangeInstructionsAsync(
            new UpdateMemberRequestDto(userId: member.Id.ToString(), email: newEmail),
            TestContext.Current.CancellationToken);

        var profile = (await _profilesApi.GetProfileByUserIdAsync(member.Id.ToString(), TestContext.Current.CancellationToken)).Response;
        profile.Email.Should().BeEquivalentTo(newEmail);
        profile.ActivationStatus.Should().Be(EmployeeActivationStatus.NotActivated);
    }

    [Fact]
    public async Task SendEmailChangeInstructions_DisabledTarget_ReturnsNotFound()
    {
        var member = await InviteContact(EmployeeType.User);
        await TerminateUser(member);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: member.Id.ToString(), email: Initializer.FakerMember.Generate().Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    public async Task SendEmailChangeInstructions_PendingInvitationTarget_ReturnsNotFound()
    {
        var invited = (await _profilesApi.InviteUsersAsync(
            new InviteUsersRequestDto([new UserInvitationRequestDto { Type = EmployeeType.User, Email = Initializer.FakerMember.Generate().Email }]),
            TestContext.Current.CancellationToken)).Response.Single();

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: invited.Id.ToString(), email: Initializer.FakerMember.Generate().Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
        exception.ErrorContent!.ToString().Should().Contain("The user could not be found");
    }

    [Fact]
    public async Task SendEmailChangeInstructions_AddressOfAnotherAccount_ReturnsBadRequest()
    {
        var member = await InviteContact(EmployeeType.User);
        var other = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: member.Id.ToString(), email: other.Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }

    /// <summary>
    /// The account's own current address is found by the "already taken" lookup before the
    /// admin-only "same address" check is reached, so both paths answer 400.
    /// </summary>
    [Fact]
    public async Task SendEmailChangeInstructions_AdminSendsCurrentAddress_ReturnsBadRequest()
    {
        var member = await InviteContact(EmployeeType.User);

        var exception = await Assert.ThrowsAsync<ApiException>(async () =>
            await _emailApi.SendEmailChangeInstructionsAsync(
                new UpdateMemberRequestDto(userId: member.Id.ToString(), email: member.Email),
                TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
    }
}
