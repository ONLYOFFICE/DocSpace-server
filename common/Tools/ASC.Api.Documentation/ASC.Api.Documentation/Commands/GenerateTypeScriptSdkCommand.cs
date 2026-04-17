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

using ASC.Api.Documentation.Commands;

namespace ASC.Api.Documentation.SDKs;

public class GenerateTypeScriptSdkCommand : SdkCommandBase
{
    public override string Name => "TypeScript";

    protected override string WorkingDirectory =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "SDK"));

    public override ValidationResult Validate(CommandContext context, NoArgumentsCommandSettings settings) =>
        ToolRunner.ValidateAvailable("npm", "--version");

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

        CopyPackages(typeScriptSdkDirectory);
        return 0;
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

    private static void CopyPackages(string typeScriptSdkDirectory)
    {
        var packageTargetDirectory = Path.GetFullPath(
            Path.Combine(typeScriptSdkDirectory, "..", "..", "..", "client", "libs", "ui-kit"));

        Directory.CreateDirectory(packageTargetDirectory);

        foreach (var packagePath in Directory.EnumerateFiles(typeScriptSdkDirectory, "onlyoffice-docspace-api-sdk-*.tgz"))
        {
            var destinationPath = Path.Combine(packageTargetDirectory, Path.GetFileName(packagePath));
            File.Copy(packagePath, destinationPath, overwrite: true);
        }
    }
}
