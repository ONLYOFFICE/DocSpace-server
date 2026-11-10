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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Owner;

/// <summary>
/// POST /api/2.0/settings/owner as a member who holds the portal-settings right. The owner's address of a
/// portal this suite registers is never activated, so the request stops at that check: it is the first one after
/// the right, ahead of the guest and the owner checks.
/// </summary>
[Trait("Category", "Settings")]
public class OwnerChangeInstructionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task SendOwnerChangeInstructions_Owner_UnactivatedOwnerAddress_ReturnsBadRequest(EmployeeType newOwnerType)
    {
        var newOwner = await InviteContact(newOwnerType);
        await _webApiClient.Authenticate(Owner);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.SendOwnerChangeInstructionsAsync(
                new OwnerIdSettingsRequestDto(newOwner.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400);
        exception.ErrorContent!.ToString().Should().Contain("Owner's email is not activated");
    }

    [Fact]
    public async Task SendOwnerChangeInstructions_DocSpaceAdmin_UnactivatedOwnerAddress_ReturnsBadRequest()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        var newOwner = await InviteContact(EmployeeType.User);
        await _webApiClient.Authenticate(admin);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _ownerApi.SendOwnerChangeInstructionsAsync(
                new OwnerIdSettingsRequestDto(newOwner.Id), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(400, "the right is checked first and an admin holds it; the owner's address is checked next");
    }
}
