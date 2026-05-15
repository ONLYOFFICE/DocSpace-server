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

namespace ASC.Files.Core.Security;

/// <summary>
/// The record of the file entry sharing settings.
/// </summary>
public class FileShareRecord<T>
{
    /// <summary>
    /// The ID of the user/group who hasthe access rights to the  file entry.
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// The file entry ID.
    /// </summary>
    public T EntryId { get; set; }

    /// <summary>
    /// The file entry type.
    /// </summary>
    public FileEntryType EntryType { get; set; }

    /// <summary>
    /// The subject type of the access right.
    /// </summary>
    public SubjectType SubjectType { get; set; }

    /// <summary>
    /// The subject ID.
    /// </summary>
    public Guid Subject { get; set; }

    /// <summary>
    /// The ID of the file entry owner.
    /// </summary>
    public Guid Owner { get; set; }

    /// <summary>
    /// The access rights type.
    /// </summary>
    public FileShare Share { get; set; }

    /// <summary>
    /// The parameters of the file shared link.
    /// </summary>
    public FileShareOptions Options { get; set; }

    /// <summary>
    /// The parent ID of the file entry.
    /// </summary>
    public T ParentId { get; set; }

    /// <summary>
    /// The level of the file entry access right.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Specifies if the sharing settings recird is a shared link or not.
    /// </summary>
    public bool IsLink => SubjectType is SubjectType.InvitationLink or SubjectType.ExternalLink or SubjectType.PrimaryExternalLink;

    public class ShareComparer(FolderType rootFolderType) : IComparer<FileShare>
    {
        private static readonly int[] _roomShareOrder =
        [
            (int)FileShare.None,
            (int)FileShare.RoomManager,
            (int)FileShare.ContentCreator,
            (int)FileShare.Editing,
            (int)FileShare.FillForms,
            (int)FileShare.Review,
            (int)FileShare.Comment,
            (int)FileShare.Read,
            
            // Not used
            
            (int)FileShare.ReadWrite,
            (int)FileShare.CustomFilter,
            (int)FileShare.Varies,
            (int)FileShare.Restrict
        ];

        private static readonly int[] _filesShareOrder =
        [
            (int)FileShare.None,
            (int)FileShare.Editing,
            (int)FileShare.CustomFilter,
            (int)FileShare.Review,
            (int)FileShare.FillForms,
            (int)FileShare.Comment,
            (int)FileShare.Read,
            (int)FileShare.Restrict,
            
            // Not used
            (int)FileShare.ReadWrite,
            (int)FileShare.RoomManager,
            (int)FileShare.ContentCreator,
            (int)FileShare.Varies
        ];

        private readonly int[] _shareOrder = rootFolderType is FolderType.VirtualRooms or FolderType.Archive
            ? _roomShareOrder
            : _filesShareOrder;

        public int Compare(FileShare x, FileShare y)
        {
            return Array.IndexOf(_shareOrder, (int)x).CompareTo(Array.IndexOf(_shareOrder, (int)y));
        }
    }
}

/// <summary>
/// The short record of the file entry sharing settings.
/// </summary>
public class SmallShareRecord
{
    /// <summary>
    /// The subject ID.
    /// </summary>
    public Guid Subject { get; set; }

    /// <summary>
    /// The ID of the file entry parent.
    /// </summary>
    public Guid ShareParentTo { get; set; }

    /// <summary>
    /// The ID of the file entry owner.
    /// </summary>
    public Guid Owner { get; set; }

    /// <summary>
    /// The date and time when the sharing setting record was created.
    /// </summary>
    public DateTime TimeStamp { get; set; }

    /// <summary>
    /// The access rights type.
    /// </summary>
    public FileShare Share { get; set; }

    /// <summary>
    /// The subject type of the access right.
    /// </summary>
    public SubjectType SubjectType { get; set; }
}


public static class ShareCompareHelper
{
    private static readonly ConcurrentDictionary<string, Expression> _predicates = new();

    public static Expression<Func<TType, int>> GetCompareExpression<TType>(Expression<Func<TType, FileShare>> memberExpression, FolderType rootFolderType)
    {
        var key = $"{typeof(TType)}-{rootFolderType}";

        if (_predicates.TryGetValue(key, out var value))
        {
            return (Expression<Func<TType, int>>)value;
        }

        var shares = Enum.GetValues<FileShare>()
            .Order(new FileShareRecord<TType>.ShareComparer(rootFolderType))
            .ToList();

        ConditionalExpression expression = null;

        for (var i = shares.Count - 1; i >= 0; i--)
        {
            expression = Expression.Condition(
                Expression.Equal(memberExpression.Body, Expression.Constant(shares[i])), Expression.Constant(i),
                expression != null ? expression : Expression.Constant(i + 1));
        }

        var predicate = Expression.Lambda<Func<TType, int>>(expression!, memberExpression.Parameters[0]);

        _predicates.TryAdd(key, predicate);

        return predicate;
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class FileShareRecordMapper
{
    public static partial FileShareRecord<int> MapToFileShareRecordInternal(this FileShareRecord<string> source);
}