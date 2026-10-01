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

namespace ASC.Files.Core.Entries;

public class RoomGroup
{
    public int Id { get; init; }
    public string Name { get; set; }
    public string Icon { get; set; }
    public Guid UserID { get; set; }

    /// <summary>
    /// The kind of room the group gathers - see <see cref="RoomGroupArea"/> for how it maps onto the
    /// section the group is shown in.
    /// </summary>
    public FolderType FolderType { get; set; }
}

/// <summary>
/// The Rooms / Forms split as the groups see it. A group is dedicated to one kind of room and stored
/// as such: a group of form-filling rooms belongs to the Forms section, every other group to Rooms.
/// The section survives an empty group, which is why it is stored rather than derived from the rooms
/// still in it.
/// </summary>
public static class RoomGroupArea
{
    /// <summary>
    /// The room kind a group of the given section gathers. Only the two sections that were split own
    /// groups; Rooms is not tied to one kind of room, so it is stored as no kind at all.
    /// </summary>
    public static FolderType ToFolderType(this SearchArea searchArea)
    {
        return searchArea == SearchArea.Forms ? FolderType.FillingFormsRoom : FolderType.DEFAULT;
    }

    /// <summary>
    /// The section a group of the given room kind is shown in.
    /// </summary>
    public static SearchArea ToSearchArea(this FolderType folderType)
    {
        return folderType == FolderType.FillingFormsRoom ? SearchArea.Forms : SearchArea.Active;
    }
}

[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.None, PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class RoomGroupMapper
{
    public static partial RoomGroup MapToRoomGroup(this DbFilesGroup source);
}