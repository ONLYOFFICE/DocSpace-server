namespace ASC.Data.Stress.Command;

public class CreateFolderWithFilesDepthCommand : AsyncCommand<CreateFolderWithFilesDepthCommand.Settings>, IBaseCommand
{
    public static string Name => "create-folder-depth";
    public static string Description => "Creates folders with files at specified depth";
    
    public class Settings : CommandSettings
    {
        public static Settings Default = new()
        {
            Email = "test@onlyoffice.com",
            Password = "11111111"
        };
        
        [CommandOption("--folder-id")]
        public int FolderId { get; set; }

        [CommandOption("--depth")]
        public int Depth { get; set; } = 5;

        [CommandOption("--files-count")]
        public int FilesCount { get; set; } = 10;

        [CommandOption("--folders-count")]
        public int FoldersCount { get; set; } = 5;

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
        
        return ValidationResult.Success();
    }
    
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var configuration = await ApiHelper.GetConfigurationAsync(settings.Email, settings.Password);
        
        using var filesApi = new FilesApi(configuration);
        using var foldersApi = new FoldersApi(configuration);
        
        var system = new Faker().System;
        var token = CancellationToken.None;

        if (settings.FolderId == 0)
        {
            var rootFolder = (await foldersApi.GetRootFoldersAsync(cancellationToken: token)).Response;
            settings.FolderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType is FolderType.USER)!.Current.Id;
        }

        await CreateFolderWithFilesDepth(filesApi, foldersApi, system, settings.FolderId, settings.Depth, settings.FoldersCount, settings.FilesCount, token);
        return 0;
    }

    private static async Task CreateFolderWithFilesDepth(FilesApi filesApi, FoldersApi foldersApi, Bogus.DataSets.System system, int folderId, int depth, int foldersCount, int filesCount, CancellationToken token)
    {
        var newFolders = new List<int>();
        List<Task> tasks = [];
        for (var i = 0; i < foldersCount; i++)
        {
            tasks.Add(foldersApi.CreateFolderAsync(folderId, new CreateFolder(system.FileName()), token).ContinueWith(r=> newFolders.Add(r.Result.Response.Id), token));
        }
        
        await Task.WhenAll(tasks);
        tasks.Clear();
        
        foreach (var newFolder in newFolders)
        {
            var k = 0;
            for (var j = 0; j < filesCount; j++)
            {
                tasks.Add(filesApi.CreateFileAsync(newFolder, new CreateFileJsonElement(system.FileName("pdf")), cancellationToken: token));
                k++;
                if (k == 100)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                    k = 0;
                }
            }
        }

        await Task.WhenAll(tasks);
        tasks.Clear();
        
        foreach (var newFolder in newFolders)
        {
            var newDepth = depth - 1;
            if (newDepth > 0)
            {
                await CreateFolderWithFilesDepth(filesApi, foldersApi, system, newFolder, newDepth, foldersCount, filesCount, token);
            }
        }

        AnsiConsole.MarkupLine($"[green]Folder {folderId} created. Depth: {depth}[/]");
    }
}
