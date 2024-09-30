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

namespace ASC.Files.Core.Security;

public interface ISecurityDao<T>
{
    Task SetShareAsync(FileShareRecord<T> r);
    IAsyncEnumerable<FileShareRecord<T>> GetSharesAsync(IEnumerable<Guid> subjects);
    Task<IEnumerable<FileShareRecord<T>>> GetSharesAsync(FileEntry<T> entry, IEnumerable<Guid> subjects = null);
    Task RemoveBySubjectAsync(Guid subject, bool withoutOwner);
    IAsyncEnumerable<FileShareRecord<T>> GetPureShareRecordsAsync(IEnumerable<FileEntry<T>> entries);
    IAsyncEnumerable<FileShareRecord<T>> GetPureShareRecordsAsync(FileEntry<T> entry);
    Task DeleteShareRecordsAsync(IEnumerable<FileShareRecord<T>> records);
    Task<bool> IsPureSharedAsync(T entryId, FileEntryType type, IEnumerable<SubjectType> subjectTypes);
    Task<bool> IsSharedAsync(FileEntry<T> entry, IEnumerable<SubjectType> subjectTypes);
    Task<int> GetPureSharesCountAsync(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text);
    IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync(FileEntry<T> entry, ShareFilterType filterType, EmployeeActivationStatus? status, string text, int offset = 0, int count = -1);
    IAsyncEnumerable<FileShareRecord<T>> GetPureSharesAsync(FileEntry<T> entry, IEnumerable<Guid> subjects);
    IAsyncEnumerable<UserInfoWithShared> GetUsersWithSharedAsync(FileEntry<T> entry, string text, EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus, bool excludeShared, string separator, bool includeStrangers, Area area, int offset = 0, int count = -1);
    Task<int> GetUsersWithSharedCountAsync(FileEntry<T> entry, string text, EmployeeStatus? employeeStatus, EmployeeActivationStatus? activationStatus, bool excludeShared, string separator, bool includeStrangers, Area area);
    IAsyncEnumerable<GroupInfoWithShared> GetGroupsWithSharedAsync(FileEntry<T> entry, string text, bool excludeShared, int offset, int count);
    Task<int> GetGroupsWithSharedCountAsync(FileEntry<T> entry, string text, bool excludeShared);
    IAsyncEnumerable<GroupMemberSecurityRecord> GetGroupMembersWithSecurityAsync(FileEntry<T> entry, Guid groupId, string text, int offset, int count);
    Task<int> GetGroupMembersWithSecurityCountAsync(FileEntry<T> entry, Guid groupId, string text);
}
