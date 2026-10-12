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

namespace ASC.Web.Api.Tests.Tests._02_Settings.DocsCloud;

/// <summary>
/// Shared assertions for the DocsCloud settings endpoints: every one of them requires
/// <c>EditPortalSettings</c> (Owner or DocSpaceAdmin only) and rejects an anonymous caller before
/// the request even reaches the controller, so every permission test in this folder has the same
/// two shapes.
/// </summary>
public abstract class DocsCloudTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Authenticates as the given non-admin role and asserts the call is rejected with 403 Access denied.
    /// </summary>
    protected async Task ExpectAccessDeniedAsync(EmployeeType employeeType, Func<Task> action)
    {
        var member = await InviteMember(employeeType);
        await _webApiClient.Authenticate(member);

        var exception = await Assert.ThrowsAsync<ApiException>(() => action());

        exception.ErrorCode.Should().Be(403);
        exception.ErrorContent?.ToString().Should().Contain("Access denied");
    }

    /// <summary>
    /// Authenticates as anonymous and asserts the call is rejected with 401 before authorization runs.
    /// </summary>
    protected async Task ExpectUnauthorizedAsync(Func<Task> action)
    {
        await _webApiClient.Authenticate(null);

        var exception = await Assert.ThrowsAsync<ApiException>(() => action());

        exception.ErrorCode.Should().Be(401);
    }
}
