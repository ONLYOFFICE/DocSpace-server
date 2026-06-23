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

using File = Microsoft.SharePoint.Client.File;
using Folder = Microsoft.SharePoint.Client.Folder;

namespace ASC.Files.Thirdparty.SharePoint;

[Scope(typeof(IDaoSelector<File, Folder, ClientObject>))]
internal class SharePointDaoSelector(IServiceProvider serviceProvider, IDaoFactory daoFactory) : RegexDaoSelectorBase<File, Folder, ClientObject>(serviceProvider, daoFactory)
{
    public override IFileDao<string> GetFileDao(string id)
    {
        var fileDao = _serviceProvider.GetService<SharePointFileDao>();
        var info = GetInfo(id);
        fileDao.Init(info.PathPrefix, info.ProviderInfo);
        return fileDao;
    }

    public override IFolderDao<string> GetFolderDao(string id)
    {
        var folderDao = _serviceProvider.GetService<SharePointFolderDao>();
        var info = GetInfo(id);
        folderDao.Init(info.PathPrefix, info.ProviderInfo);
        return folderDao;
    }

    public override IThirdPartyTagDao GetTagDao(string id)
    {
        var tagDao = _serviceProvider.GetService<SharePointTagDao>();
        var info = GetInfo(id);
        tagDao.Init(info.PathPrefix, info.ProviderInfo);
        return tagDao;
    }

    public override string ConvertId(string id)
    {
        if (id != null)
        {
            var match = Selector.Match(id);
            if (match.Success)
            {
                //return GetInfo(id).ProviderInfo.SpRootFolderId + match.Groups["path"].Value.Replace('|', '/');
                return "" + match.Groups["path"].Value.Replace('|', '/');
            }

            throw new ArgumentException("Id is not a sharepoint id");
        }

        return base.ConvertId(null);
    }
}