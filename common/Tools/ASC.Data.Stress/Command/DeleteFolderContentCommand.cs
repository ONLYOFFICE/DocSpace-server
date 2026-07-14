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

using System.Text.Json;

namespace ASC.Data.Stress.Command;

public class DeleteFolderContentCommand : AsyncCommand<DeleteFolderContentCommand.Settings>, IBaseCommand
{
    public static string Name => "delete-folder-content";
    public static string Description => "Deletes all folders and files from the \"My documents\"";

    public class Settings : CommandSettings
    {
        public static readonly Settings Default = new()
        {
            Email = "test@onlyoffice.com",
            Password = "11111111"
        };

        [CommandOption("--folder-id")]
        public int FolderId { get; set; }

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

        using var foldersApi = new FoldersApi(configuration);
        using var operationsApi = new OperationsApi(configuration);

        var token = CancellationToken.None;

        if (settings.FolderId == 0)
        {
            var rootFolder = (await foldersApi.GetRootFoldersAsync(cancellationToken: token)).Response;
            settings.FolderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType is FolderType.USER)!.Current.Id;
        }

        // Deletion runs as a slow background operation and the per-id status endpoint
        // reports "finished" prematurely, so the folder content itself is the source of
        // truth: keep (re)publishing the delete and poll the listing until it is empty.
        const int pollDelayMs = 2000;
        const int republishAfterStalledPolls = 45; // ~90s without progress -> re-issue delete
        const int giveUpAfterStalledPolls = 150;    // ~5min without progress -> abort

        var (folderIds, fileIds) = await GetFolderContentIdsAsync(foldersApi, settings.FolderId, token);

        if (folderIds.Count == 0 && fileIds.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Folder {settings.FolderId} is already empty.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"Deleting [green]{folderIds.Count}[/] folders and [green]{fileIds.Count}[/] files from folder {settings.FolderId}...");
        await PublishDeleteAsync(operationsApi, folderIds, fileIds, token);

        var lastRemaining = folderIds.Count + fileIds.Count;
        var stalledPolls = 0;

        while (true)
        {
            await Task.Delay(pollDelayMs, token);

            (folderIds, fileIds) = await GetFolderContentIdsAsync(foldersApi, settings.FolderId, token);
            var remaining = folderIds.Count + fileIds.Count;

            if (remaining == 0)
            {
                AnsiConsole.MarkupLine($"[green]Folder {settings.FolderId} cleared.[/]");
                return 0;
            }

            if (remaining < lastRemaining)
            {
                lastRemaining = remaining;
                stalledPolls = 0;
                AnsiConsole.MarkupLine($"Remaining: [green]{folderIds.Count}[/] folders, [green]{fileIds.Count}[/] files...");
                continue;
            }

            stalledPolls++;

            if (stalledPolls >= giveUpAfterStalledPolls)
            {
                AnsiConsole.MarkupLine($"[red]Folder {settings.FolderId} still contains {folderIds.Count} folders and {fileIds.Count} files; no progress - aborting.[/]");
                return 1;
            }

            if (stalledPolls % republishAfterStalledPolls == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No progress; re-issuing delete for {folderIds.Count} folders and {fileIds.Count} files...[/]");
                await PublishDeleteAsync(operationsApi, folderIds, fileIds, token);
            }
        }
    }

    private static async Task PublishDeleteAsync(OperationsApi operationsApi, List<int> folderIds, List<int> fileIds, CancellationToken token)
    {
        // Deleting the top-level folders cascades to all nested folders and files.
        // Immediately=false moves items to the trash, which is what actually removes
        // them from "My documents" (a hard delete from a non-trash folder is a no-op).
        var request = new DeleteBatchRequestDto(
            folderIds: folderIds.Select(id => new DeleteBatchRequestDtoAllOfFolderIds(id)).ToList(),
            fileIds: fileIds.Select(id => new DeleteBatchRequestDtoAllOfFileIds(id)).ToList(),
            deleteAfter: false,
            immediately: false);

        await operationsApi.DeleteBatchItemsAsync(request, token);
    }

    private static async Task<(List<int> folderIds, List<int> fileIds)> GetFolderContentIdsAsync(FoldersApi foldersApi, int folderId, CancellationToken token)
    {
        var folderIds = new List<int>();
        var fileIds = new List<int>();

        const int pageSize = 100;
        var startIndex = 0;

        while (true)
        {
            // The strongly typed content exposes children as the base type without Id,
            // so parse the raw response to collect folder and file identifiers.
            var response = await foldersApi.GetFolderByFolderIdWithHttpInfoAsync(folderId, count: pageSize, startIndex: startIndex, cancellationToken: token);

            using var document = JsonDocument.Parse(response.RawContent);
            var content = document.RootElement.GetProperty("response");

            CollectIds(content, "folders", folderIds);
            CollectIds(content, "files", fileIds);

            var total = content.TryGetProperty("total", out var totalElement) ? totalElement.GetInt32() : 0;
            startIndex += pageSize;

            if (startIndex >= total)
            {
                break;
            }
        }

        return (folderIds, fileIds);
    }

    private static void CollectIds(JsonElement content, string propertyName, List<int> ids)
    {
        if (content.TryGetProperty(propertyName, out var array) && array.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in array.EnumerateArray())
            {
                if (item.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id))
                {
                    ids.Add(id);
                }
            }
        }
    }
}
