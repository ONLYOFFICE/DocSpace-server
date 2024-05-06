// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

namespace ASC.Core.Common.EF;

public partial class UserDbContext
{
    public Task<bool> AnyAclAsync(int tenantId, Guid subject, Guid action, string obj, AceType aceType)
    {
        return Queries.AnyAclAsync(this, tenantId, subject, action, obj, aceType);
    }
    
    public Task<Acl> AclAsync(int tenantId, Guid subject, Guid action, string obj, AceType aceType)
    {
        return Queries.AclAsync(this, tenantId, subject, action, obj, aceType);
    }
    
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