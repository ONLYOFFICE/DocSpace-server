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

public interface IUserService
{
    Task<IEnumerable<UserInfo>> GetUsersAsync(int tenant);
    Task<int> GetUsersCountAsync(UserQueryFilter filter);
    IAsyncEnumerable<UserInfo> GetUsers(UserQueryFilter filter);
    Task<byte[]> GetUserPhotoAsync(int tenant, Guid id);
    Task<DateTime> GetUserPasswordStampAsync(int tenant, Guid id);
    Task<Group> GetGroupAsync(int tenant, Guid id);
    Task<Group> SaveGroupAsync(int tenant, Group group);
    Task<IDictionary<string, UserGroupRef>> GetUserGroupRefsAsync(int tenant);
    Task<IEnumerable<Group>> GetGroupsAsync(int tenant);
    IAsyncEnumerable<Group> GetGroupsAsync(int tenant, string text, Guid userId, bool manager, GroupSortType sortBy, bool sortOrderAsc, int offset = 0, int count = -1);
    Task<int> GetGroupsCountAsync(int tenant, string text, Guid userId, bool manager);
    Task<IEnumerable<UserInfo>> GetUsersAllTenantsAsync(IEnumerable<Guid> userIds);
    Task<IEnumerable<UserInfo>> GetUsersAllTenantsAsync(string email, EmployeeActivationStatus? status);
    Task<UserGroupRef> GetUserGroupRefAsync(int tenant, Guid groupId, UserGroupRefType refType);
    Task<UserGroupRef> SaveUserGroupRefAsync(int tenant, UserGroupRef r);
    Task<UserInfo> GetUserAsync(int tenant, Guid id);
    UserInfo GetUser(int tenant, Guid id);
    Task<UserInfo> GetUserAsync(int tenant, Guid id, Expression<Func<User, UserInfo>> exp);
    Task<UserInfo> GetUserAsync(int tenant, string email);
    Task<UserInfo> GetUserByPasswordHashAsync(int tenant, string login, string passwordHash);
    Task<UserInfo> GetUserByUserName(int tenant, string userName);
    Task<UserInfo> SaveUserAsync(int tenant, UserInfo user);
    Task RemoveGroupAsync(int tenant, Guid id);
    Task RemoveUserAsync(int tenant, Guid id, bool immediate = false);
    Task<IEnumerable<string>> GetDavUserEmailsAsync(int tenant);
    Task RemoveUserGroupRefAsync(int tenant, Guid userId, Guid groupId, UserGroupRefType refType);
    Task SetUserPasswordHashAsync(int tenant, Guid id, string passwordHash);
    Task SetUserPhotoAsync(int tenant, Guid id, byte[] photo);
    Task SaveUsersRelationAsync(int tenantId, Guid sourceUserId, Guid targetUserId);
    Task<Dictionary<Guid, UserRelation>> GetUserRelationsAsync(int tenantId, Guid sourceUserId);
    Task<Dictionary<Guid, UserRelation>> GetUserRelationsByTargetAsync(int tenantId, Guid targetUserId);
    Task DeleteUserRelationAsync(int tenantId, Guid sourceUserId, Guid targetUserId);

    Task<InvitationLink> CreateInvitationLinkAsync(int tenantId, EmployeeType employeeType, DateTime expiration, int? maxUseCount);
    Task<InvitationLink> GetInvitationLinkAsync(int tenantId, Guid id);
    Task<InvitationLink> GetInvitationLinkAsync(int tenantId, EmployeeType employeeType);
    Task<List<InvitationLink>> GetInvitationLinksAsync(int tenantId);
    Task UpdateInvitationLinkAsync(int tenantId, Guid id, DateTime expiration, int? maxUseCount);
    Task IncreaseInvitationLinkUsageAsync(int tenantId, Guid id);
    Task DeleteInvitationLinkAsync(int tenantId, Guid id);
}