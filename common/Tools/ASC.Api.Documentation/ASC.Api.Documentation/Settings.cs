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

namespace ASC.Api.Documentation;

public class JoinSettings : CommandSettings
{
    [CommandOption("-o|--output <OUTPUT>")]
    public string Output { get; set; } = null!;

    [CommandOption("-f|--file <FILES>")]
    public IEnumerable<string>? Files { get; set; }

    public override ValidationResult Validate()
    {
        var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

        if (!string.IsNullOrWhiteSpace(Output))
        {
            Output = Path.GetFullPath(Output);
        }
        else
        {
            var outputPath = configuration.GetSection("pathToFile").Get<string[]>();
            if (outputPath == null || outputPath.Length == 0)
            {
                return ValidationResult.Error("File path not specified. Use -o|--output <OUTPUT> or configure file in appsettings.json");
            }
            Output = Path.GetFullPath(Path.Combine(outputPath));
        }

        if (Files == null || !Files.Any())
        {
            var list = new List<string>();

            var joinSection = configuration.GetSection("join");

            foreach (var child in joinSection.GetChildren())
            {
                AddFromConfig(child, list);
            }

            if (list.Count == 0)
            {
                return ValidationResult.Error("No input files specified and config is empty");
            }

            Files = [.. list];
        }
        else
        {
            Files = [.. Files.Select(Path.GetFullPath)];
        }

        return ValidationResult.Success();
    }

    private static void AddFromConfig(IConfigurationSection section, List<string> list)
    {
        var parts = section.Get<string[]>();

        if (parts == null || parts.Length == 0)
        {
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(parts));

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Swagger file not found: {fullPath}");
        }

        list.Add(fullPath);
    }
}
