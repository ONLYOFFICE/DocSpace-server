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

namespace ASC.Migration.Core.Migrators.Model;
public class MigrationStorage
{
    public List<MigrationFolder> Folders { get; set; } = [];
    public List<MigrationFile> Files { get; set; } = [];
    public List<MigrationSecurity> Securities { get; set; } = [];
    public long BytesTotal { get; set; }
    public FolderType Type { get; set; } = FolderType.USER;
    public string RootKey { get; set; }

    public bool ShouldImport { get; set; }
    public bool ShouldImportSharedFiles { get; set; }
    public bool ShouldImportSharedFolders { get; set; }

    public MigratingApiFiles ToApiInfo()
    {
        return new MigratingApiFiles
        {
            BytesTotal = BytesTotal,
            FilesCount = Files.Count,
            FoldersCount = Folders.Count
        };
    }
}

public enum ShareBy
{
    EmailAndName,
    Id
}

public class MigrationFile
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int Version { get; set; }
    public int VersionGroup { get; set; }
    public string Comment { get; set; }
    public int Folder { get; set; }
    public string Path { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

public class MigrationFolder
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Title { get; set; }
    public int Level { get; set; }

    /*only projects*/

    public bool Private { get; set; }
    public string Owner { get; set; }
}

public class MigrationSecurity
{
    public int EntryId { get; set; }
    public int EntryType { get; set; }
    public string Subject { get; set; }
    public int Security { get; set; }
}