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
/// GET /files/docservice - reads the document service location, without touching a live document
/// service: <c>GetVersionAsync</c> (behind <c>version=true</c>) swallows any connection failure and
/// falls back to a fixed version string, so this endpoint never needs a running Document Server.
/// </summary>
[Trait("Category", "Settings")]
public class GetDocServiceUrlTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    public static TheoryData<EmployeeType?> AllowedRoles =>
    [
        null, // the portal owner
        EmployeeType.DocSpaceAdmin,
        EmployeeType.RoomAdmin,
        EmployeeType.User,
        EmployeeType.Guest
    ];

    [Fact]
    public async Task GetDocServiceUrl_FreshPortal_IsDefault()
    {
        // Act
        var url = (await _filesSettingsApi.GetDocServiceUrlAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert - nothing has customised the document service location yet
        url.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDocServiceUrl_UrlAndApiUrlAreNonEmptyStrings()
    {
        // Act
        var url = (await _filesSettingsApi.GetDocServiceUrlAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        url.DocServiceUrl.Should().NotBeNullOrEmpty();
        url.DocServiceUrlApi.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetDocServiceUrl_WithVersionTrue_ReturnsVersionString()
    {
        // Act
        var url = (await _filesSettingsApi.GetDocServiceUrlAsync(true, TestContext.Current.CancellationToken)).Response;

        // Assert
        url.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetDocServiceUrl_WithVersionFalse_VersionIsEmpty()
    {
        // Act
        var url = (await _filesSettingsApi.GetDocServiceUrlAsync(false, TestContext.Current.CancellationToken)).Response;

        // Assert
        url.Version.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDocServiceUrl_Anonymous_CanRead()
    {
        // Arrange
        await _filesClient.Authenticate(null);

        // Act
        var url = (await _filesSettingsApi.GetDocServiceUrlAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        url.DocServiceUrl.Should().NotBeNull();
    }

    [Theory]
    [MemberData(nameof(AllowedRoles))]
    public async Task GetDocServiceUrl_EveryRole_CanRead(EmployeeType? employeeType)
    {
        // Arrange
        if (employeeType != null)
        {
            var member = employeeType == EmployeeType.Guest ? await InviteGuest() : await InviteContact(employeeType.Value);
            await _filesClient.Authenticate(member);
        }

        // Act
        var url = (await _filesSettingsApi.GetDocServiceUrlAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

        // Assert
        url.DocServiceUrl.Should().NotBeNull();
    }
}
