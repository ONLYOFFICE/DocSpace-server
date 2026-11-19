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

namespace ASC.Web.Api.Tests.Tests._05_Security.Firebase;

/// <summary>
/// POST /api/2.0/settings/push/docregisterdevice with a Firebase token longer than the 255 characters its column holds.
/// The token used to reach the INSERT, which the stack's MySQL in strict mode refused, so the call returned 500; the
/// request model now refuses it with 400. A token of exactly 255 characters is still registered, so the limit is not
/// stricter than the column.
/// </summary>
[Trait("Category", "Security")]
public class FirebaseTokenLengthTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task DocRegisterDevice_TokenLongerThan255_ThrowsBadRequest()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var request = new FirebaseRequestDto(Token(256), true);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _firebaseApi.DocRegisterPusnNotificationDeviceAsync(request, TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(400);
    }

    [Fact]
    public async Task DocRegisterDevice_TokenOf255_IsRegistered()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        var token = Token(255);

        // Act
        var result = await _firebaseApi.DocRegisterPusnNotificationDeviceAsync(
            new FirebaseRequestDto(token, true), TestContext.Current.CancellationToken);

        // Assert
        result.Response.FirebaseDeviceToken.Should().Be(token);
    }

    /// <summary>A unique token of <paramref name="length"/> characters.</summary>
    private static string Token(int length)
    {
        return Guid.NewGuid().ToString("N").PadRight(length, 'x');
    }
}
