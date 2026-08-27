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

namespace ASC.Files.Tests.Tests._02_Folders.Upload;

/// <summary>
/// <c>POST /api/2.0/files/@my/upload</c> - access control. Every portal member has their own My
/// Documents section; a Guest has none.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Upload")]
public class UploadToMyPermissionsTests(
    AspireAppFixture fixture)
    : UploadTestBase(fixture)
{
    [Fact]
    public async Task UploadToMy_Owner_Returns200()
    {
        var uploaded = await UploadToMyAsync("Autotest file content"u8.ToArray(), "autotest-my-owner.txt");

        uploaded.Should().ContainSingle();
    }

    [Theory]
    [InlineData(EmployeeType.DocSpaceAdmin)]
    [InlineData(EmployeeType.RoomAdmin)]
    [InlineData(EmployeeType.User)]
    public async Task UploadToMy_Member_UploadsToOwnMyDocuments_Returns200(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _filesClient.Authenticate(member);

        var uploaded = await UploadToMyAsync("Autotest file content"u8.ToArray(), $"autotest-my-{employeeType}.txt");

        uploaded.Should().ContainSingle();
    }

    [Fact]
    public async Task UploadToMy_Guest_Returns404()
    {
        var guest = await InviteMember(EmployeeType.Guest);
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToMyAsync("Autotest file content"u8.ToArray(), "autotest-my-guest.txt"));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task UploadToMy_Anonymous_Returns401()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await UploadToMyAsync("Autotest file content"u8.ToArray(), "autotest-my-anon.txt"));

        exception.ErrorCode.Should().Be(401);
    }
}
