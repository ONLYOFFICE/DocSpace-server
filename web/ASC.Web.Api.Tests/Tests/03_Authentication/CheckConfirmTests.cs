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


namespace ASC.Web.Api.Tests.Tests._03_Authentication;

/// <summary>
/// POST /api/2.0/authentication/confirm — validation of a confirmation link key.
/// </summary>
[Trait("Category", "Authentication")]
public class CheckConfirmTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Bug 84502: type=EmpInvite without <c>emplType</c> answered 403 with the raw .NET message
    /// "Nullable object must have a value." — <c>EmailValidationKeyModelHelper.ValidateAsync</c>
    /// cast the nullable employee type with no null check (the LinkInvite branch uses
    /// <c>emplType ?? default</c>). The correct status is not specified anywhere, so the test only
    /// asserts what is clearly wrong: an unhandled exception message in the body and a 403 for a
    /// request that is merely incomplete.
    /// </summary>
    [Trait("Bug", "84502")]
    [Fact]
    public async Task CheckConfirm_EmpInviteWithoutEmployeeType_DoesNotSurfaceInternalException()
    {
        // Arrange
        await _webApiClient.Authenticate(null);
        var request = new CheckConfirmRequestDto(key: "garbage", email: "someone@example.com", type: ConfirmType.EmpInvite);

        // Act
        int status;
        string content;

        try
        {
            var response = await _authenticationApi.CheckConfirmWithHttpInfoAsync(request, TestContext.Current.CancellationToken);
            status = (int)response.StatusCode;
            content = response.RawContent ?? "";
        }
        catch (ApiException exception)
        {
            status = exception.ErrorCode;
            content = exception.ErrorContent?.ToString() ?? "";
        }

        // Assert
        content.Should().NotContain("Nullable object must have a value");
        status.Should().NotBe(403);
    }
}
