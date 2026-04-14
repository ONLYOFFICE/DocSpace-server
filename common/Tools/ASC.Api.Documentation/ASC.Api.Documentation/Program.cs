// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

using System.Diagnostics;
using System.Text;

namespace ASC.Api.Documentation;

internal class Program
{
    static int Main(string[] args)
    {
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var workDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SDK"));

        var options = new[]
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

        if (!Directory.Exists(workDirectory))
        {
            AnsiConsole.MarkupLine($"[red]Directory not found: {Markup.Escape(workDirectory)}[/]");
            return 1;
        }

        AnsiConsole.MarkupLine("[yellow]Joining OpenAPI documents...[/]");
        var joinExitCode = new CommandApp<OpenapiJoiner>().Run(Array.Empty<string>());
        if (joinExitCode != 0)
        {
            AnsiConsole.MarkupLine($"[red]Join failed with exit code {joinExitCode}.[/]");
            return joinExitCode;
        }

        var firstWindowChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select")
                .AddChoices(new[]
                {
                    "Build All SDK",
                    "Manage Options"
                })
        );

        if (firstWindowChoice != "Build All SDK")
        {
            while (true)
            {
                var selected = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Manage Options")
                        .InstructionsText("[grey](Space - Select, Enter - Confirm)[/]")
                        .NotRequired()
                        .AddChoices(options)
                );

                if (selected.Count > 0)
                {
                    options = selected.ToArray();
                    break;
                }

                AnsiConsole.MarkupLine("[red]You need to select at least one option.[/]");
            }
        }

        var commands = new StringBuilder("/c mvn clean package");
        return RunCommand(workDirectory, commands, options);
    }

    private static int RunCommand(string workDirectory, StringBuilder commands, IEnumerable<string> options)
    {
        foreach (var option in options)
        {
            commands.Append($" && openapi-generator-cli generate -c tools/tools{option}.json --custom-generator target/sdk-1.0-jar-with-dependencies.jar");

            if (option == "CSharp")
            {
                commands.Append(" && cd ../../../../../sdk/docspace-api-sdk-csharp");
                commands.Append(" && dotnet build");
                commands.Append(" && xcopy \"src\\DocSpace.API.SDK\\bin\\Debug\\*.nupkg\" \"..\\..\\.nuget\\packages\" /s /y /b /i");
                commands.Append(" && cd ../../common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/SDK");
            }

            if (option == "TypeScript")
            {
                commands.Append(" && cd ../../../../../sdk/docspace-api-sdk-typescript");
                commands.Append(" && npm install");
                commands.Append(" && npm pack");
                commands.Append(" && xcopy \"onlyoffice-docspace-api-sdk-*.tgz\" \"..\\..\\..\\client\\libs\\ui-kit\" /s /y /b /i");
                commands.Append(" && cd ../../common/Tools/ASC.Api.Documentation/ASC.Api.Documentation/SDK");
            }
        }

        AnsiConsole.MarkupLine("[yellow]Starting SDK generation[/]");
        AnsiConsole.MarkupLine($"[grey]Working directory: {Markup.Escape(Path.GetFullPath(workDirectory))}[/]");

        using var process = Process.Start(new ProcessStartInfo("cmd.exe", commands.ToString())
        {
            WorkingDirectory = workDirectory,
            UseShellExecute = false
        });

        if (process == null)
        {
            AnsiConsole.MarkupLine("[red]Failed to start command process.[/]");
            return 1;
        }

        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            AnsiConsole.MarkupLine("[green]SDK generation completed successfully.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]SDK generation failed with exit code {process.ExitCode}.[/]");
        }

        AnsiConsole.MarkupLine("[grey]Press any key to continue...[/]");
        Console.ReadKey(true);

        return process.ExitCode;
    }
}
