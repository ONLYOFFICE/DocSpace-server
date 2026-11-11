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

namespace ASC.Files.Tests.Tests._04_Security;

/// <summary>
/// <c>POST /files/share/{key}/password</c> - the attempt limit: wrong passwords for one link from one address are
/// counted, and past the portal's tolerated number (5 by default) the link is blocked with 403, even for the right
/// password, until the block expires.
/// </summary>
[Trait("Category", "Security")]
[Trait("Feature", "Sharing")]
public class ExternalSharePasswordLimitTests(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    private const int DefaultAttemptCount = 5;

    /// <summary>A fresh link password: link passwords take 8 to 30 latin letters only.</summary>
    private static string NewPassword()
    {
        return new string([.. Guid.NewGuid().ToString("N").Take(16).Select(c => (char)('a' + (c % 26)))]);
    }

    [Fact]
    public async Task ApplyExternalSharePassword_MoreWrongAttemptsThanAllowed_BlocksTheLinkWith403()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Password Limit Room");
        var primary = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink;
        var password = NewPassword();
        var link = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(primary.Id, FileShare.Read, linkType: LinkType.External, password: password),
            TestContext.Current.CancellationToken)).Response.SharedLink;
        var wrong = new ExternalShareRequestParam { Password = password + "x" };

        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, link.RequestToken);

        try
        {
            // Act - the tolerated wrong attempts are answered, the next one is refused
            for (var i = 0; i < DefaultAttemptCount; i++)
            {
                var answer = (await _sharingApi.ApplyExternalSharePasswordAsync(link.RequestToken, wrong, TestContext.Current.CancellationToken)).Response;
                answer.Status.Should().Be(ExternalShareStatus.InvalidPassword);
            }

            var blocked = await Assert.ThrowsAsync<ApiException>(
                async () => await _sharingApi.ApplyExternalSharePasswordAsync(link.RequestToken, wrong, TestContext.Current.CancellationToken));
            var blockedRight = await Assert.ThrowsAsync<ApiException>(
                async () => await _sharingApi.ApplyExternalSharePasswordAsync(
                    link.RequestToken, new ExternalShareRequestParam { Password = password }, TestContext.Current.CancellationToken));

            // Assert
            blocked.ErrorCode.Should().Be(403);
            blockedRight.ErrorCode.Should().Be(403);
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }

    [Fact]
    public async Task ApplyExternalSharePassword_RightPassword_IsNotCountedAsAnAttempt()
    {
        // Arrange
        var room = await CreateCustomRoom("Autotest Password Count Room");
        var primary = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink;
        var password = NewPassword();
        var link = (await _roomsApi.SetRoomLinkAsync(
            room.Id,
            new RoomLinkRequest(primary.Id, FileShare.Read, linkType: LinkType.External, password: password),
            TestContext.Current.CancellationToken)).Response.SharedLink;
        var wrong = new ExternalShareRequestParam { Password = password + "x" };
        var right = new ExternalShareRequestParam { Password = password };

        await _filesClient.Authenticate(null);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, link.RequestToken);

        try
        {
            // Act - one wrong attempt short of the limit, then more right ones than the limit leaves room for:
            // if a right password were counted, the second right attempt would already be blocked
            for (var i = 0; i < DefaultAttemptCount - 1; i++)
            {
                (await _sharingApi.ApplyExternalSharePasswordAsync(link.RequestToken, wrong, TestContext.Current.CancellationToken))
                    .Response.Status.Should().Be(ExternalShareStatus.InvalidPassword);
            }

            for (var i = 0; i < DefaultAttemptCount; i++)
            {
                (await _sharingApi.ApplyExternalSharePasswordAsync(link.RequestToken, right, TestContext.Current.CancellationToken))
                    .Response.Status.Should().Be(ExternalShareStatus.Ok);
            }

            var last = (await _sharingApi.ApplyExternalSharePasswordAsync(link.RequestToken, right, TestContext.Current.CancellationToken)).Response;

            // Assert
            last.Status.Should().Be(ExternalShareStatus.Ok);
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
        }
    }
}
