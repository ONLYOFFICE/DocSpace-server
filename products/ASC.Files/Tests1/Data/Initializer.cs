﻿// (c) Copyright Ascensio System SIA 2009-2024
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
using ASC.Migrations.Core.Models;

namespace ASC.Files.Tests1.Data;

public static class Initializer
{
    public static readonly User Owner = new("test@example.com", "11111111");
    
    private static bool _initialized;
    private static HttpClient? _apiClient;
    private static HttpClient? _peopleClient;
    private static PasswordHasher? _passwordHasher;
    
    private static readonly List<KeyValuePair<string, string?>> _settings =
    [
        new("log:dir", Path.Combine("..", "..", "..", "..", "Logs", "Test")),
        new("$STORAGE_ROOT", Path.Combine("..", "..", "..", "..", "Data", "Test")),
        new("web:hub:internal", "")
    ];

    private static readonly Faker<MemberRequestDto> _fakerMember = new Faker<MemberRequestDto>()
        .RuleFor(x => x.FirstName, f => f.Person.FirstName)
        .RuleFor(x => x.LastName, f => f.Person.LastName)
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.Password, f => f.Internet.Password(8, 10));
    
    private static JsonSerializerOptions JsonRequestSerializerOptions { get; } = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
    
    public static List<KeyValuePair<string, string?>> GetSettings(CustomProviderInfo providerInfo, string redisConnectionString, string rabbitMqConnectionString, string openSearchConnectionString)
    {
        var result = new List<KeyValuePair<string, string?>>(_settings)
        {
            new("ConnectionStrings:default:connectionString", providerInfo.ConnectionString()),
            new("ConnectionStrings:default:providerName", providerInfo.ProviderFullName),
            new("testAssembly", $"ASC.Migrations.{providerInfo.Provider}.SaaS")
        };
        
        var redisSplit = redisConnectionString.Split(':');
        var redisHost = redisSplit[0];
        var redisPort = redisSplit[1];
        
        result.Add(new KeyValuePair<string, string?>("Redis:Hosts:0:Host", redisHost));
        result.Add(new KeyValuePair<string, string?>("Redis:Hosts:0:Port", redisPort));
        
        var rabbitMqSettings = new Uri(rabbitMqConnectionString);
        var rabbitMqUserInfo = rabbitMqSettings.UserInfo.Split(':');
        result.Add(new KeyValuePair<string, string?>("RabbitMQ:Hostname", rabbitMqSettings.Host));
        result.Add(new KeyValuePair<string, string?>("RabbitMQ:Port", rabbitMqSettings.Port.ToString()));
        result.Add(new KeyValuePair<string, string?>("RabbitMQ:UserName", rabbitMqUserInfo[0]));
        result.Add(new KeyValuePair<string, string?>("RabbitMQ:Password", rabbitMqUserInfo[1]));
        
        var openSearchSplit = openSearchConnectionString.Split(':');
        var openSearchHost = openSearchSplit[0];
        var openSearchPort = openSearchSplit[1];
        
        result.Add(new KeyValuePair<string, string?>("elastic:Scheme", "http"));
        result.Add(new KeyValuePair<string, string?>("elastic:Host", openSearchHost));
        result.Add(new KeyValuePair<string, string?>("elastic:Port", openSearchPort));
        
        return result;
    }
    
    public static async Task InitializeAsync(FilesApiFactory filesFactory, WebApplicationFactory<WebApiProgram> apiFactory, WebApplicationFactory<PeopleProgram> peopleFactory)
    {
        _passwordHasher = filesFactory.Services.GetRequiredService<PasswordHasher>();
        
        if (!_initialized)
        {
            var apiClientStartTask = Task.Run(() =>
            {
                _apiClient = apiFactory.WithWebHostBuilder(Build).CreateClient();
                _apiClient.BaseAddress = new Uri(_apiClient.BaseAddress, "api/2.0/");
            });

            var peopleClientStartTask = Task.Run(() =>
            {
                _peopleClient = peopleFactory.WithWebHostBuilder(Build).CreateClient();
                _peopleClient.BaseAddress = new Uri(_peopleClient.BaseAddress, "api/2.0/");
            });
            
            await Task.WhenAll(apiClientStartTask, peopleClientStartTask);
            
            var response = await _apiClient.GetAsync("settings");
            var settings = await HttpClientHelper.ReadFromJson<SettingsDto>(response);
            
            if (!string.IsNullOrEmpty(settings?.WizardToken))
            {
                _apiClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

                response = await _apiClient.PutAsJsonAsync("settings/wizard/complete", new WizardRequestsDto
                {
                    Email = Owner.Email, 
                    PasswordHash = _passwordHasher.GetClientPassword(Owner.Password)
                });
                
                _apiClient.DefaultRequestHeaders.Remove("confirm");
                
                _ = await HttpClientHelper.ReadFromJson<WizardSettings>(response);
            }
        }
        
        await filesFactory.HttpClient.Authenticate(Owner);
        
        _ = await filesFactory.HttpClient.GetAsync("@root");

        if (!_initialized)
        {
            await filesFactory.BackupTables();
        }

        _initialized = true;
        return;

        void Build(IWebHostBuilder builder)
        {
            foreach (var setting in GetSettings(filesFactory.ProviderInfo, filesFactory.RedisConnectionString, filesFactory.RabbitMqConnectionString, filesFactory.OpenSearchConnectionString))
            {
                builder.UseSetting(setting.Key, setting.Value);
            }
        }
    }
    
    internal static async Task<User> InviteContact(EmployeeType employeeType)
    {
        await _apiClient.Authenticate(Owner);

        var inviteResponse = await _apiClient.GetAsync($"portal/users/invite/{employeeType}");
        var shortLink = await HttpClientHelper.ReadFromJson<string>(inviteResponse);
        var fullLink = await _apiClient.GetAsync(shortLink);
        var confirmHeader = fullLink.RequestMessage?.RequestUri?.Query.Substring(1);
        if (confirmHeader == null)
        {
            throw new HttpRequestException($"Unable to get confirmation link for {employeeType}");

        }
        
        await _peopleClient.Authenticate(Owner);
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
        }, JsonRequestSerializerOptions);
        
        _peopleClient.DefaultRequestHeaders.Remove("confirm");
        
        if (!createMemberResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Unable to invite user {employeeType}");
        }

        return new User(fakeMember.Email, fakeMember.Password);
    }

    public static async Task Authenticate(this HttpClient client, User user)
    {        
        if (_apiClient != null)
        {
            var authenticationResponse = await _apiClient.PostAsJsonAsync("authentication", new AuthRequestsDto
            {
                UserName = user.Email, 
                PasswordHash = _passwordHasher.GetClientPassword(user.Password)
            }, JsonRequestSerializerOptions);
            var authenticationTokenDto = await HttpClientHelper.ReadFromJson<AuthenticationTokenDto>(authenticationResponse);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authenticationTokenDto?.Token);
        }
    }
    
    internal static string Password(
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