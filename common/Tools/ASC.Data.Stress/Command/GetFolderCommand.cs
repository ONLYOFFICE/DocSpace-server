using System.Text.RegularExpressions;

namespace ASC.Data.Stress.Command;

public partial class GetFolderCommand : AsyncCommand<GetFolderCommand.Settings>, IBaseCommand
{
    [GeneratedRegex(@"aspnetcore-request-time;dur=(\d+)ms")]
    private static partial Regex MyRegex();
    
    public static string Name => "get-folder";
    public static string Description => "Gets folder multiple times";

    public class Settings : CommandSettings
    {
        public static readonly Settings Default = new()
        {
            Iterations = 100,
            Email = "test@onlyoffice.com",
            Password = "11111111",
            FolderType = DocSpace.API.SDK.Model.FolderType.USER
        };
        
        [CommandOption("--iterations")]
        public required int Iterations { get; set; }

        [CommandOption("--email")]
        public required string Email { get; set; }

        [CommandOption("--password")]
        public required string Password { get; set; }
        
        [CommandOption("--folder-type")]
        public required FolderType? FolderType { get; set; }
    }
    
    public override ValidationResult Validate(CommandContext context, Settings settings)
    {               
        if (settings.FolderType == null)
        {
            var prompt = new SelectionPrompt<FolderType>()
                .Title("Choose Folder Type")
                .AddChoices(FolderType.USER, FolderType.Favorites, FolderType.Recent, FolderType.SHARE, FolderType.VirtualRooms)
                .UseConverter(c =>
                {
                    return c switch
                    {
                        FolderType.USER => "My",
                        FolderType.Favorites => "Favorites",
                        FolderType.Recent => "Recent",
                        FolderType.SHARE => "Share With Me",
                        FolderType.VirtualRooms => "VirtualRooms",
                        _ => "My"
                    };
                });
            
            settings.FolderType = AnsiConsole.Prompt(prompt);
        } 
        
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
        using var foldersApi = new FoldersApi(await ApiHelper.GetConfigurationAsync(settings.Email, settings.Password));
        var token = CancellationToken.None;
        
        var rootFolder = (await foldersApi.GetRootFoldersAsync(cancellationToken: token)).Response;
        var myFolder = rootFolder.FirstOrDefault(r => r.Current.RootFolderType == settings.FolderType)!.Current.Id;
        
        var elapsed = 0;
        
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var ctxTask = ctx.AddTask($"[green]Get {settings.FolderType.ToString()} folder[/]");
                elapsed = await GetFolder(ctxTask, foldersApi, myFolder, settings.Iterations, token);
            });
        
        AnsiConsole.MarkupLine($"Folder [green]{settings.FolderType.ToString()}.[/]");
        AnsiConsole.MarkupLine($"Iterations: [green]{settings.Iterations}.[/]");
        AnsiConsole.MarkupLine($"Total elapsed time in seconds: [green]{elapsed}. [/]");

        var e = Math.Round(elapsed * 1.0 / settings.Iterations, 2);
        var color = e <= 200 ? "green" : "red";
        AnsiConsole.MarkupLine($"Elapsed: [{color}]{e}[/]");
        
        return 0;
    }

    private static async Task<int> GetFolder(ProgressTask progressTask, FoldersApi foldersApi, int recentFolder, int iterations1, CancellationToken token)
    {
        var totalElapsed = 0;
        for (var i = 0; i < iterations1; i++)
        {
            var headers = (await foldersApi.GetFolderByFolderIdWithHttpInfoAsync(recentFolder, count: 100, cancellationToken: token)).Headers;
            
            foreach (var h in headers["Server-Timing"])
            {
                var match = MyRegex().Match(h);
                if (match.Success)
                {
                    totalElapsed += Convert.ToInt32(match.Groups[1].Value);
                }
            }
            
            progressTask.Increment(100.0 / iterations1);
        }

        return totalElapsed;
    }
}
