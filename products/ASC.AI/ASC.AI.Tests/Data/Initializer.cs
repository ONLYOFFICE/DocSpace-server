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

namespace ASC.AI.Tests.Data;

public static class Initializer
{
    public const string OwnerEmail = "test@example.com";
    public const string OwnerPassword = "11111111";

    private static PasswordHasherSettings _passwordHasherSettings = null!;

    // Maps each per-test HttpClient to the WebApi client of the portal it belongs to, so the
    // HttpClient.Authenticate(user) extension can sign in without any shared/ambient state. Entries
    // are weakly held and removed when a test disposes its PortalClients.
    private static readonly ConditionalWeakTable<HttpClient, AiApiClient> _authApis = new();

    public static readonly Faker Faker = new("en");

    /// <summary>
    /// Stores the (machine-key-derived, portal-independent) password-hash settings used to compute
    /// client-side password hashes. Called once by the fixture before any test runs.
    /// </summary>
    internal static void InitializePasswordHasher(PasswordHasherSettings settings)
    {
        _passwordHasherSettings = settings;
    }

    internal static void RegisterAuthApi(HttpClient client, AiApiClient webApi)
    {
        _authApis.AddOrUpdate(client, webApi);
    }

    internal static void UnregisterAuthApi(HttpClient client)
    {
        _authApis.Remove(client);
    }

    public static async ValueTask Authenticate(this HttpClient client, User? user)
    {
        if (user is null)
        {
            client.DefaultRequestHeaders.Authorization = null;
            return;
        }

        if (!_authApis.TryGetValue(client, out var webApi))
        {
            throw new InvalidOperationException(
                "The HttpClient is not associated with a portal. Create clients via AspireAppFixture.CreatePortalAsync.");
        }

        if (user.PasswordHash is null)
        {
            var hashSw = Stopwatch.StartNew();
            user.PasswordHash = GetClientPassword(user.Password);
            Timing.Write($"hash({user.Email})", hashSw.ElapsedMilliseconds);
        }

        var authSw = Stopwatch.StartNew();
        using var response = await webApi.PostAsync(
            "/api/2.0/authentication",
            new
            {
                userName = user.Email,
                passwordHash = user.PasswordHash
            },
            TestContext.Current.CancellationToken);

        var token = await webApi.ReadAsync<AuthTokenResponse>(response, TestContext.Current.CancellationToken);
        Timing.Write($"auth({user.Email})", authSw.ElapsedMilliseconds);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }

    public static string GetClientPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            password = Guid.NewGuid().ToString();
        }

        var salt = new UTF8Encoding(false).GetBytes(_passwordHasherSettings.Salt);

        var hashBytes = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            _passwordHasherSettings.Iterations,
            _passwordHasherSettings.Size / 8);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
