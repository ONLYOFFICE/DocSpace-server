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

namespace ASC.Files.Tests.Tests._03_Rooms.Links;

/// <summary>
/// <c>GET /files/{fileId}</c> read by an anonymous visitor through a room's external link: the authors of the
/// file must not reach a visitor who has no portal account.
/// </summary>
[Trait("Category", "Rooms")]
public class RoomLinkFileAuthorTests(
    AspireAppFixture fixture)
    : RoomsPermissionsTestBase(fixture)
{
    public static TheoryData<string, RoomType> LinkRoomTypes => new()
    {
        { "Custom", RoomType.CustomRoom },
        { "Public", RoomType.PublicRoom },
        { "FormFilling", RoomType.FillingFormsRoom },
    };

    /// <summary>
    /// A visitor of a shared room link is not a portal member, so the file they open must not name the people who
    /// created, edited, shared or own it: those entries carried the account id and, in the profile link
    /// <c>/accounts/people/filter?search=&lt;email&gt;</c>, the email address of each of them. The visitor follows
    /// the browser's path - the link is opened first, which hands out the anonymous session cookie, and the file
    /// is read with that cookie and the link token.
    /// </summary>
    [Theory]
    [MemberData(nameof(LinkRoomTypes))]
    public async Task GetFileInfo_AnonymousViaRoomLink_HasNoAuthors(string label, RoomType roomType)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);
        var room = roomType switch
        {
            RoomType.PublicRoom => await CreatePublicRoom($"Autotest Link Author {label}"),
            RoomType.FillingFormsRoom => await CreateFillingFormsRoom($"Autotest Link Author {label}"),
            _ => await CreateCustomRoom($"Autotest Link Author {label}")
        };
        var fileId = roomType == RoomType.FillingFormsRoom
            ? await UploadStartedFormAsync(room.Id)
            : (await CreateFile("Autotest Link Author.docx", room.Id)).Id;

        var token = (await _roomsApi.GetRoomsPrimaryExternalLinkAsync(room.Id, cancellationToken: TestContext.Current.CancellationToken)).Response.SharedLink.RequestToken;

        await _filesClient.Authenticate(null);

        var opened = await _sharingApi.GetExternalShareDataWithHttpInfoAsync(token, folderId: room.Id.ToString(), cancellationToken: TestContext.Current.CancellationToken);
        var sessionCookie = opened.Headers.TryGetValue("Set-Cookie", out var cookies)
            ? cookies.Select(c => c.Split(';')[0]).FirstOrDefault(c => c.StartsWith("anonymous_session_key=", StringComparison.Ordinal))
            : null;
        sessionCookie.Should().NotBeNull("opening the link hands an anonymous visitor their session cookie");

        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpRequestExtensions.RequestTokenHeader, token);
        _filesClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", sessionCookie);

        try
        {
            // Act
            var info = (await _filesApi.GetFileInfoAsync(fileId, cancellationToken: TestContext.Current.CancellationToken)).Response;

            // Assert
            info.Id.Should().Be(fileId);
            info.CreatedBy.Should().BeNull();
            info.UpdatedBy.Should().BeNull();
            info.OwnedBy.Should().BeNull();
            info.SharedBy.Should().BeNull();
        }
        finally
        {
            _filesClient.DefaultRequestHeaders.Remove(HttpRequestExtensions.RequestTokenHeader);
            _filesClient.DefaultRequestHeaders.Remove("Cookie");
        }
    }

    /// <summary>
    /// Uploads the shared <c>new.pdf</c> asset - a real ONLYOFFICE PDF form - and starts filling it, since a form
    /// room only shows a started form to a visitor of its link.
    /// </summary>
    private async Task<int> UploadStartedFormAsync(int roomId)
    {
        await using var stream = typeof(RoomLinkFileAuthorTests).Assembly.GetManifestResourceStream("ASC.Files.Tests.Data.new.pdf")!;

        var session = (await _filesOperationsApi.CreateUploadSessionInFolderAsync(
            roomId,
            new SessionRequest("Autotest Link Author.pdf", stream.Length),
            cancellationToken: TestContext.Current.CancellationToken)).Response;

        await _filesOperationsApi.UploadAsyncSessionAsync(roomId, session.Id, 1, new FileParameter(stream), TestContext.Current.CancellationToken);

        var formId = (await _filesOperationsApi.FinalizeSessionAsync(roomId, session.Id, TestContext.Current.CancellationToken)).Response.File.Id;

        await _filesApi.ManageFormFillingAsync(
            formId.ToString(),
            new ManageFormFillingDto(formId, FormFillingManageAction.Start),
            TestContext.Current.CancellationToken);

        return formId;
    }
}
