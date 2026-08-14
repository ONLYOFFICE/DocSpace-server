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

public abstract class SdkCommandBase<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    protected abstract string Name { get; }

    protected string WorkingDirectory => SdkPaths.WorkingDirectory;

    /// <summary>
    /// Pins the generator version for openapi-generator-cli. The CLI otherwise looks for
    /// openapitools.json in the current directory and silently writes one pinning
    /// "latest" when it finds none - which both leaves the file behind wherever the tool
    /// happened to start and unpins the generator version.
    /// </summary>
    private string OpenApiToolsConfig => Path.Combine(WorkingDirectory, "openapitools.json");

    public override ValidationResult Validate(CommandContext context, TSettings settings) =>
        ToolRunner.ValidateAvailable("openapi-generator-cli", "--openapitools", OpenApiToolsConfig, "version");

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        TSettings settings,
        CancellationToken cancellationToken)
    {
        return await RunGeneratorAsync([], cancellationToken);
    }

    /// <summary>
    /// Runs openapi-generator-cli against `tools{Name}.json`. Commands that need more than one
    /// pass - a document per service, say - call this repeatedly with the differing options.
    /// </summary>
    protected async Task<int> RunGeneratorAsync(
        IReadOnlyList<string> extraArguments,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "--openapitools",
            OpenApiToolsConfig,
            "generate",
            "-c",
            Path.Combine("tools", $"tools{Name}.json"),
            "--custom-generator",
            Path.Combine("target", "sdk-1.0-jar-with-dependencies.jar")
        };

        arguments.AddRange(extraArguments);

        return await ToolRunner.RunAndWriteAsync(
            "openapi-generator-cli",
             arguments,
             WorkingDirectory,
             cancellationToken,
             $"Failed to start openapi-generator-cli for {Name}.");
    }
}

public abstract class SdkCommandBase : SdkCommandBase<NoArgumentsCommandSettings>;
