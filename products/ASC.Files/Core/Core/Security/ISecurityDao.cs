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

public interface ISecurityDao<T>
{
    Task SetShareAsync(FileShareRecord<T> r);
    IAsyncEnumerable<FileShareRecord<T>> GetSharesAsync(IEnumerable<Guid> subjects, Guid? ownerId = null);
    Task<IEnumerable<FileShareRecord<T>>> GetSharesAsync(FileEntry<T> entry, HashSet<Guid> subjects = null);
    Task RemoveBySubjectAsync(Guid subject, bool withoutOwner);
    Task RemoveSecuritiesAsync(Guid subject, Guid ownerId, SubjectType subjectType);
    IAsyncEnumerable<FileShareRecord<T>> GetPureShareRecordsAsync(IEnumerable<FileEntry<T>> entries);
    IAsyncEnumerable<FileShareRecord<T>> GetPureShareRecordsAsync(FileEntry<T> entry);
    Task DeleteShareRecordsAsync(IEnumerable<FileShareRecord<T>> records);
    Task<int> GetPureSharesCountAsync(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text);
    IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text, int offset = 0, int count = -1);
    IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync(FileEntry<T> entry, IEnumerable<Guid> subjects);
    Task<bool> IsPublicAsync(FileEntry<T> entry);
    IAsyncEnumerable<UserInfoWithShared> GetUsersWithSharedAsync(FileEntry<T> entry, string text, EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus, bool excludeShared, bool includeShared, string separator, bool includeStrangers, Area area, bool? invitedByMe, Guid? inviterId, IEnumerable<EmployeeType> employeeTypes, IEnumerable<Guid> parentUserIds, int offset = 0, int count = -1);
    Task<int> GetUsersWithSharedCountAsync(FileEntry<T> entry, string text, EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus, bool excludeShared, bool includeShared, string separator, bool includeStrangers, Area area, bool? invitedByMe, Guid? inviterId, IEnumerable<EmployeeType> employeeTypes, IEnumerable<Guid> parentUserIds);
    IAsyncEnumerable<GroupInfoWithShared> GetGroupsWithSharedAsync(FileEntry<T> entry, string text, bool excludeShared, int offset, int count, IEnumerable<Guid> parentUserIds);
    Task<int> GetGroupsWithSharedCountAsync(FileEntry<T> entry, string text, bool excludeShared, IEnumerable<Guid> parentUserIds);
    IAsyncEnumerable<GroupMemberSecurityRecord> GetGroupMembersWithSecurityAsync(FileEntry<T> entry, Guid groupId, string text, int offset, int count);
    Task<int> GetGroupMembersWithSecurityCountAsync(FileEntry<T> entry, Guid groupId, string text);
    Task<int> UpdateShareByFolderTypesAsync(Guid subject, IEnumerable<FolderType> folderTypes, FileShare share);
}
