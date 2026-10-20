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


namespace ASC.Data.Backup.Tests.Tests;

/// <summary>
/// The shared steps of the access-control suites of the backup endpoints: switching the identity of the
/// backup client and asserting the status a refused caller gets. Not a test class itself.
/// </summary>
public abstract class BackupPermissionsTests(AspireAppFixture fixture) : BackupTestBase(fixture)
{
    /// <summary>The text of the 403 answer of every operation that needs the portal settings permission.</summary>
    protected const string AccessDenied = "Access denied";

    /// <summary>Invites a member of the given type into this portal and makes the backup client act as it.</summary>
    protected async Task<User> AuthenticateAsMemberAsync(EmployeeType employeeType)
    {
        var member = await InviteMember(employeeType);
        await _backupClient.Authenticate(member);

        return member;
    }

    /// <summary>Makes the backup client anonymous, runs the call and asserts it is refused with 401.</summary>
    protected async Task ExpectUnauthorizedAsync(Func<Task> call)
    {
        await _backupClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(call);

        exception.ErrorCode.Should().Be(401);
    }

    /// <summary>Runs the call as the current identity and asserts it is refused with 403 and the access-denied text.</summary>
    protected static async Task ExpectForbiddenAsync(Func<Task> call)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(call);

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain(AccessDenied);
    }

    /// <summary>Runs the call as the current identity, asserts the status and returns the exception for further checks.</summary>
    protected static async Task<ApiException> ExpectStatusAsync(Func<Task> call, int status)
    {
        var exception = await Assert.ThrowsAsync<ApiException>(call);

        exception.ErrorCode.Should().Be(status);

        return exception;
    }
}
