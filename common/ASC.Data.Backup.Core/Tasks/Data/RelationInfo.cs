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

namespace ASC.Data.Backup.Tasks.Data;

public enum RelationImportance
{
    Low,
    Normal
}

[DebuggerDisplay("({ParentTable},{ParentColumn})->({ChildTable},{ChildColumn})")]
public class RelationInfo(string parentTable, string parentColumn, string childTable, string childColumn, Type parentModule, Func<DataRowInfo, bool> collisionResolver, RelationImportance importance)
{
    public string ParentTable { get; private set; } = parentTable;
    public string ParentColumn { get; private set; } = parentColumn;
    public string ChildTable { get; private set; } = childTable;
    public string ChildColumn { get; private set; } = childColumn;
    public Type ParentModule { get; private set; } = parentModule;
    public RelationImportance Importance { get; private set; } = importance;
    public Func<DataRowInfo, bool> CollisionResolver { get; private set; } = collisionResolver;


    public RelationInfo(string parentTable, string parentColumn, string childTable, string childColumn, Func<DataRowInfo, bool> collisionResolver)
        : this(parentTable, parentColumn, childTable, childColumn, null, collisionResolver) { }

    public RelationInfo(string parentTable, string parentColumn, string childTable, string childColumn, Type parentModule)
        : this(parentTable, parentColumn, childTable, childColumn, parentModule, null) { }

    public RelationInfo(string parentTable, string parentColumn, string childTable, string childColumn, Type parentModule = null, Func<DataRowInfo, bool> collisionResolver = null)
        : this(parentTable, parentColumn, childTable, childColumn, parentModule, collisionResolver, RelationImportance.Normal) { }

    public bool FitsForTable(string tableName)
    {
        return string.Equals(tableName, ChildTable, StringComparison.InvariantCultureIgnoreCase);
    }

    public bool FitsForRow(DataRowInfo row)
    {
        return FitsForTable(row.TableName) && (CollisionResolver == null || CollisionResolver(row));
    }

    public bool IsExternal()
    {
        return ParentModule != null;
    }

    public bool IsSelfRelation()
    {
        return string.Equals(ParentTable, ChildTable, StringComparison.InvariantCultureIgnoreCase);
    }
}