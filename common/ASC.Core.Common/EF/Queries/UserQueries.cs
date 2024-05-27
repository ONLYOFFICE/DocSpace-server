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
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<DateTime?> LastModifiedAsync(int tenantId, Guid userId)
    {
        return Queries.LastModifiedAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<byte[]> PhotoAsync(int tenantId, Guid userId)
    {
        return Queries.PhotoAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultDateTime])]
    public IAsyncEnumerable<int> TenantIdsAsync(DateTime from)
    {
        return Queries.TenantIdsAsync(this, from);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public IAsyncEnumerable<Guid> GroupIdsAsync(int tenantId, Guid parentId)
    {
        return Queries.GroupIdsAsync(this, tenantId, parentId);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteAclByIdsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return Queries.DeleteAclByIdsAsync(this, tenantId, ids);
    }
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteSubscriptionsByIdsAsync(int tenantId, IEnumerable<string> ids)
    {
        return Queries.DeleteSubscriptionsByIdsAsync(this, tenantId, ids);
    }
    
        
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteDbSubscriptionMethodsByIdsAsync(int tenantId, IEnumerable<string> ids)
    {
        return Queries.DeleteDbSubscriptionMethodsByIdsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteUserGroupsByIdsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return Queries.DeleteUserGroupsByIdsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> UpdateUserGroupsByIdsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return Queries.UpdateUserGroupsByIdsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteDbGroupsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return Queries.DeleteDbGroupsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> UpdateDbGroupsAsync(int tenantId, IEnumerable<Guid> ids)
    {
        return Queries.UpdateDbGroupsAsync(this, tenantId, ids);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteAclAsync(int tenantId, Guid id)
    {
        return Queries.DeleteAclAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteSubscriptionsAsync(int tenantId, string id)
    {
        return Queries.DeleteSubscriptionsAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<int> DeleteDbSubscriptionMethodsAsync(int tenantId, string id)
    {
        return Queries.DeleteDbSubscriptionMethodsAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteUserPhotosAsync(int tenantId, Guid userId)
    {
        return Queries.DeleteUserPhotosAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([null])]
    public Task<int> DeleteAccountLinksAsync(string id)
    {
        return Queries.DeleteAccountLinksAsync(this, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteUserGroupsAsync(int tenantId, Guid userId)
    {
        return Queries.DeleteUserGroupsAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> UpdateUserGroupsAsync(int tenantId, Guid userId)
    {
        return Queries.UpdateUserGroupsAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteUsersAsync(int tenantId, Guid userId)
    {
        return Queries.DeleteUsersAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> UpdateUsersAsync(int tenantId, Guid userId)
    {
        return Queries.UpdateUsersAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<int> DeleteUserSecuritiesAsync(int tenantId, Guid userId)
    {
        return Queries.DeleteUserSecuritiesAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid, UserGroupRefType.Contains])]
    public Task<int> DeleteUserGroupsByGroupIdAsync(int tenantId, Guid userId, Guid groupId, UserGroupRefType refType)
    {
        return Queries.DeleteUserGroupsByGroupIdAsync(this, tenantId, userId, groupId, refType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid, PreCompileQuery.DefaultGuid, UserGroupRefType.Contains])]
    public Task<int> UpdateUserGroupsByGroupIdAsync(int tenantId, Guid userId, Guid groupId, UserGroupRefType refType)
    {
        return Queries.UpdateUserGroupsByGroupIdAsync(this, tenantId, userId, groupId, refType);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<User> UserAsync(int tenantId, Guid userId)
    {
        return Queries.UserAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<User> UserByIdAsync(int tenantId, Guid userId)
    {
        return Queries.UserByIdAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<UserGroup> GroupsByTenantAsync(int tenantId)
    {
        return Queries.GroupsByTenantAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<User> UserByEmailAsync(int tenantId, string email)
    {
        return Queries.UserByEmailAsync(this, tenantId, email);
    }
    
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null])]
    public Task<User> UserByUserNameAsync(int tenantId, string userName)
    {
        return Queries.UserByUserNameAsync(this, tenantId, userName);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<User> UserByTenantAsync(int tenantId)
    {
        return Queries.UserByTenantAsync(this, tenantId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultGuid])]
    public Task<bool> AnyUsersAsync(int tenantId, string userName, Guid id)
    {
        return Queries.AnyUsersAsync(this, tenantId, userName, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, null, PreCompileQuery.DefaultGuid])]
    public Task<bool> AnyUsersByEmailAsync(int tenantId, string email, Guid id)
    {
        return Queries.AnyUsersByEmailAsync(this, tenantId, email, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<User> FirstOrDefaultUserAsync(int tenantId, Guid id)
    {
        return Queries.FirstOrDefaultUserAsync(this, tenantId, id);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt, PreCompileQuery.DefaultGuid])]
    public Task<UserPhoto> UserPhotoAsync(int tenantId, Guid userId)
    {
        return Queries.UserPhotoAsync(this, tenantId, userId);
    }
    
    [PreCompileQuery([PreCompileQuery.DefaultInt])]
    public IAsyncEnumerable<string> EmailsAsync(int tenantId)
    {
        return Queries.EmailsAsync(this, tenantId);
    }
}

static file class Queries
{
    public static readonly Func<UserDbContext, int, Guid, Task<DateTime?>> LastModifiedAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserSecurity
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId)
                    .Select(r => r.LastModified)
                    .FirstOrDefault());

    public static readonly Func<UserDbContext, int, Guid, Task<byte[]>> PhotoAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Photos
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.UserId == userId)
                    .Select(r => r.Photo)
                    .FirstOrDefault());

    public static readonly Func<UserDbContext, DateTime, IAsyncEnumerable<int>> TenantIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, DateTime from) =>
                ctx.Users
                    .Where(u => u.LastModified > from)
                    .Select(u => u.TenantId)
                    .Distinct());

    public static readonly Func<UserDbContext, int, Guid, IAsyncEnumerable<Guid>> GroupIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid parentId) =>
                ctx.Groups
                    .Where(r => r.TenantId == tenantId)
                    .Where(r => r.ParentId == parentId)
                    .Select(r => r.Id));

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> DeleteAclByIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Acl
                    .Where(r => r.TenantId == tenantId && ids.Any(i => i == r.Subject))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<string>, Task<int>> DeleteSubscriptionsByIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<string> ids) =>
                ctx.Subscriptions
                    .Where(r => r.TenantId == tenantId && ids.Any(i => i == r.Recipient))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<string>, Task<int>> DeleteDbSubscriptionMethodsByIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<string> ids) =>
                ctx.SubscriptionMethods
                    .Where(r => r.TenantId == tenantId && ids.Any(i => i == r.Recipient))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> DeleteUserGroupsByIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId && ids.Any(i => i == r.UserGroupId))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> UpdateUserGroupsByIdsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId && ids.Any(i => i == r.UserGroupId))
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> DeleteDbGroupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Groups
                    .Where(r => r.TenantId == tenantId && ids.Any(i => i == r.Id))
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, IEnumerable<Guid>, Task<int>> UpdateDbGroupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, IEnumerable<Guid> ids) =>
                ctx.Groups
                    .Where(r => r.TenantId == tenantId && ids.Any(i => i == r.Id))
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteAclAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Acl
                    .Where(r => r.TenantId == tenantId && r.Subject == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, string, Task<int>> DeleteSubscriptionsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string id) =>
                ctx.Subscriptions
                    .Where(r => r.TenantId == tenantId && r.Recipient == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, string, Task<int>> DeleteDbSubscriptionMethodsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string id) =>
                ctx.SubscriptionMethods
                    .Where(r => r.TenantId == tenantId && r.Recipient == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUserPhotosAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Photos
                    .Where(r => r.TenantId == tenantId && r.UserId == userId)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, string, Task<int>> DeleteAccountLinksAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, string id) =>
                ctx.AccountLinks
                    .Where(r => r.Id == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUserGroupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId && r.Userid == userId)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> UpdateUserGroupsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId && r.Userid == userId)
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                        .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUsersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Users.Where(r => r.TenantId == tenantId && r.Id == id)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Task<int>> UpdateUsersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Users.Where(r => r.TenantId == tenantId && r.Id == id)
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)
                                         .SetProperty(p => p.TerminatedDate, DateTime.UtcNow)
                                         .SetProperty(p => p.Status, EmployeeStatus.Terminated)));

    public static readonly Func<UserDbContext, int, Guid, Task<int>> DeleteUserSecuritiesAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.UserSecurity
                    .Where(r => r.TenantId == tenantId && r.UserId == userId)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Guid, UserGroupRefType, Task<int>> DeleteUserGroupsByGroupIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId, Guid groupId, UserGroupRefType refType) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && r.Userid == userId
                                && r.UserGroupId == groupId
                                && r.RefType == refType)
                    .ExecuteDelete());

    public static readonly Func<UserDbContext, int, Guid, Guid, UserGroupRefType, Task<int>> UpdateUserGroupsByGroupIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId, Guid groupId, UserGroupRefType refType) =>
                ctx.UserGroups
                    .Where(r => r.TenantId == tenantId
                                && r.Userid == userId
                                && r.UserGroupId == groupId
                                && r.RefType == refType)
                    .ExecuteUpdate(q => q.SetProperty(p => p.Removed, true)
                                         .SetProperty(p => p.LastModified, DateTime.UtcNow)));

    public static readonly Func<UserDbContext, int, Guid, Task<User>> UserAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Users.First(r => r.TenantId == tenantId && r.Id == userId));
    
    public static readonly Func<UserDbContext, int, Guid, Task<User>> UserByIdAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Users.FirstOrDefault(r => r.TenantId == tenantId && r.Id == userId));
    
    public static readonly Func<UserDbContext, int, IAsyncEnumerable<UserGroup>> GroupsByTenantAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId) =>
                ctx.UserGroups.Where(r => tenantId == Tenant.DefaultTenant || r.TenantId == tenantId));
    
    public static readonly Func<UserDbContext, int, string, Task<User>> UserByEmailAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string email) =>
                ctx.Users.FirstOrDefault(r => r.TenantId == tenantId && r.Email == email && !r.Removed));
    
    public static readonly Func<UserDbContext, int, string, Task<User>> UserByUserNameAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string userName) =>
                ctx.Users.FirstOrDefault(r => r.TenantId == tenantId && r.UserName == userName && !r.Removed));
    
    public static readonly Func<UserDbContext, int, IAsyncEnumerable<User>> UserByTenantAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId) =>
                ctx.Users.Where(r => r.TenantId == tenantId));

    public static readonly Func<UserDbContext, int, string, Guid, Task<bool>> AnyUsersAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string userName, Guid id) =>
                ctx.Users
                    .Where(r => tenantId == Tenant.DefaultTenant || r.TenantId == tenantId)
                    .Any(r => r.UserName == userName && r.Id != id && !r.Removed));

    public static readonly Func<UserDbContext, int, string, Guid, Task<bool>> AnyUsersByEmailAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, string email, Guid id) =>
                ctx.Users
                    .Where(r => tenantId == Tenant.DefaultTenant || r.TenantId == tenantId)
                    .Any(r => r.Email == email && r.Id != id && !r.Removed));

    public static readonly Func<UserDbContext, int, Guid, Task<User>> FirstOrDefaultUserAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid id) =>
                ctx.Users
                    .Where(r => tenantId == Tenant.DefaultTenant || r.TenantId == tenantId)
                    .FirstOrDefault(a => a.Id == id));

    public static readonly Func<UserDbContext, int, Guid, Task<UserPhoto>> UserPhotoAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId, Guid userId) =>
                ctx.Photos.FirstOrDefault(r => r.UserId == userId && r.TenantId == tenantId));

    public static readonly Func<UserDbContext, int, IAsyncEnumerable<string>> EmailsAsync =
        Microsoft.EntityFrameworkCore.EF.CompileAsyncQuery(
            (UserDbContext ctx, int tenantId) =>
                (from usersDav in ctx.UsersDav
                 join users in ctx.Users on new { tenant = usersDav.TenantId, userId = usersDav.UserId } equals new
                 {
                     tenant = users.TenantId,
                     userId = users.Id
                 }
                 where usersDav.TenantId == tenantId
                 select users.Email)
                .Distinct());
}