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

namespace ASC.AI.Core.MdTextToDocx;

internal abstract class Target
{
    public static async Task<Target> InitializeAsync(IDaoFactory daoFactory, FileSecurity fileSecurity, int folderId, string? thirdpartyFolderId)
    {
        Target target;

        if (!string.IsNullOrEmpty(thirdpartyFolderId))
        {
            var folderDao = daoFactory.GetFolderDao<string>();
            target = new Target<string>(folderDao, thirdpartyFolderId);
        }
        else
        {
            var folderDao = daoFactory.GetFolderDao<int>();
            target = new Target<int>(folderDao, folderId);
        }

        await target.ResolveAsync(fileSecurity);

        return target;
    }

    protected abstract Task ResolveAsync(FileSecurity fileSecurity);
    public abstract Task SaveFile(FileConverter fileConverter, string fileUri, string fileType, string title, bool updateIfExists);
}

internal class Target<TFolder>(IFolderDao<TFolder> folderDao, TFolder folderId) : Target
{
    private Folder<TFolder>? _folder;

    protected override async Task ResolveAsync(FileSecurity fileSecurity)
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
