// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
