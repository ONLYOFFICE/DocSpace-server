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

namespace ASC.Web.Api.Tests.Tests._02_Settings.Messages;

/// <summary>
/// POST /api/2.0/settings/sendadmmail while the contact form of the sign-in page is switched off: the form does not
/// exist then, and the call says so with 405, as <c>sendjoininvite</c> does for a portal with nothing to join. It threw
/// an unmapped <c>MethodAccessException</c>, and a 500 came back.
/// </summary>
[Trait("Category", "Settings")]
public class SendAdminMailFormOffTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    [Fact]
    public async Task SendAdminMail_ContactFormOff_Returns405()
    {
        // Arrange
        await _webApiClient.Authenticate(Owner);
        await _messagesApi.EnableAdminMessageSettingsAsync(
            new TurnOnAdminMessageSettingsRequestDto(false), TestContext.Current.CancellationToken);
        await _webApiClient.Authenticate(null);
        var email = Initializer.FakerMember.Generate().Email;

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await _messagesApi.SendAdminMailAsync(
                new AdminMessageSettingsRequestDto(message: "Autotest message", email: email), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(405);
        exception.ErrorContent!.ToString().Should().Contain("Method not available");
    }
}
