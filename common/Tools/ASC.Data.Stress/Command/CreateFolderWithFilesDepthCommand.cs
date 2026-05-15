// Copyright (C) Ascensio System SIA, 2009-2026
// 
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
// 
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
// 
// No trademark rights are granted under this License.
// 
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
// 
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
// 
// SPDX-License-Identifier: AGPL-3.0-only

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
