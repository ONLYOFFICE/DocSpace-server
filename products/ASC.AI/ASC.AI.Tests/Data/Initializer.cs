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
    public static readonly User Owner = new("test@example.com", "11111111")
    {
        Id = Guid.Parse("66faa6e4-f133-11ea-b126-00ffeec8b4ef")
    };

    private static bool _initialized;
    private static PasswordHasherSettings _passwordHasherSettings = null!;
    private static AspireAppFixture _fixture = null!;

    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private static readonly Faker _faker = new("en");

    public static async Task InitializeAsync(AspireAppFixture fixture)
    {
        _fixture = fixture;

        await _initLock.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            var settings = await GetPortalSettingsAsync(TestContext.Current.CancellationToken);

            if (!string.IsNullOrEmpty(settings.WizardToken))
            {
                _passwordHasherSettings = settings.PasswordHash!;

                fixture.WebApiHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

                using var wizardResponse = await fixture.WebApi.PutAsync(
                    "/api/2.0/settings/wizard/complete",
                    new
                    {
                        email = Owner.Email,
                        passwordHash = GetClientPassword(Owner.Password)
                    },
                    TestContext.Current.CancellationToken);

                fixture.WebApiHttpClient.DefaultRequestHeaders.Remove("confirm");

                wizardResponse.EnsureSuccessStatusCode();
            }
            else if (_passwordHasherSettings is null)
            {
                _passwordHasherSettings = settings.PasswordHash!;
            }

            await fixture.AiHttpClient.Authenticate(Owner);

            await fixture.FilesHttpClient.Authenticate(Owner);
            using (var rootFoldersResponse = await fixture.FilesApi.GetAsync(
                "/api/2.0/files/@root",
                TestContext.Current.CancellationToken))
            {
                rootFoldersResponse.EnsureSuccessStatusCode();
            }

            await fixture.BackupTables();

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public static async Task<User> InviteContactAsync(EmployeeType employeeType, CancellationToken cancellationToken)
    {
        await _fixture.WebApiHttpClient.Authenticate(Owner);

        using var shortLinkResponse = await _fixture.WebApi.GetAsync(
            $"/api/2.0/portal/users/invite/{employeeType}",
            cancellationToken);
        var shortLink = await _fixture.WebApi.ReadAsync<string>(shortLinkResponse, cancellationToken);

        using var fullLinkResponse = await _fixture.WebApiHttpClient.GetAsync(shortLink, cancellationToken);
        var confirmHeader = fullLinkResponse.RequestMessage?.RequestUri?.Query.TrimStart('?');
        if (string.IsNullOrEmpty(confirmHeader))
        {
            throw new HttpRequestException($"Unable to get confirmation link for {employeeType}");
        }

        var parsedQuery = HttpUtility.ParseQueryString(confirmHeader);
        if (!Enum.TryParse(parsedQuery["emplType"], out EmployeeType parsedEmployeeType))
        {
            parsedEmployeeType = EmployeeType.Guest;
        }

        var email = _faker.Person.Email;
        var firstName = _faker.Person.FirstName;
        var lastName = _faker.Person.LastName;
        var password = _faker.Internet.Password(10, false);

        await _fixture.PeopleHttpClient.Authenticate(Owner);
        _fixture.PeopleHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", confirmHeader);

        using var createResponse = await _fixture.PeopleApi.PostAsync(
            "/api/2.0/people",
            new
            {
                fromInviteLink = true,
                cultureName = "en-US",
                spam = false,
                email,
                password,
                firstName,
                lastName,
                type = (int)parsedEmployeeType,
                key = parsedQuery["key"] ?? string.Empty
            },
            cancellationToken);

        _fixture.PeopleHttpClient.DefaultRequestHeaders.Remove("confirm");

        var created = await _fixture.PeopleApi.ReadAsync<CreatedUserDto>(createResponse, cancellationToken);

        return new User(email, password)
        {
            Id = created.Id
        };
    }

    public static async ValueTask Authenticate(this HttpClient client, User? user)
    {
        if (user is null)
        {
            client.DefaultRequestHeaders.Authorization = null;
            return;
        }

        user.PasswordHash ??= GetClientPassword(user.Password);

        using var response = await _fixture.WebApi.PostAsync(
            "/api/2.0/authentication",
            new
            {
                userName = user.Email,
                passwordHash = user.PasswordHash
            },
            TestContext.Current.CancellationToken);

        var token = await _fixture.WebApi.ReadAsync<AuthTokenResponse>(response, TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
    }

    private static async Task<WizardSettingsResponse> GetPortalSettingsAsync(CancellationToken cancellationToken)
    {
        using var response = await _fixture.WebApi.GetAsync(
            "/api/2.0/settings?withPassword=true",
            cancellationToken);
        return await _fixture.WebApi.ReadAsync<WizardSettingsResponse>(response, cancellationToken);
    }

    private static string GetClientPassword(string password)
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

    private class CreatedUserDto
    {
        public Guid Id { get; init; }
    }
}
