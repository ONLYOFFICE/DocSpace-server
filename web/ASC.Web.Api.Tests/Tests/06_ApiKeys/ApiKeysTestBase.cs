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

namespace ASC.Web.Api.Tests.Tests._06_ApiKeys;

/// <summary>
/// Shared setup for the ApiKeys suites. <c>ApiKeysController</c> (<c>/api/2.0/keys</c>) lives under
/// ASC.People, so <see cref="_apiKeysApi"/> rides <c>_peopleClient</c> — acting as a role means
/// re-authenticating that client, same convention as <c>NotificationsTestBase</c>.
/// </summary>
public abstract class ApiKeysTestBase(
    AspireAppFixture fixture)
    : BaseTest(fixture)
{
    /// <summary>
    /// Authenticates <c>_peopleClient</c> as the given actor, inviting a fresh member first when
    /// the actor is not the portal owner.
    /// </summary>
    protected async Task<User> AuthenticateAsAsync(ApiKeyActor actor)
    {
        var user = actor switch
        {
            ApiKeyActor.Owner => Owner,
            ApiKeyActor.DocSpaceAdmin => await InviteMember(EmployeeType.DocSpaceAdmin),
            ApiKeyActor.RoomAdmin => await InviteMember(EmployeeType.RoomAdmin),
            ApiKeyActor.User => await InviteMember(EmployeeType.User),
            ApiKeyActor.Guest => await InviteMember(EmployeeType.Guest),
            _ => throw new ArgumentOutOfRangeException(nameof(actor), actor, null)
        };

        await _peopleClient.Authenticate(user);
        return user;
    }

    /// <summary>
    /// Runs <paramref name="action"/> with <c>_peopleClient</c> authenticated as the given API key
    /// (<c>Authorization: Bearer &lt;key&gt;</c>) instead of a user token, then restores whatever
    /// identity the client had before — mirrors the TS suite's <c>forApiKey(...)</c>.
    /// </summary>
    protected async Task<T> AsApiKeyAsync<T>(string apiKey, Func<Task<T>> action)
    {
        var original = _peopleClient.DefaultRequestHeaders.Authorization;
        _peopleClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        try
        {
            return await action();
        }
        finally
        {
            _peopleClient.DefaultRequestHeaders.Authorization = original;
        }
    }
}
