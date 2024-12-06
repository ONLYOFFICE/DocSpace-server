// (c) Copyright Ascensio System SIA 2009-2024
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
using Bogus;
using Bogus.DataSets;

namespace ASC.Files.Tests1.Data;


public static class Initializer
{
    public static readonly User Owner = new("test@example.com", "11111111");
    
    private static bool _initialized;
    private static HttpClient? _apiClient;
    private static HttpClient? _peopleClient;
    
    private static readonly List<KeyValuePair<string, string?>> _settings =
    [
        new("log:dir", Path.Combine("..", "..", "..", "..", "Logs", "Test")),
        new("$STORAGE_ROOT", Path.Combine("..", "..", "..", "..", "Data", "Test")),
        new("testAssembly", $"ASC.Migrations.MySql.SaaS"),
        new("web:hub:internal", "")
    ];

    private static readonly Faker<MemberRequestDto> _fakerMember = new Faker<MemberRequestDto>()
        .RuleFor(x => x.FirstName, f => f.Person.FirstName)
        .RuleFor(x => x.LastName, f => f.Person.LastName)
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.Password, f => f.Internet.Password(8, 10));
    
    public static List<KeyValuePair<string, string?>> GetSettings(string connectionString)
    {
        var result = new List<KeyValuePair<string, string?>>(_settings)
        {
            new("ConnectionStrings:default:connectionString", connectionString)
        };
        
        return result;
    }
    
    public static async Task InitializeAsync(FilesApiFactory filesFactory, WebApplicationFactory<WebApiProgram> apiFactory, WebApplicationFactory<PeopleProgram> peopleFactory)
    {
        var passwordHasher = filesFactory.Services.GetRequiredService<PasswordHasher>();
        
        if (!_initialized)
        {
            _apiClient = apiFactory.WithWebHostBuilder(builder =>
            {
                foreach (var setting in GetSettings(filesFactory.ConnectionString))
                {
                    builder.UseSetting(setting.Key, setting.Value);
                }
            }).CreateClient();

            _apiClient.BaseAddress = new Uri(_apiClient.BaseAddress, "api/2.0/");
            
            _peopleClient = peopleFactory.WithWebHostBuilder(builder =>
            {
                foreach (var setting in GetSettings(filesFactory.ConnectionString))
                {
                    builder.UseSetting(setting.Key, setting.Value);
                }
            }).CreateClient();

            _peopleClient.BaseAddress = new Uri(_peopleClient.BaseAddress, "api/2.0/");

            var response = await _apiClient.GetAsync("settings");
            var settings = await HttpClientHelper.ReadFromJson<SettingsDto>(response);
            
            if (!string.IsNullOrEmpty(settings?.WizardToken))
            {
                _apiClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

                response = await _apiClient.PutAsJsonAsync("settings/wizard/complete", new WizardRequestsDto { Email = Owner.Email, PasswordHash = passwordHasher.GetClientPassword(Owner.Password) });
                
                _apiClient.DefaultRequestHeaders.Remove("confirm");
                
                _ = await HttpClientHelper.ReadFromJson<WizardSettings>(response);
            }
        }
        
        await Authenticate(filesFactory, filesFactory.HttpClient, Owner.Email, Owner.Password);
        
        _ = await filesFactory.HttpClient.GetAsync("@root");

        if (!_initialized)
        {
            await filesFactory.BackupTables();
        }

        _initialized = true;
    }
    
    internal static async Task<User> InviteContact(FilesApiFactory filesFactory, EmployeeType employeeType)
    {
        await Authenticate(filesFactory, _apiClient, Owner.Email, Owner.Password);

        var inviteResponse = await _apiClient.GetAsync($"portal/users/invite/{employeeType}");
        var shortLink = await HttpClientHelper.ReadFromJson<string>(inviteResponse);
        var fullLink = await _apiClient.GetAsync(shortLink);
        var confirmHeader = fullLink.RequestMessage?.RequestUri?.Query.Substring(1);
        if (confirmHeader == null)
        {
            throw new HttpRequestException($"Unable to get confirmation link for {employeeType}");

        }
        
        await Authenticate(filesFactory, _peopleClient, Owner.Email, Owner.Password);
        _peopleClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", confirmHeader);
        
        var parsedQuery = HttpUtility.ParseQueryString(confirmHeader);
        if(!Enum.TryParse(parsedQuery["emplType"], out EmployeeType parsedEmployeeType))
        {
            parsedEmployeeType = EmployeeType.Guest;
        }
        
        var fakeMember = _fakerMember.Generate();
        
        var createMemberResponse = await _peopleClient.PostAsJsonAsync("people", new MemberRequestDto
        {
            FromInviteLink = true,
            CultureName = "en-US",
            Spam = false,
            
            Email = fakeMember.Email,
            Password = fakeMember.Password,
            FirstName = fakeMember.FirstName,
            LastName = fakeMember.LastName,
            
            Type = parsedEmployeeType,
            Key = parsedQuery["key"],
        }, filesFactory.JsonRequestSerializerOptions);
        _peopleClient.DefaultRequestHeaders.Remove("confirm");
        
        if (!createMemberResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unable to invite user {employeeType}");
        }

        return new User(fakeMember.Email, fakeMember.Password);
    }

    public static async Task Authenticate(FilesApiFactory filesFactory, HttpClient client, string email, string password)
    {        
        var passwordHasher = filesFactory.Services.GetRequiredService<PasswordHasher>();
        if (_apiClient != null)
        {
            var authenticationResponse = await _apiClient.PostAsJsonAsync("authentication", new AuthRequestsDto { UserName = email, PasswordHash = passwordHasher.GetClientPassword(password) }, filesFactory.JsonRequestSerializerOptions);
            var authenticationTokenDto = await HttpClientHelper.ReadFromJson<AuthenticationTokenDto>(authenticationResponse);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationTokenDto?.Token);
        }
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