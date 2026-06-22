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

namespace ASC.Files.Core.Mapping;

[Scope]
public class FilesMappingAction(TenantUtil tenantUtil)
{
    public void Process(Folder<int> destination)
    {
        switch (destination.FolderType)
        {
            case FolderType.COMMON:
                destination.Title = FilesUCResource.CorporateFiles;
                break;
            case FolderType.USER:
                destination.Title = FilesUCResource.MyFiles;
                break;
            case FolderType.SHARE:
                destination.Title = FilesUCResource.SharedForMe;
                break;
            case FolderType.Recent:
                destination.Title = FilesUCResource.Recent;
                break;
            case FolderType.Favorites:
                destination.Title = FilesUCResource.Favorites;
                break;
            case FolderType.TRASH:
                destination.Title = FilesUCResource.Trash;
                break;
            case FolderType.Privacy:
                destination.Title = FilesUCResource.PrivacyRoom;
                break;
            case FolderType.Projects:
                destination.Title = FilesUCResource.ProjectFiles;
                break;
            case FolderType.VirtualRooms:
                destination.Title = FilesUCResource.VirtualRooms;
                break;
            case FolderType.RoomTemplates:
                destination.Title = FilesUCResource.RoomTemplates;
                break;
            case FolderType.Archive:
                destination.Title = FilesUCResource.Archive;
                break;
            case FolderType.ReadyFormFolder:
                destination.Title = FilesUCResource.ReadyFormFolder;
                break;
            case FolderType.InProcessFormFolder:
                destination.Title = FilesUCResource.InProcessFormFolder;
                break;
            case FolderType.AiAgents:
                destination.Title = FilesUCResource.AiAgents;
                break;
            case FolderType.Forms:
                destination.Title = FilesUCResource.Forms;
                break;
            case FolderType.BUNCH:
                try
                {
                    destination.Title = string.Empty;
                }
                catch (Exception)
                {
                    //Global.Logger.Error(e);
                }
                break;
        }

        if (destination.FolderType != FolderType.DEFAULT)
        {
            if (0.Equals(destination.ParentId))
            {
                destination.RootFolderType = destination.FolderType;
            }

            if (destination.RootCreateBy == Guid.Empty)
            {
                destination.RootCreateBy = destination.CreateBy;
            }

            if (0.Equals(destination.RootId))
            {
                destination.RootId = destination.Id;
            }
        }
    }

    public void Process(FileShareRecord<int> source, DbFilesSecurity destination)
    {
        Process<int>(source, destination);
    }

    public void Process(FileShareRecord<string> source, DbFilesSecurity destination)
    {
        Process<string>(source, destination);
    }

    private void Process<T>(FileShareRecord<T> source, DbFilesSecurity destination)
    {
        source.Options?.ExpirationDate = tenantUtil.DateTimeToUtc(source.Options.ExpirationDate);
    }

}