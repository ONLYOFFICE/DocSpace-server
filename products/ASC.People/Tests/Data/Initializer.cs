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

extern alias ASCPeople;
using MemberRequestDto = ASCPeople::ASC.People.ApiModels.RequestDto.MemberRequestDto;
using WizardRequestsDto = DocSpace.API.SDK.Model.WizardRequestsDto;

namespace ASC.People.Tests.Data;

public static class Initializer
{
    public static readonly User Owner = new("test@example.com", "11111111");

    private static bool _initialized;
    private static PasswordHasher _passwordHasherSettings = null!;
    private static AspireAppFixture _fixture = null!;

    internal static readonly Faker<MemberRequestDto> FakerMember = new Faker<MemberRequestDto>()
        .RuleFor(x => x.FirstName, f => f.Person.FirstName)
        .RuleFor(x => x.LastName, f => f.Person.LastName)
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.Password, f => f.Internet.Password(8, 10));

    private static readonly SemaphoreSlim _initLock = new(1, 1);

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

            var settings = (await fixture.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (!string.IsNullOrEmpty(settings.WizardToken))
            {
                fixture.WebApiHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

                _passwordHasherSettings = settings.PasswordHash;

                await fixture.CommonSettingsApi.CompleteWizardAsync(new WizardRequestsDto(Owner.Email, GetClientPassword(Owner.Password)), TestContext.Current.CancellationToken);

                fixture.WebApiHttpClient.DefaultRequestHeaders.Remove("confirm");
            }

            await fixture.BackupTables();

            _initialized = true;
        }
        finally { _initLock.Release(); }
    }

    internal static async Task<User> InviteContact(EmployeeType employeeType, User? user = null)
    {
        user ??= Owner;
        await _fixture.WebApiHttpClient.Authenticate(user);

        var shortLink = (await _fixture.PortalUsersApi.GetInvitationLinkAsync(employeeType, TestContext.Current.CancellationToken)).Response;
        var fullLink = await _fixture.WebApiHttpClient.GetAsync(shortLink);
        var confirmHeader = fullLink.RequestMessage?.RequestUri?.Query.Substring(1);
        if (confirmHeader == null)
        {
            throw new HttpRequestException($"Unable to get confirmation link for {employeeType}");
        }

        await _fixture.PeopleHttpClient.Authenticate(user);
        _fixture.PeopleHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", confirmHeader);

        var parsedQuery = HttpUtility.ParseQueryString(confirmHeader);
        if (!Enum.TryParse(parsedQuery["emplType"], out EmployeeType parsedEmployeeType))
        {
            parsedEmployeeType = EmployeeType.Guest;
        }

        var fakeMember = FakerMember.Generate();

        var createMemberResponse = await _fixture.ProfilesApi.AddMemberWithHttpInfoAsync(new DocSpace.API.SDK.Model.MemberRequestDto
        {
            FromInviteLink = true,
            CultureName = "en-US",
            Spam = false,

            Email = fakeMember.Email,
            Password = fakeMember.Password,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName,

            Type = parsedEmployeeType,
            Key = parsedQuery["key"] ?? "",
        }, TestContext.Current.CancellationToken);

        _fixture.PeopleHttpClient.DefaultRequestHeaders.Remove("confirm");

        if (createMemberResponse.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException($"Unable to invite user {employeeType}");
        }

        return new User(fakeMember.Email, fakeMember.Password)
        {
            Id = createMemberResponse.Data.Response.Id
        };
    }

    public static async ValueTask Authenticate(this HttpClient client, User? user)
    {
        if (user == null)
        {
            client.DefaultRequestHeaders.Authorization = null;
            return;
        }

        user.PasswordHash ??= GetClientPassword(user.Password);

        var authMe = await _fixture.AuthenticationApi.AuthenticateMeAsync(new AuthRequestsDto
        {
            UserName = user.Email,
            PasswordHash = user.PasswordHash
        }, TestContext.Current.CancellationToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authMe.Response.Token);
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

        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
    }

    private static string Password(
        this Internet internet,
        int minLength,
        int maxLength,
        bool includeUppercase = true,
        bool includeNumber = false,
        bool includeSymbol = false) {

        ArgumentNullException.ThrowIfNull(internet);
        ArgumentOutOfRangeException.ThrowIfLessThan(minLength, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, minLength);

        var r = internet.Random;
        var s = "";

        s += r.Char('a', 'z').ToString();
        if (s.Length < maxLength)
        {
            if (includeUppercase)
            {
                s += r.Char('A', 'Z').ToString();
            }
        }

        if (s.Length < maxLength)
        {
            if (includeNumber)
            {
                s += r.Char('0', '9').ToString();
            }
        }

        if (s.Length < maxLength)
        {
            if (includeSymbol)
            {
                s += r.Char('!', '/').ToString();
            }
        }

        if (s.Length < minLength)
        {
            s += r.String2(minLength - s.Length);                // pad up to min
        }

        if (s.Length < maxLength)
        {
            s += r.String2(r.Number(0, maxLength - s.Length));   // random extra padding in range min..max
        }

        var chars         = s.ToArray();
        var charsShuffled = r.Shuffle(chars).ToArray();

        return new string(charsShuffled);
    }
}
