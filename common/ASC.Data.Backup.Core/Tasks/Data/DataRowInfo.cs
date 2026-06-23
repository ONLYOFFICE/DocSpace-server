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

public class DataRowInfo(string tableName)
{
    public string TableName { get; private set; } = tableName;
    public IReadOnlyCollection<string> ColumnNames => _columnNames.AsReadOnly();

    public object this[int index] => _values[index];
    public object this[string columnName] => _values[GetIndex(columnName)];

    private readonly List<string> _columnNames = [];
    private readonly List<object> _values = [];

    public void SetValue(string columnName, object item)
    {
        var index = GetIndex(columnName);
        if (index == -1)
        {
            _columnNames.Add(columnName);
            _values.Add(item);
        }
        else
        {
            _values[index] = item;
        }
    }

    public override string ToString()
    {
        const int maxStrLength = 150;

        var sb = new StringBuilder(maxStrLength);

        var i = 0;
        while (i < _values.Count && sb.Length <= maxStrLength)
        {
            var strVal = Convert.ToString(_values[i]);
            sb.Append($"\"{strVal}\", ");
            i++;
        }

        if (sb.Length > maxStrLength + 2)
        {
            sb.Length = maxStrLength - 3;
            sb.Append("...");
        }
        else if (sb.Length > 0)
        {
            sb.Length -= 2;
        }

        return sb.ToString();
    }

    private int GetIndex(string columnName)
    {
        return _columnNames.FindIndex(name => name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }
}