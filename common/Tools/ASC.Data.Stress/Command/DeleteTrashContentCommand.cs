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

public class DeleteTrashContentCommand : AsyncCommand<DeleteTrashContentCommand.Settings>, IBaseCommand
{
    public static string Name => "delete-trash-content";
    public static string Description => "Permanently deletes all folders and files from the Trash";

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
            settings.FolderId = rootFolder.FirstOrDefault(r => r.Current.RootFolderType is FolderType.TRASH)!.Current.Id;
        }

        var (folderIds, fileIds) = await GetFolderContentIdsAsync(foldersApi, settings.FolderId, token);

        if (folderIds.Count == 0 && fileIds.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Trash (folder {settings.FolderId}) is already empty.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"Emptying the Trash: [green]{folderIds.Count}[/] folders and [green]{fileIds.Count}[/] files (nested content removed by cascade)...");

        // The emptytrash endpoint collects the whole Trash content itself and removes it in a
        // single operation. single=true returns only this operation so it is trackable by its
        // own id; otherwise the status endpoint reports "finished" prematurely.
        var operations = (await operationsApi.EmptyTrashAsync(single: true, cancellationToken: token)).Response;
        var operationId = operations.FirstOrDefault()?.Id;

        var error = await WaitOperationAsync(operationsApi, operationId, token);
        if (!string.IsNullOrEmpty(error))
        {
            AnsiConsole.MarkupLine($"[red]Operation error: {error}[/]");
            return 1;
        }

        (folderIds, fileIds) = await GetFolderContentIdsAsync(foldersApi, settings.FolderId, token);
        if (folderIds.Count == 0 && fileIds.Count == 0)
        {
            AnsiConsole.MarkupLine($"[green]Trash (folder {settings.FolderId}) cleared.[/]");
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Trash still contains {folderIds.Count} folders and {fileIds.Count} files.[/]");
        return 1;
    }

    // Tracks the delete operation by its own id (published with single=true).
    // Returns the operation error message, or null on success.
    private static async Task<string?> WaitOperationAsync(OperationsApi operationsApi, string? operationId, CancellationToken token)
    {
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(30));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, token);

        string? lastProcessed = null;

        while (true)
        {
            var statuses = (await operationsApi.GetOperationStatusesAsync(id: operationId, cancellationToken: linkedCts.Token)).Response;
            var operation = statuses.FirstOrDefault(o => o.Id == operationId) ?? statuses.FirstOrDefault();

            if (operation != null)
            {
                if (!string.IsNullOrEmpty(operation.Error))
                {
                    return operation.Error;
                }

                if (operation.Processed != lastProcessed)
                {
                    lastProcessed = operation.Processed;
                    AnsiConsole.MarkupLine($"Progress: [green]{operation.Progress}%[/] (processed {operation.Processed})...");
                }

                if (operation.Finished)
                {
                    return null;
                }
            }

            await Task.Delay(500, linkedCts.Token);
        }
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

            var before = folderIds.Count + fileIds.Count;
            CollectIds(content, "folders", folderIds);
            CollectIds(content, "files", fileIds);
            var pageCount = folderIds.Count + fileIds.Count - before;

            startIndex += pageSize;

            // The Trash listing "total" is unreliable, so page until an empty page.
            if (pageCount == 0)
            {
                break;
            }
        }

        return (folderIds.Distinct().ToList(), fileIds.Distinct().ToList());
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
