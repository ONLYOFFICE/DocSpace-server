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

namespace ASC.Core;

[DebuggerDisplay("{UserId} - {GroupId}")]
[ProtoContract]
public class UserGroupRef
{
    [ProtoMember(1)]
    public Guid UserId { get; set; }

    [ProtoMember(2)]
    public Guid GroupId { get; set; }

    [ProtoMember(3)]
    public bool Removed { get; set; }

    [ProtoMember(4)]
    public DateTime LastModified { get; set; }

    [ProtoMember(5)]
    public UserGroupRefType RefType { get; set; }

    [ProtoMember(6)]
    public int TenantId { get; set; }

    public UserGroupRef() { }

    public UserGroupRef(Guid userId, Guid groupId, UserGroupRefType refType)
    {
        UserId = userId;
        GroupId = groupId;
        RefType = refType;
    }

    public static string CreateKey(int tenant, Guid userId, Guid groupId, UserGroupRefType refType)
    {
        return tenant + userId.ToString("N") + groupId.ToString("N") + (int)refType;
    }

    public string CreateKey()
    {
        return CreateKey(TenantId, UserId, GroupId, RefType);
    }

    public override int GetHashCode()
    {
        return UserId.GetHashCode() ^ GroupId.GetHashCode() ^ TenantId.GetHashCode() ^ RefType.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is UserGroupRef r && r.TenantId == TenantId && r.UserId == UserId && r.GroupId == GroupId && r.RefType == RefType;
    }
}

[Mapper(PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public static partial class UserGroupRefMapper
{
    [MapperIgnoreSource(nameof(UserGroup.Tenant))]
    [MapProperty(nameof(UserGroup.UserGroupId), nameof(UserGroupRef.GroupId))]
    public static partial UserGroupRef Map(this UserGroup source);
    public static partial IQueryable<UserGroupRef> Project(this IQueryable<UserGroup> source);
    public static partial List<UserGroupRef> Map(this List<UserGroup> source);
}