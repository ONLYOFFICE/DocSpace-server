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

[Transient]
public class FormFillingReportTask : DocumentBuilderTask<int, FormFillingReportTaskData>
{
    public FormFillingReportTask()
    {

    }

    public FormFillingReportTask(IServiceScopeFactory serviceProvider) : base(serviceProvider)
    {
    }

    private const string ScriptName = "FormFillingReport.docbuilder";

    protected override async Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        var script = await DocumentBuilderScriptHelper.ReadTemplateFromEmbeddedResource(ScriptName) ?? throw new Exception("Template not found");
        var tempFileName = DocumentBuilderScriptHelper.GetTempFileName(".xlsx");

        // Resolved from the per-execution scope: the tenant and user context the report depends on
        // is only established after DoJob has created that scope.
        var data = await serviceProvider.GetRequiredService<FormFillingReportBuilder>()
            .BuildAsync(_userId, _data.RoomId, _data.OriginalFormId, _data.OriginalFormVersion);

        script = script
            .Replace("${tempFileName}", tempFileName)
            .Replace("${inputData}", JsonSerializer.Serialize(data));

        return new DocumentBuilderInputData(script, tempFileName, "");
    }

    protected override Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        var headers = _data.Headers != null
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value))
            : [];

        return serviceProvider.GetRequiredService<FormFillingResultFileWriter>()
            .SaveAsync(_data.OriginalFormId, _data.IsNewFile, fileUri, headers);
    }
}

public record FormFillingReportTaskData(int RoomId, int OriginalFormId, int OriginalFormVersion, bool IsNewFile, IDictionary<string, string> Headers);
