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

namespace ASC.AI.Core.TextToDocx;

internal abstract class TextToDocxFolder
{
    public static TextToDocxFolder Create(IDaoFactory daoFactory, int folderId, string? thirdpartyFolderId)
    {
        if (!string.IsNullOrEmpty(thirdpartyFolderId))
        {
            var folderDao = daoFactory.GetFolderDao<string>();
            return new TextToDocxFolder<string>(folderDao, thirdpartyFolderId);
        }
        else
        {
            var folderDao = daoFactory.GetFolderDao<int>();
            return new TextToDocxFolder<int>(folderDao, folderId);
        }
    }

    public abstract Task GetFolder();
    public abstract Task CheckSecurity(FileSecurity fileSecurity);
    public abstract Task SaveFile(FileConverter fileConverter, string fileUri, string fileType, string title, bool updateIfExists);
}

internal class TextToDocxFolder<TFolder>(IFolderDao<TFolder> folderDao, TFolder folderId) : TextToDocxFolder
{
    private Folder<TFolder>? _folder;

    public override async Task GetFolder()
    {
        _folder = await folderDao.GetFolderAsync(folderId);

        if (_folder == null)
        {
            throw new Exception(FilesCommonResource.ErrorMessage_FolderNotFound);
        }

        if (_folder.FolderType is FolderType.AiRoom)
        {
            var folder = await folderDao.GetFoldersAsync(_folder.Id, FolderType.ResultStorage)
                .FirstOrDefaultAsync() ?? throw new Exception(FilesCommonResource.ErrorMessage_FolderNotFound);

            _folder = folder;
        }
    }

    public override async Task CheckSecurity(FileSecurity fileSecurity)
    {
        if (!await fileSecurity.CanCreateAsync(_folder))
        {
            throw new SecurityException(FilesCommonResource.ErrorMessage_SecurityException);
        }
    }

    public override async Task SaveFile(FileConverter fileConverter, string fileUri, string fileType, string title, bool updateIfExists)
    {
        await fileConverter.SaveConvertedFileAsync(_folder, fileUri, fileType, title, updateIfExists);
    }
}
