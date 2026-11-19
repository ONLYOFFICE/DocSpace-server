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

namespace ASC.Files.Tests.Tests._03_Rooms.ThirdParty;

/// <summary>
/// A third-party identifier (<c>sbox-&lt;account&gt;[-&lt;path&gt;]</c>) whose account number lies beyond the
/// 32-bit range names no connected account, exactly like an in-range number nobody connected. It used to end
/// as 500: the DAO selector read the number with <c>Convert.ToInt32</c>, which threw an
/// <c>OverflowException</c>. Each case compares the answer for the out-of-range number with the answer the same
/// endpoint gives for <c>int.MaxValue</c>, which no account carries either.
/// </summary>
/// <remarks>
/// No storage has to be connected: the number is read before any provider is looked up.
/// </remarks>
[Trait("Category", "Rooms")]
public class ThirdPartyAccountNumberRangeTests(
    AspireAppFixture fixture)
    : ThirdPartyTestBase(fixture)
{
    private const string UnknownInRangeId = "sbox-2147483647";
    private const string OutOfRangeId = "sbox-2147483648";

    public static TheoryData<string> Endpoints =>
    [
        nameof(RoomsApi.GetRoomInfoAsync),
        nameof(RoomsApi.CreateRoomThirdPartyAsync),
        nameof(RoomsApi.PinRoomAsync),
        nameof(RoomsApi.GetRoomLinksAsync),
        nameof(RoomsApi.GetRoomSecurityInfoAsync),
        nameof(RoomsApi.AddRoomTagsAsync),
        nameof(FilesApi.AddFileToRecentAsync),
        nameof(FilesApi.ToggleFileFavoriteAsync)
    ];

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task Call_AccountNumberBeyondInt32_AnsweredLikeUnknownAccount(string endpoint)
    {
        // Arrange
        await _filesClient.Authenticate(Owner);

        var unknown = await Assert.ThrowsAsync<ApiException>(async () => await Call(endpoint, UnknownInRangeId));
        unknown.ErrorCode.Should().BeInRange(400, 499, "an id that names no connected account is the caller's error");

        // Act
        var outOfRange = await Assert.ThrowsAsync<ApiException>(async () => await Call(endpoint, OutOfRangeId));

        // Assert
        outOfRange.ErrorCode.Should().Be(unknown.ErrorCode);
        ErrorMessageOf(outOfRange.ErrorContent!.ToString()).Should().Be(ErrorMessageOf(unknown.ErrorContent!.ToString()));
    }

    private Task Call(string endpoint, string id)
    {
        var ct = TestContext.Current.CancellationToken;

        return endpoint switch
        {
            nameof(RoomsApi.GetRoomInfoAsync) => _roomsApi.GetRoomInfoAsync(id, ct),
            nameof(RoomsApi.CreateRoomThirdPartyAsync) => _roomsApi.CreateRoomThirdPartyAsync(
                id, new CreateThirdPartyRoomRequest(title: "Autotest Account Number Range", roomType: RoomType.CustomRoom), ct),
            nameof(RoomsApi.PinRoomAsync) => _roomsApi.PinRoomAsync(id, ct),
            nameof(RoomsApi.GetRoomLinksAsync) => _roomsApi.GetRoomLinksAsync(id, cancellationToken: ct),
            nameof(RoomsApi.GetRoomSecurityInfoAsync) => _roomsApi.GetRoomSecurityInfoAsync(id, cancellationToken: ct),
            nameof(RoomsApi.AddRoomTagsAsync) => _roomsApi.AddRoomTagsAsync(id, new BatchTagsRequestDto(names: ["Autotest"]), ct),
            nameof(FilesApi.AddFileToRecentAsync) => _filesApi.AddFileToRecentAsync(id, ct),
            nameof(FilesApi.ToggleFileFavoriteAsync) => _filesApi.ToggleFileFavoriteAsync(id, true, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null)
        };
    }

    /// <summary>The <c>error.message</c> of an error body, or an empty string when it has none.</summary>
    private static string ErrorMessageOf(string? body)
    {
        using var json = JsonDocument.Parse(string.IsNullOrEmpty(body) ? "{}" : body);

        return json.RootElement.TryGetProperty("error", out var error)
            && error.ValueKind == JsonValueKind.Object
            && error.TryGetProperty("message", out var message)
            && message.ValueKind == JsonValueKind.String
                ? message.GetString() ?? ""
                : "";
    }
}
