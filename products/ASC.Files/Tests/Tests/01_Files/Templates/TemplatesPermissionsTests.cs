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

namespace ASC.Files.Tests.Tests._01_Files.Templates;

/// <summary>
/// Who may call <c>POST /files/templates</c> and <c>DELETE /files/templates</c>. Templates are a
/// personal, per-user list rather than a room resource, so every authenticated member type - not
/// just room participants - can manage their own list; only an unauthenticated caller and a guest
/// are rejected.
/// </summary>
[Trait("Category", "Permissions")]
[Trait("Feature", "Templates")]
public class TemplatesPermissionsTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task AddTemplates_Unauthenticated_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.AddTemplatesAsync(
                new TemplatesRequestDto([1]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task AddTemplates_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.AddTemplatesAsync(
                new TemplatesRequestDto([1]), TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task AddTemplates_User_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Templates Perm User Room");
        var file = await CreateFile("Autotest Templates Perm User File", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);

        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task AddTemplates_DocSpaceAdmin_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Templates Perm Admin Room");
        var file = await CreateFile("Autotest Templates Perm Admin File", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);

        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task AddTemplates_Owner_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Templates Perm Owner Room");
        var file = await CreateFile("Autotest Templates Perm Owner File", room.Id);

        // Act
        var result = await _filesApi.AddTemplatesAsync(
            new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTemplates_Unauthenticated_Returns401()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteTemplatesAsync([1], TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>
    /// BUG 81274: recorded against this endpoint's guest handling in the source TS suite. The
    /// product does reject the guest with 403, which is what this test asserts; if the underlying
    /// defect resurfaces, this test goes red and the trait links straight back to the bug.
    /// </summary>
    [Trait("Bug", "81274")]
    [Fact]
    public async Task DeleteTemplates_Guest_Forbidden()
    {
        // Arrange
        var guest = await InviteGuest();
        await _filesClient.Authenticate(guest);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesApi.DeleteTemplatesAsync([1], TestContext.Current.CancellationToken));

        exception.ErrorCode.Should().Be(403);
    }

    [Fact]
    public async Task DeleteTemplates_User_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Delete Templates Perm User Room");
        var file = await CreateFile("Autotest Delete Templates Perm User File", room.Id);

        var user = await InviteContact(EmployeeType.User);
        await _filesClient.Authenticate(user);
        await _filesApi.AddTemplatesAsync(new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.DeleteTemplatesAsync([file.Id], TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTemplates_DocSpaceAdmin_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Delete Templates Perm Admin Room");
        var file = await CreateFile("Autotest Delete Templates Perm Admin File", room.Id);

        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);
        await _filesClient.Authenticate(admin);
        await _filesApi.AddTemplatesAsync(new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.DeleteTemplatesAsync([file.Id], TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTemplates_Owner_Succeeds()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Delete Templates Perm Owner Room");
        var file = await CreateFile("Autotest Delete Templates Perm Owner File", room.Id);
        await _filesApi.AddTemplatesAsync(new TemplatesRequestDto([file.Id]), TestContext.Current.CancellationToken);

        // Act
        var result = await _filesApi.DeleteTemplatesAsync([file.Id], TestContext.Current.CancellationToken);

        // Assert
        result.Response.Should().BeTrue();
    }
}
