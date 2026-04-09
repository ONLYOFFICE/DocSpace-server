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

extern alias ASCWebApi;
extern alias ASCPeople;
using MemberRequestDto = ASCPeople::ASC.People.ApiModels.RequestDto.MemberRequestDto;
using WizardRequestsDto = DocSpace.API.SDK.Model.WizardRequestsDto;

namespace ASC.Files.Tests.Data;

public static class Initializer
{
    public static readonly User Owner = new("test@example.com", "11111111")
    {
        Id = Guid.Parse("66faa6e4-f133-11ea-b126-00ffeec8b4ef")
    };

    private static bool _initialized;
    private static DocSpace.API.SDK.Model.PasswordHasher _passwordHasherSettings = null!;
    private static AspireAppFixture _fixture = null!;

    public static readonly Faker<MemberRequestDto> FakerMember = new Faker<MemberRequestDto>()
        .RuleFor(x => x.FirstName, f => f.Person.FirstName)
        .RuleFor(x => x.LastName, f => f.Person.LastName)
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.Password, f => f.Internet.Password(8, 10));

    public static async Task InitializeAsync(AspireAppFixture fixture)
    {
        _fixture = fixture;

        if (!_initialized)
        {
            var settings = (await fixture.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;

            if (!string.IsNullOrEmpty(settings.WizardToken))
            {
                fixture.WebApiHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

                _passwordHasherSettings = settings.PasswordHash;

                await fixture.CommonSettingsApi.CompleteWizardAsync(new WizardRequestsDto(Owner.Email, GetClientPassword(Owner.Password)), TestContext.Current.CancellationToken);

                fixture.WebApiHttpClient.DefaultRequestHeaders.Remove("confirm");
            }
        }

        await fixture.FilesHttpClient.Authenticate(Owner);
        _ = await fixture.FoldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        if (!_initialized)
        {
            await fixture.BackupTables();
        }

        _initialized = true;
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
