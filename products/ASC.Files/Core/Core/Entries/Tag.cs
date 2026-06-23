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

namespace ASC.Files.Core;

/// <summary>
/// The tag type.
/// </summary>
[Flags]
public enum TagType
{
    [Description("New")]
    New = 1,

    [Description("Favorite")]
    Favorite = 2,

    [Description("System")]
    System = 4,

    [Description("Locked")]
    Locked = 8,

    [Description("Recent")]
    Recent = 16,

    [Description("Template")]
    Template = 32,

    [Description("Custom")]
    Custom = 64,

    [Description("Pin")]
    Pin = 128,

    [Description("Origin")]
    Origin = 256,

    [Description("Recent by link")]
    RecentByLink = 512,

    [Description("From room")]
    FromRoom = 1024,

    [Description("Custom filter")]
    CustomFilter = 2048
}

/// <summary>
/// The tag information.
/// </summary>
[DebuggerDisplay("{Name} ({Id}) entry {EntryType} ({EntryId})")]
public sealed class Tag
{
    /// <summary>
    /// The tag name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// The tag type.
    /// </summary>
    public TagType Type { get; set; }

    /// <summary>
    /// The tag owner ID.
    /// </summary>
    public Guid Owner { get; set; }

    /// <summary>
    /// The tag entry ID.
    /// </summary>
    public object EntryId { get; set; }

    /// <summary>
    /// The file entry type for which the tag has been created.
    /// </summary>
    public FileEntryType EntryType { get; set; }

    /// <summary>
    /// The tag ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The number of tags.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// The date and time when the tag was created.
    /// </summary>
    public DateTime? CreateOn { get; set; }

    public Tag() { }

    public Tag(string name, TagType type, Guid owner, int count = 0)
    {
        Name = name;
        Type = type;
        Owner = owner;
        Count = count;
    }

    public Tag AddEntry<T>(FileEntry<T> entry)
    {
        if (entry != null)
        {
            EntryId = entry.Id;
            EntryType = entry.FileEntryType;
        }

        return this;
    }

    public static Tag New<T>(Guid owner, FileEntry<T> entry, int count = 1)
    {
        return new Tag("new", TagType.New, owner, count).AddEntry(entry);
    }

    public static Tag Recent<T>(Guid owner, FileEntry<T> entry)
    {
        return new Tag("recent", TagType.Recent, owner).AddEntry(entry);
    }

    public static Tag Favorite<T>(Guid owner, FileEntry<T> entry)
    {
        return new Tag("favorite", TagType.Favorite, owner).AddEntry(entry);
    }

    public static Tag Template<T>(Guid owner, FileEntry<T> entry)
    {
        return new Tag("template", TagType.Template, owner).AddEntry(entry);
    }

    public static Tag Custom<T>(Guid owner, FileEntry<T> entry, string name)
    {
        return new Tag(name, TagType.Custom, owner).AddEntry(entry);
    }

    public static Tag Pin<T>(Guid owner, FileEntry<T> entry)
    {
        return new Tag("pin", TagType.Pin, owner).AddEntry(entry);
    }

    public static Tag FromRoom<T>(T entryId, FileEntryType type, Guid owner)
    {
        return new Tag("fromroom", TagType.FromRoom, owner)
        {
            EntryId = entryId,
            EntryType = type
        };
    }

    public static Tag Origin<T>(T entryId, FileEntryType type, T originId, Guid owner)
    {
        return new Tag(originId.ToString(), TagType.Origin, owner)
        {
            EntryId = entryId,
            EntryType = type
        };
    }

    public static Tag RecentByLink<T>(Guid owner, Guid linkId, FileEntry<T> file)
    {
        return new Tag(linkId.ToString(), TagType.RecentByLink, owner).AddEntry(file);
    }

    public override bool Equals(object obj)
    {
        return obj is Tag f && Equals(f);
    }

    public bool Equals(Tag f)
    {
        return f != null && f.Id == Id && f.EntryType == EntryType && Equals(f.EntryId, EntryId);
    }

    public override int GetHashCode()
    {
        return (Id + EntryType + EntryId.ToString()).GetHashCode();
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class TagMapper
{
    public static partial Tag Map(this DbFilesTag source);

    public static partial void ApplyUpdate(DbFilesTagLink link, Tag tag);
}