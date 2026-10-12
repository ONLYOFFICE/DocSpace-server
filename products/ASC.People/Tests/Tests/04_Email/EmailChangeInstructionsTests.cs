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
/// POST /people/email — functional coverage: who can be told to change their email, and what
/// happens on the wire. The endpoint behaves differently depending on whether the caller is a
/// DocSpace admin: an admin changes the target's email immediately (no confirmation step), while
/// anyone else only triggers a notification to the target. Both paths return the same message.
/// </summary>
public class EmailChangeInstructionsTests(AspireAppFixture fixture) : BaseTest(fixture)
{
    private const string SuccessMessage = "The email change instructions have been successfully sent";

    public static readonly TheoryData<EmployeeType?, EmployeeType?> AllowedPairs = new()
    {
        // Owner (admin) targeting someone else — the email changes immediately.
        { null, EmployeeType.DocSpaceAdmin },
        { null, EmployeeType.RoomAdmin },
        { null, EmployeeType.User },

        // DocSpace admin targeting self or someone else — same immediate-change path.
        { EmployeeType.DocSpaceAdmin, null },
        { EmployeeType.DocSpaceAdmin, EmployeeType.RoomAdmin },
        { EmployeeType.DocSpaceAdmin, EmployeeType.User },

        // Non-admins may only target themselves — this only sends a notification.
        { EmployeeType.RoomAdmin, null },
        { EmployeeType.User, null },
        { EmployeeType.Guest, null },
    };

    /// <summary>
    /// The one case where the caller is also the target and is an admin (the Owner): the change is
    /// applied immediately, so the test proves it end to end by signing in with the new address and
    /// then restoring the original one for portal cleanup.
    /// </summary>
    [Fact]
    public async Task SendEmailChangeInstructions_OwnerToSelf_ChangesEmailImmediately()
    {
        var originalEmail = Owner.Email;
        var newEmail = Initializer.FakerMember.Generate().Email;

        await _peopleClient.Authenticate(Owner);

        var result = await _emailApi.SendEmailChangeInstructionsAsync(
            new UpdateMemberRequestDto(userId: Owner.Id.ToString(), email: newEmail),
            TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Be(SuccessMessage);

        // The email changed immediately — sign-in with the new address must work.
        var ownerWithNewEmail = new User(newEmail, Owner.Password) { Id = Owner.Id };
        await _peopleClient.Authenticate(ownerWithNewEmail);

        // Restore the original email so the rest of the suite (and portal cleanup) keeps working.
        var restored = await _emailApi.SendEmailChangeInstructionsAsync(
            new UpdateMemberRequestDto(userId: Owner.Id.ToString(), email: originalEmail),
            TestContext.Current.CancellationToken);

        restored.StatusCode.Should().Be(200);

        await _peopleClient.Authenticate(Owner, forceRefresh: true);
    }

    [Theory]
    [MemberData(nameof(AllowedPairs))]
    public async Task SendEmailChangeInstructions_ForAllowedPairs_ReturnsSuccessMessage(EmployeeType? viewerType, EmployeeType? targetType)
    {
        var viewer = viewerType is null ? Owner : await InviteMember(viewerType.Value);
        var target = targetType is null ? viewer : await InviteMember(targetType.Value);

        await _peopleClient.Authenticate(viewer);

        var result = await _emailApi.SendEmailChangeInstructionsAsync(
            new UpdateMemberRequestDto(userId: target.Id.ToString(), email: Initializer.FakerMember.Generate().Email),
            TestContext.Current.CancellationToken);

        result.StatusCode.Should().Be(200);
        result.Response.Should().Be(SuccessMessage);
    }
}
