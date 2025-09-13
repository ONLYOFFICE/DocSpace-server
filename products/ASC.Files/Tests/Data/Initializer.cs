// (c) Copyright Ascensio System SIA 2009-2025
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
using PasswordHasher = ASC.Security.Cryptography.PasswordHasher;
using WizardRequestsDto = DocSpace.API.SDK.Model.WizardRequestsDto;

namespace ASC.Files.Tests.Data;

public static class Initializer
{
    public static readonly User Owner = new("test@example.com", "11111111");
    
    private static bool _initialized;

    private static PasswordHasher _passwordHasher = null!;
    private static readonly Lock _locker = new();
    private static WepApiFactory _apiFactory = null!;
    private static PeopleFactory _peopleFactory = null!;
    private static FilesServiceFactory _filesServiceFactory = null!;
    private static readonly string _basePath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "..", "..", ".."));
    private static readonly List<KeyValuePair<string, string?>> _settings =
    [
        new("log:dir", Path.Combine(_basePath, "Logs", "Test")),
        new("$STORAGE_ROOT", Path.Combine(_basePath, "Data", "Test")),
        new("web:hub:internal", ""),
        new("core:base-domain", "localhost"),
        new("license:file:path", Path.Combine(_basePath, "Data", "license", "license.lic"))
    ];

    private static readonly Faker<MemberRequestDto> _fakerMember = new Faker<MemberRequestDto>()
        .RuleFor(x => x.FirstName, f => f.Person.FirstName)
        .RuleFor(x => x.LastName, f => f.Person.LastName)
        .RuleFor(x => x.Email, f => f.Person.Email)
        .RuleFor(x => x.Password, f => f.Internet.Password(8, 10));

    public static List<KeyValuePair<string, string?>>? GlobalSettings { get; private set; }

    public static void InitSettings(CustomProviderInfo providerInfo, string redisConnectionString, string rabbitMqConnectionString, string openSearchConnectionString)
    {
        lock (_locker)
        {
            if (GlobalSettings is { Count: > 0 })
            {
                return;
            }

            GlobalSettings =
            [
                .._settings,
                new KeyValuePair<string, string?>("ConnectionStrings:default:connectionString", providerInfo.ConnectionString()),
                new KeyValuePair<string, string?>("ConnectionStrings:default:providerName", providerInfo.ProviderFullName),
                new KeyValuePair<string, string?>("testAssembly", $"ASC.Migrations.{providerInfo.Provider}.SaaS")
            ];

            var redisSplit = redisConnectionString.Split(':');
            var redisHost = redisSplit[0];
            var redisPort = redisSplit[1];

            GlobalSettings.Add(new KeyValuePair<string, string?>("Redis:Hosts:0:Host", redisHost));
            GlobalSettings.Add(new KeyValuePair<string, string?>("Redis:Hosts:0:Port", redisPort));

            var rabbitMqSettings = new Uri(rabbitMqConnectionString);
            var rabbitMqUserInfo = rabbitMqSettings.UserInfo.Split(':');
            GlobalSettings.Add(new KeyValuePair<string, string?>("RabbitMQ:Hostname", rabbitMqSettings.Host));
            GlobalSettings.Add(new KeyValuePair<string, string?>("RabbitMQ:Port", rabbitMqSettings.Port.ToString()));
            GlobalSettings.Add(new KeyValuePair<string, string?>("RabbitMQ:UserName", rabbitMqUserInfo[0]));
            GlobalSettings.Add(new KeyValuePair<string, string?>("RabbitMQ:Password", rabbitMqUserInfo[1]));

            var openSearchSplit = openSearchConnectionString.Split(':');
            var openSearchHost = openSearchSplit[0];
            var openSearchPort = openSearchSplit[1];

            GlobalSettings.Add(new KeyValuePair<string, string?>("elastic:Scheme", "http"));
            GlobalSettings.Add(new KeyValuePair<string, string?>("elastic:Host", openSearchHost));
            GlobalSettings.Add(new KeyValuePair<string, string?>("elastic:Port", openSearchPort));
        }
    }
    
    public static async Task InitializeAsync(
        FilesApiFactory filesFactory,
        WepApiFactory apiFactory,
        PeopleFactory peopleFactory,
        FilesServiceFactory filesServiceFactory)
    {
        _passwordHasher = filesFactory.Services.GetRequiredService<PasswordHasher>();
        
        if (!_initialized)
        {        
            _apiFactory = apiFactory;
            _peopleFactory = peopleFactory;
            _filesServiceFactory = filesServiceFactory;
            var settings  = (await apiFactory.CommonSettingsApi.GetPortalSettingsAsync(cancellationToken: TestContext.Current.CancellationToken)).Response;
            
            if (!string.IsNullOrEmpty(settings.WizardToken))
            {
                apiFactory.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", settings.WizardToken);

                await apiFactory.CommonSettingsApi.CompleteWizardAsync(new WizardRequestsDto(Owner.Email, _passwordHasher.GetClientPassword(Owner.Password)), TestContext.Current.CancellationToken);
                
                apiFactory.HttpClient.DefaultRequestHeaders.Remove("confirm");
            }
        }
        
        await filesFactory.HttpClient.Authenticate(Owner);
        _ = await filesFactory.FoldersApi.GetRootFoldersAsync(cancellationToken: TestContext.Current.CancellationToken);

        if (!_initialized)
        {
            await filesFactory.BackupTables();
        }

        _initialized = true;
    }
    
    internal static async Task<User> InviteContact(EmployeeType employeeType)
    {
        await _apiFactory.HttpClient.Authenticate(Owner);

        var shortLink = (await _apiFactory.PortalUsersApi.GetInvitationLinkAsync(employeeType, TestContext.Current.CancellationToken)).Response;
        var fullLink = await _apiFactory.HttpClient.GetAsync(shortLink);
        var confirmHeader = fullLink.RequestMessage?.RequestUri?.Query.Substring(1);
        if (confirmHeader == null)
        {
            throw new HttpRequestException($"Unable to get confirmation link for {employeeType}");

        }
        
        await _peopleFactory.HttpClient.Authenticate(Owner);
        _peopleFactory.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("confirm", confirmHeader);
        
        var parsedQuery = HttpUtility.ParseQueryString(confirmHeader);
        if(!Enum.TryParse(parsedQuery["emplType"], out EmployeeType parsedEmployeeType))
        {
            parsedEmployeeType = EmployeeType.Guest;
        }
        
        var fakeMember = _fakerMember.Generate();
        
        var createMemberResponse = await _peopleFactory.PeopleProfilesApi.AddMemberWithHttpInfoAsync(new DocSpace.API.SDK.Model.MemberRequestDto
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
        }, TestContext.Current.CancellationToken);
        
        _peopleFactory.HttpClient.DefaultRequestHeaders.Remove("confirm");
        
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
        
        var authMe = await _apiFactory.AuthenticationApi.AuthenticateMeAsync(new AuthRequestsDto
        {
            UserName = user.Email,
            PasswordHash = _passwordHasher.GetClientPassword(user.Password)
        }, TestContext.Current.CancellationToken);
        
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authMe.Response.Token);
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