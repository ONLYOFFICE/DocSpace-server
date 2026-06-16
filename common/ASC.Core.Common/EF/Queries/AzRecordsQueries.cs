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

namespace ASC.Core.Common.EF;

public partial class UserDbContext
{
    [PreCompileQuery]
    public Task<bool> AnyAclAsync(int tenantId, Guid subject, Guid action, string obj, AceType aceType)
    {
        return Queries.AnyAclAsync(this, tenantId, subject, action, obj, aceType);
    }

    [PreCompileQuery]
    public Task<Acl> AclAsync(int tenantId, Guid subject, Guid action, string obj, AceType aceType)
    {
        return Queries.AclAsync(this, tenantId, subject, action, obj, aceType);
    }

    [PreCompileQuery]
    public IAsyncEnumerable<Acl> AzRecordAsync()
    {
        return Queries.AzRecordAsync(this);
    }
}


static file class Queries
{
    public static readonly Func<UserDbContext, int, Guid, Guid, string, AceType, Task<bool>> AnyAclAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid subject, Guid action, string obj, AceType aceType) =>
                ctx.Acl
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Subject == subject)
                    .Where(r => r.Action == action)
                    .Where(r => r.Object == obj)
                    .Any(r => r.AceType == aceType));

    public static readonly Func<UserDbContext, int, Guid, Guid, string, AceType, Task<Acl>> AclAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid subject, Guid action, string obj, AceType aceType) =>
                ctx.Acl
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.Subject == subject)
                    .Where(r => r.Action == action)
                    .Where(r => r.Object == obj)
                    .FirstOrDefault(r => r.AceType == aceType));

    public static readonly Func<UserDbContext, IAsyncEnumerable<Acl>> AzRecordAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx) =>
                ctx.Acl
                    .Where(r => r.TenantId == Tenant.DefaultTenant));
    //.ToDictionary(a => string.Concat(a.TenantId.ToString(), a.Subject.ToString(), a.Action.ToString(), a.Object)));
}