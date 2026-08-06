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
public class RoomIndexExportTask : DocumentBuilderTask<int, RoomIndexExportTaskData>
{
    public RoomIndexExportTask()
    {

    }

    public RoomIndexExportTask(IServiceScopeFactory serviceProvider) : base(serviceProvider)
    {
    }

    protected override async Task<DocumentBuilderInputData> GetDocumentBuilderInputDataAsync(IServiceProvider serviceProvider)
    {
        // Resolved from the per-execution scope: the tenant and user context the export depends on
        // is only established after DoJob has created that scope.
        var (scriptFilePath, tempFileName, outputFileName) = await serviceProvider.GetRequiredService<RoomIndexExportBuilder>()
            .BuildAsync(_userId, _data.RoomId);

        return new DocumentBuilderInputData(scriptFilePath, tempFileName, outputFileName);
    }

    protected override async Task<File<int>> ProcessSourceFileAsync(IServiceProvider serviceProvider, Uri fileUri, DocumentBuilderInputData inputData)
    {
        var daoFactory = serviceProvider.GetRequiredService<IDaoFactory>();
        var fileSaver = serviceProvider.GetRequiredService<ReportResultFileSaver>();
        var filesMessageService = serviceProvider.GetRequiredService<FilesMessageService>();

        var folderDao = daoFactory.GetFolderDao<int>();

        var parentId = await folderDao.GetFolderIDUserAsync(false, _userId);

        var file = await fileSaver.SaveAsync(_userId, parentId, inputData.OutputFileName, fileUri);

        var headers = _data.Headers != null
            ? _data.Headers.ToDictionary(x => x.Key, x => new StringValues(x.Value))
            : [];

        var room = await folderDao.GetFolderAsync(_data.RoomId);

        await filesMessageService.SendAsync(MessageAction.RoomIndexExportSaved, room, headers: headers);

        if (System.IO.File.Exists(inputData.Script))
        {
            System.IO.File.Delete(inputData.Script);
        }

        return file;
    }
}

public record RoomIndexExportTaskData(int RoomId, IDictionary<string, string> Headers);
