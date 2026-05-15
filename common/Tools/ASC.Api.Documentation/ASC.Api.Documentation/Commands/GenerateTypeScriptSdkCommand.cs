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

namespace ASC.Api.Documentation.Commands;

public class GenerateTypeScriptSdkCommand : SdkCommandBase
{
    protected override string Name => "TypeScript";

    public override ValidationResult Validate(CommandContext context, NoArgumentsCommandSettings settings)
    {
        var baseValidation = base.Validate(context, settings);
        if (!baseValidation.Successful)
        {
            return baseValidation;
        }

        return ToolRunner.ValidateAvailable("npm", "--version");
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        NoArgumentsCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var generateExitCode = await base.ExecuteAsync(context, settings, cancellationToken);
        if (generateExitCode != 0)
        {
            return generateExitCode;
        }

        var typeScriptSdkDirectory = Path.GetFullPath(
            Path.Combine(
                WorkingDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "sdk",
                "docspace-api-sdk-typescript"));

        var installExitCode = await RunNpmAsync("install", typeScriptSdkDirectory, cancellationToken);
        if (installExitCode != 0)
        {
            return installExitCode;
        }

        var packExitCode = await RunNpmAsync("pack", typeScriptSdkDirectory, cancellationToken);
        if (packExitCode != 0)
        {
            return packExitCode;
        }

        return CopyPackages(typeScriptSdkDirectory);
    }

    private static async Task<int> RunNpmAsync(string command, string workingDirectory, CancellationToken cancellationToken)
    {
        return await ToolRunner.RunAndWriteAsync(
            "npm",
            [command],
            workingDirectory,
            cancellationToken,
            $"Failed to start npm {command}.");
    }

    private static int CopyPackages(string typeScriptSdkDirectory)
    {
        var packageTargetDirectory = Path.GetFullPath(
            Path.Combine(typeScriptSdkDirectory, "..", "..", "..", "client", "libs", "ui-kit"));

        if (!Directory.Exists(typeScriptSdkDirectory))
        {
            AnsiConsole.MarkupLine(
                $"[red]{Markup.Escape($"Package source directory '{typeScriptSdkDirectory}' was not found after running npm pack.")}[/]");
            return 1;
        }

        Directory.CreateDirectory(packageTargetDirectory);

        var copiedPackages = 0;
        foreach (var packagePath in Directory.EnumerateFiles(typeScriptSdkDirectory, "onlyoffice-docspace-api-sdk-*.tgz"))
        {
            var destinationPath = Path.Combine(packageTargetDirectory, Path.GetFileName(packagePath));
            File.Copy(packagePath, destinationPath, overwrite: true);
            copiedPackages++;
        }

        if (copiedPackages == 0)
        {
            AnsiConsole.MarkupLine(
                $"[red]{Markup.Escape($"No .tgz files matching 'onlyoffice-docspace-api-sdk-*.tgz' were found in '{typeScriptSdkDirectory}' after running npm pack.")}[/]");
            return 1;
        }

        return 0;
    }
}
