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

public class GenerateCSharpSdkCommand : SdkCommandBase<CSharpSdkCommandSettings>
{
    protected override string Name => "CSharp";

    public override ValidationResult Validate(CommandContext context, CSharpSdkCommandSettings settings)
    {
        var baseValidation = base.Validate(context, settings);
        if (!baseValidation.Successful)
        {
            return baseValidation;
        }

        return ToolRunner.ValidateAvailable("dotnet", "--version");
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CSharpSdkCommandSettings settings,
        CancellationToken cancellationToken)
    {
        var generateExitCode = await base.ExecuteAsync(context, settings, cancellationToken);
        if (generateExitCode != 0)
        {
            return generateExitCode;
        }

        var csharpSdkDirectory = Path.GetFullPath(
            Path.Combine(
                WorkingDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "sdk",
                "docspace-api-sdk-csharp"));

        var buildExitCode = await BuildAsync(csharpSdkDirectory, settings.Configuration, cancellationToken);
        if (buildExitCode != 0)
        {
            return buildExitCode;
        }

        return CopyPackages(csharpSdkDirectory, settings.Configuration);
    }

    private static async Task<int> BuildAsync(
        string workingDirectory,
        string configuration,
        CancellationToken cancellationToken)
    {
        return await ToolRunner.RunAndWriteAsync(
            "dotnet",
            ["build", "-c", configuration],
            workingDirectory,
            cancellationToken,
            "Failed to start dotnet build.");
    }

    private static int CopyPackages(string csharpSdkDirectory, string configuration)
    {
        var packageSourceDirectory = Path.Combine(csharpSdkDirectory, "src", "DocSpace.API.SDK", "bin", configuration);
        var packageTargetDirectory = Path.GetFullPath(
            Path.Combine(csharpSdkDirectory, "..", "..", ".nuget", "packages"));

        if (!Directory.Exists(packageSourceDirectory))
        {
            AnsiConsole.MarkupLine(
                $"[red]{Markup.Escape($"Package source directory '{packageSourceDirectory}' was not found after building configuration '{configuration}'.")}[/]");
            return 1;
        }

        Directory.CreateDirectory(packageTargetDirectory);

        var copiedPackages = 0;
        foreach (var packagePath in Directory.EnumerateFiles(packageSourceDirectory, "*.nupkg"))
        {
            var destinationPath = Path.Combine(packageTargetDirectory, Path.GetFileName(packagePath));
            File.Copy(packagePath, destinationPath, overwrite: true);
            copiedPackages++;
        }

        if (copiedPackages == 0)
        {
            AnsiConsole.MarkupLine(
                $"[red]{Markup.Escape($"No .nupkg files were found in '{packageSourceDirectory}' after building configuration '{configuration}'.")}[/]");
            return 1;
        }

        return 0;
    }
}
