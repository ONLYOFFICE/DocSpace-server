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

namespace ASC.Files.Core.Helpers;

#nullable enable

/// <summary>A form field read out of a PDF form: its key and Document Server field type.</summary>
public record FormFieldDefinition(string Key, string Type);

/// <summary>
/// Reads the field layout of a PDF form straight from the file via the ExtractFormFieldsData.docbuilder
/// script, returning the same "{ formsdata: [...] }" shape Document Server's normal formsDataUrl submit
/// callback produces. Works regardless of the form's language, so callers don't need to know its field keys
/// in advance.
/// </summary>
[Scope]
public class FormFieldsExtractor(
    DocumentServiceConnector documentServiceConnector,
    DocumentServiceHelper documentServiceHelper,
    PathProvider pathProvider,
    DocumentBuilderTask documentBuilderTask,
    IHttpClientFactory httpClientFactory)
{
    // Default encoder unicode-escapes "&", which the docbuilder script engine doesn't decode back inside
    // string literals, breaking every query parameter after the first in an embedded URL.
    private static readonly JsonSerializerOptions _scriptStringOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private static readonly TimeSpan _sourceUrlSignatureLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// The field keys/types present in a "{ formsdata: [...] }" payload, matching how the report lays out its
    /// columns (picture/signature fields excluded).
    /// </summary>
    public static IReadOnlyList<FormFieldDefinition> ParseFields(string formsDataJson)
    {
        var fields = new List<FormFieldDefinition>();

        using var document = JsonDocument.Parse(formsDataJson);
        if (document.RootElement.TryGetProperty("formsdata", out var formsArray) && formsArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var form in formsArray.EnumerateArray())
            {
                var key = form.TryGetProperty("key", out var keyProp) ? keyProp.GetString() : null;
                var type = form.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

                if (!string.IsNullOrEmpty(key) && type != "picture" && type != "signature")
                {
                    fields.Add(new FormFieldDefinition(key, type ?? "text"));
                }
            }
        }

        return fields;
    }

    /// <summary>
    /// Runs ExtractFormFieldsData.docbuilder against a form file and returns the "{ formsdata: [...] }" JSON it
    /// embeds in its text output.
    /// </summary>
    public async Task<string> ExtractFieldsJsonAsync(File<int> file, bool lastVersion, CancellationToken cancellationToken = default)
    {
        var sourceUrl = documentServiceConnector.ReplaceCommunityAddress(pathProvider.GetFileStreamUrl(file, lastVersion));

        // OpenFile(url) can't set headers, so the signature travels as a query param — bound to this file and
        // short-lived (exp-enforced), since query strings, unlike headers, tend to end up in access logs.
        var signatureToken = documentServiceHelper.GetSignature(new
        {
            fileId = file.Id,
            exp = DateTimeOffset.UtcNow.Add(_sourceUrlSignatureLifetime).ToUnixTimeSeconds()
        });

        if (!string.IsNullOrEmpty(signatureToken))
        {
            sourceUrl = FilesLinkUtility.AddQueryString(sourceUrl, new Dictionary<string, string>
            {
                { FilesLinkUtility.SignatureQueryKey, signatureToken }
            });
        }

        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource("ExtractFormFieldsData.docbuilder")
            ?? throw new InvalidOperationException("ExtractFormFieldsData.docbuilder template not found.");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".txt");

        // The docbuilder engine keeps only one document active at a time, so the script embeds the result in
        // the opened PDF's own text output between unique markers; this pulls the JSON back out from between them.
        var markerStart = $"@@FORMDATA_START_{Guid.NewGuid():N}@@";
        var markerEnd = $"@@FORMDATA_END_{Guid.NewGuid():N}@@";

        script = script
            .Replace("${sourceFileUrl}", JsonSerializer.Serialize(sourceUrl, _scriptStringOptions))
            .Replace("${tempFileName}", tempFileName)
            .Replace("${resultMarkerStart}", markerStart)
            .Replace("${resultMarkerEnd}", markerEnd);

        var inputData = new DocumentBuilderInputData(script, tempFileName, "");
        var resultTextUrl = await documentBuilderTask.BuildFileAsync(inputData, cancellationToken);

#pragma warning disable CA2000 // HttpClient is short-lived and disposed by runtime
        var httpClient = httpClientFactory.CreateClient();
#pragma warning restore CA2000
        using var response = await httpClient.GetAsync(resultTextUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var resultText = await response.Content.ReadAsStringAsync(cancellationToken);

        var startIndex = resultText.IndexOf(markerStart, StringComparison.Ordinal);
        var endIndex = resultText.IndexOf(markerEnd, StringComparison.Ordinal);
        if (startIndex < 0 || endIndex < 0 || endIndex <= startIndex)
        {
            throw new InvalidOperationException("The form data markers were not found in the DocBuilder script output.");
        }

        startIndex += markerStart.Length;

        return resultText[startIndex..endIndex];
    }
}
