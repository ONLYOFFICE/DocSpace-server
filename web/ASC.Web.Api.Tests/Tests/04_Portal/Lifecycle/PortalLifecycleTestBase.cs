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

namespace ASC.Web.Api.Tests.Tests._04_Portal.Lifecycle;

/// <summary>
/// What the lifecycle classes share: the system API, which has no SDK and is called raw, and polling of an
/// answer that changes once Web.Api sees a status saved by another process through its tenant cache.
/// </summary>
public abstract class PortalLifecycleTestBase(AspireAppFixture fixture) : BaseTest(fixture)
{
    /// <summary>
    /// Calls the system API and returns its status and JSON body. <paramref name="signedIn"/> sends it as this
    /// test's portal with the sign-in of its Web.Api client, which the server-only methods require.
    /// </summary>
    protected async Task<(HttpStatusCode Status, JsonElement Body)> ApiSystemAsync(HttpMethod method, string path, object? body = null, bool signedIn = false)
    {
        // Once: a suspended portal refuses the sign-in itself, and the token taken before keeps working.
        if (signedIn && _webApiClient.DefaultRequestHeaders.Authorization is null)
        {
            await _webApiClient.Authenticate(Owner);
        }

        using var client = Fixture.CreateApiSystemClient(signedIn ? Clients : null);
        using var request = new HttpRequestMessage(method, path);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var json = string.IsNullOrWhiteSpace(text) ? default : JsonDocument.Parse(text).RootElement.Clone();

        return (response.StatusCode, json);
    }

    /// <summary>
    /// Repeats <paramref name="request"/> until it answers <paramref name="expected"/> or 15 seconds pass, and
    /// returns the status of the last answer - the SDK's for a success, its <see cref="ApiException"/>'s otherwise.
    /// </summary>
    protected static async Task<HttpStatusCode> PollAsync(Func<Task> request, HttpStatusCode expected)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);

        while (true)
        {
            HttpStatusCode status;

            try
            {
                await request();
                status = HttpStatusCode.OK;
            }
            catch (ApiException exception)
            {
                status = (HttpStatusCode)exception.ErrorCode;
            }

            if (status == expected || DateTime.UtcNow >= deadline)
            {
                return status;
            }

            await Task.Delay(500, TestContext.Current.CancellationToken);
        }
    }

    /// <summary>A portal name nobody has: lower-case, starts with a letter, 13 characters.</summary>
    protected static string FreePortalName()
    {
        return "t" + Guid.NewGuid().ToString("N")[..12];
    }
}
