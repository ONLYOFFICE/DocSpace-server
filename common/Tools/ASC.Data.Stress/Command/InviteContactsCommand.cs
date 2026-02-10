using DocSpace.API.SDK.Api.Portal;

namespace ASC.Data.Stress.Command;

public class InviteContactsCommand : AsyncCommand<InviteContactsCommand.Settings>, IBaseCommand
{
    public static string Name => "invite-contacts";
    public static string Description => "Invites contacts in batches";

    public class Settings : CommandSettings
    {
        [CommandOption("--users-per-type")]
        public int UsersPerType { get; set; } = 100;

        [CommandOption("--email")]
        public required string Email { get; set; }

        [CommandOption("--password")]
        public required string Password { get; set; }
        
        public static readonly Settings Default = new()
        {
            Email = "test@onlyoffice.com",
            Password = "11111111"
        };
    }
    
    public override ValidationResult Validate(CommandContext context, Settings settings)
    {               
        if (string.IsNullOrEmpty(settings.Email))
        {
            settings.Email = AnsiConsole.Ask("Enter user [green]email[/]:", Settings.Default.Email);
        }  
        
        if (string.IsNullOrEmpty(settings.Password))
        {
            settings.Password = AnsiConsole.Ask("Enter user [green]password[/]:", Settings.Default.Password);
        }
        
        
        return ValidationResult.Success();
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        
        var configuration = await ApiHelper.GetConfigurationAsync(settings.Email, settings.Password);
        var  usersApi = new UsersApi(configuration);
        
        var token = CancellationToken.None;
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {                
                var ctxTask = ctx.AddTask($"[green]Invite contacts[/]");
                var contacts = await InviteContactsInBatches(ctxTask, factory, usersApi, configuration, settings, token);
                AnsiConsole.MarkupLine($"[green]Created {contacts.Count} users[/]");
            });
        
        return 0;
    }

    private static async Task<List<User>> InviteContactsInBatches(ProgressTask progressTask, IHttpClientFactory factory, UsersApi usersApi, Configuration configuration, Settings settings,  CancellationToken token)
    {
        var member = new Faker<MemberRequestDto>()
            .CustomInstantiator(r=> new MemberRequestDto())
            .RuleFor(x => x.FirstName, f => f.Person.FirstName)
            .RuleFor(x => x.LastName, f => f.Person.LastName)
            .RuleFor(x => x.Email, f => f.Person.Email)
            .RuleFor(x => x.Password, f => f.Internet.Password());

        var result = new List<User>();

        List<Task<User>> tasks = [];
        var j = 0;

        var employeeTypes = new List<EmployeeType> { EmployeeType.RoomAdmin, EmployeeType.DocSpaceAdmin, EmployeeType.User};
        
        foreach (var empl in employeeTypes)
        {
            foreach (var _ in Enumerable.Range(1, settings.UsersPerType))
            {
                tasks.Add(InviteContact(member, empl));
                j++;

                if (j == 100)
                {
                    result.AddRange(await Task.WhenAll(tasks));
                    tasks.Clear();
                    j = 0;
                }
            }
        }

        return result;

        async Task<User> InviteContact(Faker<MemberRequestDto> fakerMember, EmployeeType employeeType)
        {
            using var profilesApi = new ProfilesApi(configuration);
            var shortLink = (await usersApi.GetInvitationLinkAsync(employeeType, token)).Response;
            using var client = factory.CreateClient();

            var fullLink = await client.GetAsync(shortLink, token);
            var confirmHeader = fullLink.RequestMessage?.RequestUri?.Query.Substring(1);
            if (confirmHeader == null)
            {
                throw new HttpRequestException($"Unable to get confirmation link for {employeeType}");
            }

            profilesApi.Configuration.DefaultHeaders.Add("confirm", confirmHeader);

            var parsedQuery = HttpUtility.ParseQueryString(confirmHeader);
            if(!Enum.TryParse(parsedQuery["emplType"], out EmployeeType parsedEmployeeType))
            {
                parsedEmployeeType = EmployeeType.Guest;
            }

            var fakeMember = fakerMember.Generate();

            try
            {
                var createMemberResponse = await profilesApi.AddMemberWithHttpInfoAsync(new MemberRequestDto
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
                }, token);

                profilesApi.Configuration.DefaultHeaders.Remove("confirm");

                if (createMemberResponse.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpRequestException($"Unable to invite user {employeeType}");
                }
                progressTask.Increment(100.0 / (employeeTypes.Count * settings.UsersPerType));
                return new User(fakeMember.Email, fakeMember.Password)
                {
                    Id = createMemberResponse.Data.Response.Id
                };
            }
            catch (ApiException e)
            {
                AnsiConsole.WriteException(e);
            }

            return new User("", "");
        }
    }
}
