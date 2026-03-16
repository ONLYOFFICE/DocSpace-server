namespace ASC.Data.Stress.Command;

public class ExecuteFileCreationAndSharingCommand : AsyncCommand<ExecuteFileCreationAndSharingCommand.Settings>, IBaseCommand
{
    public static string Name => "create-and-share";
    public static string Description => "Creates files and shares them";

    public class Settings : CommandSettings
    {
        [CommandOption("--user-id")] 
        public string UserId { get; set; } = "1545484b-766b-461a-849a-43d45d5f8018";

        public static Settings Default = new()
        {
            Iterations = 100,
            Email = "test@onlyoffice.com",
            Password = "11111111"
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
        
        if (settings.Iterations == 0)
        {
            settings.Iterations = AnsiConsole.Ask("Enter number of [green]iterations[/]:", Settings.Default.Iterations);
        }
        
        return ValidationResult.Success();
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configuration = await ApiHelper.GetConfigurationAsync(settings.Email, settings.Password);
        
        using var filesApi = new FilesApi(configuration);
        using var foldersApi = new FoldersApi(configuration);
        using var sharingApi = new SharingApi(configuration);
        
        var system = new Faker().System;
        var token = CancellationToken.None;
        var userId = Guid.Parse(settings.UserId);

        var rootFolder = (await foldersApi.GetRootFoldersAsync(cancellationToken: token)).Response;
        var userFolder = rootFolder.FirstOrDefault(r => r.Current.RootFolderType is FolderType.USER)!.Current.Id;

        await ExecuteFileCreationAndSharing(filesApi, sharingApi, system, userFolder, userId, settings.Iterations, token);
        return 0;
    }

    private static async Task ExecuteFileCreationAndSharing(FilesApi filesApi, SharingApi sharingApi, Bogus.DataSets.System system, int userFolder, Guid user2, int iterations1, CancellationToken token)
    {
        var p = 100.0 / iterations1;
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {        
                var ctxTask = ctx.AddTask("[green]Create and share file[/]");
                
                List<Task> tasks = [];

                var j = 0;
                foreach (var i in Enumerable.Range(1, iterations1))
                {
                    tasks.Add(CreateAndShareFile(user2));
                    j++;
                    if (j == 100)
                    {
                        await Task.WhenAll(tasks);
                        tasks.Clear();
                        j = 0;
                    }
                }

                async Task CreateAndShareFile(Guid guid)
                {
                    var file = (await filesApi.CreateFileAsync(userFolder, new CreateFileJsonElement(system.FileName("docx")), cancellationToken: token)).Response;

                    await filesApi.AddFileToRecentAsync(file.Id, cancellationToken: token);

                    var shareInfo1 = new List<FileShareParams>
                    {
                        new() { ShareTo = guid, Access = FileShare.Read }
                    };
                    var securityRequest1 = new SecurityInfoSimpleRequestDto
                    {
                        Share = shareInfo1
                    };

                    await sharingApi.SetFileSecurityInfoAsync(file.Id, securityRequest1, cancellationToken: token);
                    ctxTask.Increment(p);
                }
            });
    }
}