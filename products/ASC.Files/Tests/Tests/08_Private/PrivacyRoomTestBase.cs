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

namespace ASC.Files.Tests.Tests._08_Privacy;

/// <summary>
/// Shared setup for the privacy-room suites. Deliberately does not inherit
/// <c>RoomsPermissionsTestBase</c> (namespace <c>ASC.Files.Tests.Tests._03_Rooms</c>): the two small
/// helpers this feature needs (<see cref="InviteMember"/>, <see cref="InviteToRoom"/>) are lifted here
/// instead, keeping this feature folder self-contained per the "one feature, one folder" rule.
/// </summary>
public abstract class PrivacyRoomTestBase(AspireAppFixture fixture) : RoomsPermissionsTestBase(fixture)
{
    /// <summary>
    /// Sets a fresh fake encryption key pair for the currently authenticated user, mirroring the
    /// TypeScript suite's fake <c>pk-...</c> / <c>prv-...</c> helper. Real cryptography for this
    /// feature lives only in <c>PrivacyRoomTest.CRUD_UserPrivateKey</c> and the encrypt-upload flow
    /// next to it; every other test only needs a key to exist and be shaped like one.
    /// </summary>
    protected async Task<EncryptionKeyDto> SetFakeKeys(Guid id = default, string publicKeyPrefix = "pk")
    {
        var publicKey = $"{publicKeyPrefix}-{Guid.NewGuid():N}";
        var privateKey = $"prv-{Guid.NewGuid():N}";

        var keys = (await _privacyRoomApi.SetKeysAsync(
            new EncryptionKeyRequestDto(id, publicKey, privateKey),
            TestContext.Current.CancellationToken)).Response;

        return keys.Single(k => k.Id == id);
    }

    /// <summary>
    /// Runs a privacyroom call that may either return normally or throw <see cref="ApiException"/>,
    /// and reduces both outcomes to a single HTTP status code. Several bugs ported from the
    /// TypeScript suite's <c>test.fail</c> cases describe an endpoint that answers 200 today but
    /// must answer a 4xx once fixed; fixing it turns the same call from "returns a response" into
    /// "throws", so the assertion needs to see through both shapes to read red before the fix and
    /// green after it.
    /// </summary>
    protected static async Task<int> StatusOf<T>(Func<Task<ApiResponse<T>>> call)
    {
        try
        {
            var response = await call();

            return (int)response.StatusCode;
        }
        catch (ApiException ex)
        {
            return ex.ErrorCode;
        }
    }

    /// <summary>
    /// Polls <paramref name="probe"/> on a deadline until <paramref name="until"/> is satisfied, for
    /// state that is written asynchronously (key deletion propagating to room-access checks). Never
    /// throws on timeout — it returns the last observed value so the caller's own assertion message
    /// stays readable.
    /// </summary>
    protected static async Task<T> PollUntil<T>(Func<Task<T>> probe, Func<T, bool> until, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(15));
        var last = await probe();

        while (!until(last) && DateTime.UtcNow < deadline)
        {
            await Task.Delay(500, TestContext.Current.CancellationToken);
            last = await probe();
        }

        return last;
    }

    /// <summary>
    /// Sends a raw request to a privacyroom endpoint with a body or path a typed DTO cannot express
    /// (a malformed, non-GUID key id; an empty id collapsing the route).
    /// </summary>
    protected async Task<HttpResponseMessage> SendRawPrivacyRoomRequest(HttpMethod method, string path, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);

        if (json != null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await _filesClient.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
