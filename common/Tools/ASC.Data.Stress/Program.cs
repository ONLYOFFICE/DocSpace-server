using ASC.Data.Stress;
using ASC.Data.Stress.Command;

using GetFolderCommand = ASC.Data.Stress.Command.GetFolderCommand;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddBaseCommand<CreateFolderWithFilesDepthCommand>();
    config.AddBaseCommand<GetFolderCommand>();
    config.AddBaseCommand<ExecuteFileCreationAndSharingCommand>();
    config.AddBaseCommand<ExecuteRoomCreationAndSharingCommand>();
    config.AddBaseCommand<InviteContactsCommand>();
});

if (args.Length == 0)
{
    var command = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Select a [green]command[/]:")
            .AddChoices(CreateFolderWithFilesDepthCommand.Name, GetFolderCommand.Name, ExecuteFileCreationAndSharingCommand.Name, ExecuteRoomCreationAndSharingCommand.Name, InviteContactsCommand.Name)
            .UseConverter(cmd => cmd switch
            {
                _ when cmd == CreateFolderWithFilesDepthCommand.Name => CreateFolderWithFilesDepthCommand.Description,
                _ when cmd == GetFolderCommand.Name => GetFolderCommand.Description,
                _ when cmd == ExecuteFileCreationAndSharingCommand.Name => ExecuteFileCreationAndSharingCommand.Description,
                _ when cmd == ExecuteRoomCreationAndSharingCommand.Name => ExecuteRoomCreationAndSharingCommand.Description,
                _ when cmd == InviteContactsCommand.Name => InviteContactsCommand.Description,
                _ => cmd
            }));

    app.Run([command]);
}
else
{
    app.Run(args);
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();