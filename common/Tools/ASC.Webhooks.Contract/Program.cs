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

var check = args.Contains("--check");

// the server's own copy, which ASC.Webhooks.Core embeds and Web.Studio serves, and the published one in the spec
// submodule, which the SDKs are generated from; the second is skipped when the submodule is not checked out
List<string> contractPaths = [Path.Combine(WebhookContractGenerator.FindContractDirectory(AppContext.BaseDirectory), WebhookContractGenerator.ContractFileName)];

string publishedPath = null;

if (WebhookContractGenerator.FindPublishedDirectory(AppContext.BaseDirectory) is { } publishedDirectory)
{
    publishedPath = Path.Combine(publishedDirectory, WebhookContractGenerator.ContractFileName);
    contractPaths.Add(publishedPath);
}
else
{
    Console.WriteLine("sdk/docspace-api-spec is not checked out: the published copy of the contract is skipped.");
}

var contract = WebhookContractGenerator.Generate();

if (check)
{
    var outdated = 0;

    foreach (var contractPath in contractPaths)
    {
        // the pinned spec commit may not carry the published copy yet; that is a pending publication, not a stale file
        if (contractPath == publishedPath && !File.Exists(contractPath))
        {
            Console.WriteLine($"{contractPath} is not in the checked-out spec submodule yet: skipped.");
            continue;
        }

        var current = File.Exists(contractPath) ? File.ReadAllText(contractPath).ReplaceLineEndings("\n") : "";

        if (current == contract)
        {
            Console.WriteLine($"{contractPath} is up to date.");
        }
        else
        {
            Console.Error.WriteLine($"{contractPath} is out of date: run dotnet run --project common/Tools/ASC.Webhooks.Contract and commit the result.");
            outdated++;
        }
    }

    return outdated == 0 ? 0 : 1;
}

foreach (var contractPath in contractPaths)
{
    Directory.CreateDirectory(Path.GetDirectoryName(contractPath)!);
    File.WriteAllText(contractPath, contract, new UTF8Encoding(false));
    Console.WriteLine($"Wrote {contractPath}");
}

return 0;
