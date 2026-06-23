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

namespace ASC.Data.Backup.Extensions;

public class TreeNode<TEntry>()
{
    public TEntry Entry { get; set; }
    public TreeNode<TEntry> Parent { get; set; }
    public List<TreeNode<TEntry>> Children { get; private set; } = [];

    public TreeNode(TEntry entry)
        : this()
    {
        Entry = entry;
        Parent = null;
    }
}

public static class EnumerableExtensions
{
    extension<TEntry>(IEnumerable<TEntry> elements)
    {
        public IEnumerable<TreeNode<TEntry>> ToTree<TKey>(Func<TEntry, TKey> keySelector,
            Func<TEntry, TKey> parentKeySelector)
        {
            ArgumentNullException.ThrowIfNull(elements);
            ArgumentNullException.ThrowIfNull(keySelector);
            ArgumentNullException.ThrowIfNull(parentKeySelector);

            var dic = elements.ToDictionary(keySelector, x => new TreeNode<TEntry>(x));

            foreach (var val in dic.Select(r => r.Value))
            {
                var parentKey = parentKeySelector(val.Entry);
                if (parentKey != null && dic.TryGetValue(parentKeySelector(val.Entry), out var parent))
                {
                    parent.Children.Add(val);
                    val.Parent = parent;
                }
            }

            return dic.Values.Where(x => x.Parent == null);
        }

        public IEnumerable<IEnumerable<TEntry>> MakeParts(int partLength)
        {
            ArgumentNullException.ThrowIfNull(elements);

            if (partLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(partLength), partLength, "Length must be positive integer");
            }

            return MakePartsIterator(elements, partLength);
        }

        private IEnumerable<IEnumerable<TEntry>> MakePartsIterator(int partLength)
        {
            var part = new List<TEntry>(partLength);

            foreach (var entry in elements)
            {
                part.Add(entry);

                if (part.Count == partLength)
                {
                    yield return part.AsEnumerable();
                    part = new List<TEntry>(partLength);
                }
            }

            if (part.Count > 0)
            {
                yield return part.AsEnumerable();
            }
        }
    }
}