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

namespace ASC.Files.Tests.Tests._01_Files.Create;

/// <summary>
/// Who can create an HTML file in their own My Documents section via
/// <c>POST /files/@my/html</c>.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Files")]
public class HtmlFileCreatePermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task CreateHtmlFileInMyDocuments_Owner_ReturnsOk()
    {
        await _filesClient.Authenticate(Owner);

        var result = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Owner", "<p>Owner content</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest HTML My Docs Owner.html");
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_DocSpaceAdmin_ReturnsOk()
    {
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        var result = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Admin", "<p>Admin content</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest HTML My Docs Admin.html");
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_RoomAdmin_ReturnsOk()
    {
        var roomAdmin = await InviteContact(EmployeeType.RoomAdmin);
        await _filesClient.Authenticate(roomAdmin);

        var result = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Room Admin", "<p>Room admin content</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest HTML My Docs Room Admin.html");
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_User_ReturnsOk()
    {
        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        var result = (await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs User", "<p>User content</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken)).Response;

        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("Autotest HTML My Docs User.html");
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_Guest_Returns404()
    {
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Guest", "<p>Guest content</p>", createNewIfExist: true),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateHtmlFileInMyDocuments_Unauthenticated_Returns401()
    {
        await _filesClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(async () => await _filesApi.CreateHtmlFileInMyDocumentsAsync(
            new CreateTextOrHtmlFile("Autotest HTML My Docs Anon", createNewIfExist: true),
            TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }
}
