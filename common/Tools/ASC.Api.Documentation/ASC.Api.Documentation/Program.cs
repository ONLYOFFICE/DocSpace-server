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

var sdkCommands = new[]
{
    "CSharp",
    "Python",
    "PostmanCollection",
    "TypeScript",
    "Java",
    "Kotlin",
    "Php",
    "Swift6",
    "Go",
    "Ruby"
};

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<GenerateCSharpSdkCommand>("CSharp");
    config.AddCommand<GeneratePythonSdkCommand>("Python");
    config.AddCommand<GeneratePostmanCollectionSdkCommand>("PostmanCollection");
    config.AddCommand<GenerateTypeScriptSdkCommand>("TypeScript");
    config.AddCommand<GenerateJavaSdkCommand>("Java");
    config.AddCommand<GenerateKotlinSdkCommand>("Kotlin");
    config.AddCommand<GeneratePhpSdkCommand>("Php");
    config.AddCommand<GenerateSwift6SdkCommand>("Swift6");
    config.AddCommand<GenerateGoSdkCommand>("Go");
    config.AddCommand<GenerateRubySdkCommand>("Ruby");
});

var joinExitCode = new CommandApp<OpenapiJoiner>().Run(Array.Empty<string>());
if (joinExitCode != 0)
{
    return joinExitCode;
}

if (args.Length == 0)
{
    var selectAll = "Generate All SDK";
    var selectManage = "Choose SDK For Generation";
    var action = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Select an [green]action[/]:")
            .AddChoices(selectAll, selectManage));

    if (action == selectAll)
    {
        return RunCommands(app, sdkCommands);
    }

    while (true)
    {
        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select [green]SDK[/] for generation:")
                .InstructionsText("[grey](Space - Select, Enter - Confirm)[/]")
                .NotRequired()
                .AddChoices(sdkCommands));

        if (selected.Count > 0)
        {
            return RunCommands(app, selected);
        }

        AnsiConsole.MarkupLine("[red]You need to select at least one SDK.[/]");
    }
}

var buildExitCode = BuildSdkGenerator();
if (buildExitCode != 0)
{
    return buildExitCode;
}

return app.Run(args);

static int RunCommands(CommandApp app, IEnumerable<string> commands)
{
    var buildExitCode = BuildSdkGenerator();
    if (buildExitCode != 0)
    {
        return buildExitCode;
    }

    foreach (var command in commands)
    {
        var exitCode = app.Run([command]);
        if (exitCode != 0)
        {
            return exitCode;
        }
    }

    return 0;
}

static int BuildSdkGenerator()
{
    return new CommandApp<BuildSdkGeneratorCommand>().Run(Array.Empty<string>());
}
