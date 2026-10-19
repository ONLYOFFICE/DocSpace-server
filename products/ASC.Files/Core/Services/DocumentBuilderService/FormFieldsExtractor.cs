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

namespace ASC.Files.Core.Services.DocumentBuilderService;

/// <summary>
/// Reads the fields of a stored PDF form through the document builder, in the shape the editor submits them:
/// <c>{"formsdata": [...]}</c>.
/// </summary>
[Scope]
public class FormFieldsExtractor(
    IDaoFactory daoFactory,
    PathProvider pathProvider,
    DocumentServiceConnector documentServiceConnector,
    DocumentBuilderTask documentBuilderTask,
    IHttpClientFactory httpClientFactory)
{
    private const string ScriptName = "ExtractFormFieldsData.docbuilder";

    // The default encoder escapes "&", which the script engine does not decode in a string literal.
    private static readonly JsonSerializerOptions _scriptStringOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// The forms data of the file version, with null forms data for a file that is not an ONLYOFFICE form.
    /// </summary>
    public async Task<string> ExtractFieldsJsonAsync(File<int> file, CancellationToken cancellationToken)
    {
        // The builder sends no signature the stream handler would accept, so it gets a one-time temporary copy.
        string sourceUrl;
        await using (var stream = await daoFactory.GetFileDao<int>().GetFileStreamAsync(file))
        {
            sourceUrl = documentServiceConnector.ReplaceCommunityAddress(await pathProvider.GetTempUrlAsync(stream, FileUtility.GetFileExtension(file.Title)));
        }

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName)
            ?? throw new InvalidOperationException($"{ScriptName} template not found.");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".txt");

        script = script
            .Replace("${sourceFileUrl}", JsonSerializer.Serialize(sourceUrl, _scriptStringOptions))
            .Replace("${tempFileName}", tempFileName);

        var resultUrl = await documentBuilderTask.BuildFileAsync(new DocumentBuilderInputData(script, tempFileName, ""), cancellationToken);

        var httpClient = httpClientFactory.CreateClient();

        return (await httpClient.GetStringAsync(resultUrl, cancellationToken)).Trim();
    }
}
