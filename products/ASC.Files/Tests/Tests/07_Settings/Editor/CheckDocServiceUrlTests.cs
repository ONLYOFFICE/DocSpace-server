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

namespace ASC.Files.Tests.Tests._07_Settings.Editor;

/// <summary>
/// <c>PUT /files/docservice</c> — validation and access control only. A successful PUT is not
/// reachable in this harness: even with an empty <c>docServiceUrl</c> the setting falls back to the
/// configured default, so <c>DocumentServiceConnector.CheckDocServiceUrlAsync</c> always issues a
/// live HEAD request to a document server the integration-test AppHost does not run
/// (<c>products/ASC.Files/Core/Services/DocumentService/DocumentServiceConnector.cs:221</c>).
/// The positive-path tests from the TypeScript suite are deliberately not ported.
/// </summary>
[Trait("Category", "Settings")]
public class CheckDocServiceUrlTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private static CheckDocServiceUrlRequestDto EmptyUrlRequest(bool? sslVerification = null) =>
        new(docServiceUrl: "", docServiceSslVerification: sslVerification);

    
    public static TheoryData<EmployeeType> DeniedRoles =>
    [
        EmployeeType.RoomAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    [Fact]
    public async Task CheckDocServiceUrl_Anonymous_Unauthorized()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.CheckDocServiceUrlAsync(EmptyUrlRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Theory]
    [MemberData(nameof(DeniedRoles))]
    public async Task CheckDocServiceUrl_DeniedRole_Forbidden(EmployeeType employeeType)
    {
        // Arrange
        var member = employeeType == EmployeeType.Guest ? await InviteGuest() : await InviteContact(employeeType);
        await _filesClient.Authenticate(member);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.CheckDocServiceUrlAsync(EmptyUrlRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    [Fact]
    public async Task CheckDocServiceUrl_TerminatedDocSpaceAdmin_Unauthorized()
    {
        // Arrange
        var admin = await InviteContact(EmployeeType.DocSpaceAdmin);

        // Sign the admin in while they are still active: the test is about the token going dead once
        // the account is terminated, so it has to be issued before the status change.
        await _filesClient.Authenticate(admin);

        await _peopleClient.Authenticate(Owner);
        await _userStatusApi.UpdateUserStatusAsync(
            EmployeeStatus.Terminated,
            new UpdateMembersRequestDto([admin.Id], resendAll: false),
            TestContext.Current.CancellationToken);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _filesSettingsApi.CheckDocServiceUrlAsync(EmptyUrlRequest(), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }
}
