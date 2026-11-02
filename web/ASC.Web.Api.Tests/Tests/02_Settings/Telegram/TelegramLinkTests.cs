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
namespace ASC.Web.Api.Tests.Tests._02_Settings.Telegram;

/// <summary>
/// GET /api/2.0/settings/telegram/check, GET and DELETE /api/2.0/settings/telegram/link on a portal with no
/// Telegram bot configured, plus the wrong-verb rejections.
/// </summary>
[Trait("Category", "Settings")]
public class TelegramLinkTests(AspireAppFixture fixture) : TelegramTestBase(fixture)
{
    [Fact]
    public async Task CheckTelegram_NoBotConfigured_ReturnsUnlinked()
    {
        // Act
        var status = (await Telegram.CheckTelegramAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        status.Status.Should().Be(RegStatus.unlinked);
        status.Username.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task LinkTelegram_NoBotConfigured_ReturnsEmptyLink()
    {
        // Act
        var link = await Telegram.LinkTelegramAsync(TestContext.Current.CancellationToken);

        // Assert
        link.Response.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task LinkTelegram_CalledTwice_DoesNotFail()
    {
        // Act
        await Telegram.LinkTelegramAsync(TestContext.Current.CancellationToken);
        var second = await Telegram.LinkTelegramAsync(TestContext.Current.CancellationToken);

        // Assert
        second.StatusCode.Should().Be(200);
    }

    // BUG 84692: with no bot configured the returned link is empty (nothing to open in Telegram), yet the call
    // still moves the account from "unlinked" to "linking", a state the user can leave only through unlink.
    [Trait("Bug", "84692")]
    [Fact]
    public async Task LinkTelegram_NoBotConfigured_StatusStaysUnlinked()
    {
        // Act
        var link = await Telegram.LinkTelegramAsync(TestContext.Current.CancellationToken);
        var status = (await Telegram.CheckTelegramAsync(TestContext.Current.CancellationToken)).Response;

        // Assert
        link.Response.Should().BeNullOrEmpty();
        status.Status.Should().Be(RegStatus.unlinked);
    }

    [Fact]
    public async Task UnlinkTelegram_NothingLinked_IsIdempotent()
    {
        // Act
        var first = await Telegram.UnlinkTelegramAsync(TestContext.Current.CancellationToken);
        var second = await Telegram.UnlinkTelegramAsync(TestContext.Current.CancellationToken);

        // Assert
        first.Response.Should().BeTrue();
        second.Response.Should().BeTrue();
    }

    // The typed client has no way to send an unsupported verb, so these go raw.
    [Theory]
    [InlineData("api/2.0/settings/telegram/check", "POST")]
    [InlineData("api/2.0/settings/telegram/check", "PUT")]
    [InlineData("api/2.0/settings/telegram/link", "POST")]
    [InlineData("api/2.0/settings/telegram/link", "PUT")]
    public async Task TelegramEndpoint_UnsupportedVerb_ReturnsMethodNotAllowed(string path, string method)
    {
        // Act
        using var response = method == "POST"
            ? await _webApi.PostAsync(path, null, TestContext.Current.CancellationToken)
            : await _webApi.PutAsync(path, null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }
}
