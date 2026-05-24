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

namespace ASC.Notify.Cron;

public class TreeSet : ArrayList, ISortedSet
{
    public IComparer Comparator { get; } = Comparer.Default;

    public TreeSet() { }

    public TreeSet(ICollection c)
    {
        AddAll(c);
    }

    public TreeSet(IComparer c)
    {
        Comparator = c;
    }
    public new bool Add(object obj)
    {
        var inserted = AddWithoutSorting(obj);
        Sort(Comparator);
        return inserted;
    }

    public bool AddAll(ICollection c)
    {
        var e = new ArrayList(c).GetEnumerator();
        var added = false;
        while (e.MoveNext())
        {
            if (AddWithoutSorting(e.Current))
            {
                added = true;
            }
        }
        Sort(Comparator);

        return added;
    }

    public object First()
    {
        return this[0];
    }

    public override bool Contains(object item)
    {
        var tempEnumerator = GetEnumerator();
        while (tempEnumerator.MoveNext())
        {
            if (Comparator.Compare(tempEnumerator.Current, item) == 0)
            {
                return true;
            }
        }

        return false;
    }

    public ISortedSet TailSet(object limit)
    {
        var newList = new TreeSet();
        var i = 0;
        while (i < Count && Comparator.Compare(this[i], limit) < 0)
        {
            i++;
        }

        for (; i < Count; i++)
        {
            newList.Add(this[i]);
        }

        return newList;
    }

    public static TreeSet UnmodifiableTreeSet(ICollection collection)
    {
        var items = new ArrayList(collection);
        items = ReadOnly(items);

        return new TreeSet(items);
    }

    private bool AddWithoutSorting(object obj)
    {
        var inserted = Contains(obj);
        if (!inserted)
        {
            base.Add(obj);
        }

        return !inserted;
    }
}