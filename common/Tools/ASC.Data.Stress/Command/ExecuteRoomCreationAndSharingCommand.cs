using DocSpace.API.SDK.Api.Rooms;

namespace ASC.Data.Stress.Command;

public class ExecuteRoomCreationAndSharingCommand : AsyncCommand<ExecuteRoomCreationAndSharingCommand.Settings>, IBaseCommand
{
    public static string Name => "create-room-and-share";
    public static string Description => "Creates room and invites to them";

    public class Settings : CommandSettings
    {
        [CommandOption("--second-user-email")] 
        public required string SecondUserEmail { get; set; }

        public static Settings Default = new()
        {
            Iterations = 100,
            Email = "test@onlyoffice.com",
            Password = "11111111",
            SecondUserEmail = "paul.bannov@mail.ru"
        };
        
        [CommandOption("--iterations")]
        public required int Iterations { get; set; }

        [CommandOption("--email")]
        public required string Email { get; set; }

        [CommandOption("--password")]
        public required string Password { get; set; }
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
        
        if (string.IsNullOrEmpty(settings.SecondUserEmail))
        {
            settings.SecondUserEmail = AnsiConsole.Ask("Enter second user [green]email[/]:", Settings.Default.SecondUserEmail);
        }  
        
        if (settings.Iterations == 0)
        {
            settings.Iterations = AnsiConsole.Ask("Enter number of [green]iterations[/]:", Settings.Default.Iterations);
        }
        
        return ValidationResult.Success();
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configuration = await ApiHelper.GetConfigurationAsync(settings.Email, settings.Password);
        
        using var roomsApi = new RoomsApi(configuration);
        using var profilesApi = new ProfilesApi(configuration);
        var secondUser = (await profilesApi.GetProfileByEmailAsync(settings.SecondUserEmail, cancellationToken: cancellationToken)).Response;
        
        var system = new Faker().System;
        var token = CancellationToken.None;

        await ExecuteFileCreationAndSharing(roomsApi, system, secondUser.Id, settings.Iterations, token);
        return 0;
    }

    private static async Task ExecuteFileCreationAndSharing(RoomsApi roomsApi,  Bogus.DataSets.System system, Guid user2, int iterations1, CancellationToken token)
    {
        var p = 100.0 / iterations1;
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {        
                var ctxTask = ctx.AddTask("[green]Create room and invite[/]");
                
                List<Task> tasks = [];

                var j = 0;
                foreach (var i in Enumerable.Range(1, iterations1))
                {
                    tasks.Add(CreateAndShareRoom());
                    j++;
                    if (j == 100)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                        j = 0;
                    }
                }

                return;

                async Task CreateAndShareRoom()
                {
                    var room = (await roomsApi.CreateRoomAsync(new CreateRoomRequestDto(system.CommonFileName(""), roomType: RoomType.CustomRoom), token)).Response;

                    await roomsApi.SetRoomSecurityAsync(room.Id,
                        new RoomInvitationRequest
                        {
                            Invitations = [new RoomInvitation { Id = user2, Access = FileShare.Read }]
                        }, token);
                    
                    ctxTask.Increment(p);
                }
            });
    }
}