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

namespace ASC.Web.Api.Tests.Tests._04_Portal.ModuleEvents;

/// <summary>
/// POST /api/2.0/portal/moduleEvents — what is refused before any module is known (<c>ModuleEventsController</c>).
/// The stand configures no module under <c>plugins:socketEvents:modules</c>, so every key is unknown: a missing or
/// unknown key answers 401 even with a valid body, and a body over 64 KB is cut off by the request size limit
/// before the key is looked at. What needs a configured key (403 for another portal, 400/404 for the addressee,
/// delivery) is out of reach here.
/// </summary>
[Trait("Category", "Portal")]
public class ModuleEventKeyTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private DocSpace.API.SDK.Api.Portal.ModuleEventsApi ModuleEventsApi => new(
        _webApiClient,
        new Configuration { BasePath = _webApiClient.BaseAddress!.ToString().TrimEnd('/') });

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-module-key")]
    public async Task SendModuleEvent_MissingOrUnknownKey_ThrowsUnauthorized(string? key)
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await ModuleEventsApi.SendModuleEventAsync(
                key, new SendModuleEventRequest(user: Owner.Email, @event: "new-message", message: "1"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SendModuleEvent_UnknownKeyWithSignedInOwner_ThrowsUnauthorized()
    {
        // Arrange: the user session does not stand in for the module key
        await _webApiClient.Authenticate(Owner);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await ModuleEventsApi.SendModuleEventAsync(
                "not-a-module-key", new SendModuleEventRequest(room: "inbox", @event: "new-message"), TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(401);
    }

    [Fact]
    public async Task SendModuleEvent_BodyOver64Kb_ThrowsPayloadTooLarge()
    {
        // Arrange
        await _webApiClient.Authenticate(null);

        // Act
        var exception = await Assert.ThrowsAsync<ApiException>(
            async () => await ModuleEventsApi.SendModuleEventAsync(
                "not-a-module-key",
                new SendModuleEventRequest(room: "inbox", @event: "new-message", message: new string('x', 70 * 1024)),
                TestContext.Current.CancellationToken));

        // Assert
        exception.ErrorCode.Should().Be(413);
    }
}
