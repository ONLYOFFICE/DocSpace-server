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
/// Downloads a report produced by the document builder and stores it as a new file entry,
/// broadcasting the creation over the socket. Shared by every report task that saves its result
/// as a brand-new file.
/// </summary>
[Scope]
public class ReportResultFileSaver(
    IServiceProvider serviceProvider,
    IDaoFactory daoFactory,
    IHttpClientFactory clientFactory,
    SocketManager socketManager,
    GlobalFolder globalFolder)
{
    /// <summary>
    /// Saves the report into the author's "My documents" folder.
    /// </summary>
    public async Task<File<int>> SaveToMyDocumentsAsync(Guid createdBy, string title, Uri fileUri)
    {
        var parentId = await globalFolder.GetFolderMyAsync(daoFactory);

        return await SaveAsync(createdBy, parentId, title, fileUri);
    }

    public async Task<File<int>> SaveAsync(Guid createdBy, int parentId, string title, Uri fileUri)
    {
        var file = serviceProvider.GetService<File<int>>();

        file.CreateBy = createdBy;
        file.ParentId = parentId;
        file.Title = title;

        using var request = new HttpRequestMessage { RequestUri = fileUri };

#pragma warning disable CA2000
        var httpClient = clientFactory.CreateClient();
#pragma warning restore CA2000

        using var response = await httpClient.SendAsync(request);
        await using var stream = await response.Content.ReadAsStreamAsync();

        var fileDao = daoFactory.GetFileDao<int>();

        file.ContentLength = stream.Length;

        file = await fileDao.SaveFileAsync(file, stream);
        await socketManager.CreateFileAsync(file);

        return file;
    }
}
