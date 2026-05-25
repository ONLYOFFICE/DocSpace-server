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

// namespace ASC.People.Api;
//
// public class ContactsController(
//     UserManager userManager,
//     PermissionContext permissionContext,
//     ApiContext apiContext,
//     UserPhotoManager userPhotoManager,
//     IHttpClientFactory httpClientFactory,
//     EmployeeFullDtoHelper employeeFullDtoHelper,
//     TenantManager tenantManager,
//     AuthContext authContext,
//     IHttpContextAccessor httpContextAccessor)
//     : PeopleControllerBase(userManager, permissionContext, apiContext, userPhotoManager, httpClientFactory, httpContextAccessor)
// {
//     /// <remarks>
//     /// Deletes the contacts of the user with the ID specified in the request from the portal.
//     /// </remarks>
//     /// <summary>
//     /// Delete user contacts
//     /// </summary>
//     /// <path>api/2.0/people/{userid}/contacts</path>
//     [Tags("People / Contacts")]
//     [SwaggerResponse(200, "Deleted user profile with the detailed information", typeof(EmployeeFullDto))]
//     [SwaggerResponse(403, "No permissions to perform this action")]
//     [SwaggerResponse(404, "User not found")]
//     [HttpDelete("{userid}/contacts")]
//     public async Task<EmployeeFullDto> DeleteMemberContacts(ContactsRequestDto inDto)
//     {
//         var user = await  GetUserInfoAsync(inDto.UserId);
//
//         if (_userManager.IsSystemUser(user.Id))
//         {
//             throw new SecurityException();
//         }
//
//         await DeleteContactsAsync(inDto.Contacts.Contacts, user);
//         await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
//
//         return await employeeFullDtoHelper.GetFullAsync(user);
//     }
//
//     /// <remarks>
//     /// Sets the contacts of the user with the ID specified in the request replacing the current portal data with the new data.
//     /// </remarks>
//     /// <summary>
//     /// Set user contacts
//     /// </summary>
//     /// <path>api/2.0/people/{userid}/contacts</path>
//     [Tags("People / Contacts")]
//     [SwaggerResponse(200, "Updated user profile with the detailed information", typeof(EmployeeFullDto))]
//     [SwaggerResponse(403, "No permissions to perform this action")]
//     [SwaggerResponse(404, "User not found")]
//     [HttpPost("{userid}/contacts")]
//     public async Task<EmployeeFullDto> SetMemberContacts(ContactsRequestDto inDto)
//     {
//         var user = await GetUserInfoAsync(inDto.UserId);
//         
//         if (user.Id == tenantManager.GetCurrentTenant().OwnerId && user.Id != authContext.CurrentAccount.ID)
//         {
//             throw new SecurityException();
//         }
//         if (_userManager.IsSystemUser(user.Id))
//         {
//             throw new SecurityException();
//         }
//
//         user.ContactsList.Clear();
//         await UpdateContactsAsync(inDto.Contacts.Contacts, user);
//         await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
//
//         return await employeeFullDtoHelper.GetFullAsync(user);
//     }
//
//     /// <remarks>
//     /// Updates the contact information of the user with the ID specified in the request merging the new data into the current portal data.
//     /// </remarks>
//     /// <summary>
//     /// Update user contacts
//     /// </summary>
//     /// <path>api/2.0/people/{userid}/contacts</path>
//     [Tags("People / Contacts")]
//     [SwaggerResponse(200, "Updated user profile with the detailed information", typeof(EmployeeFullDto))]
//     [SwaggerResponse(403, "No permissions to perform this action")]
//     [SwaggerResponse(404, "User not found")]
//     [HttpPut("{userid}/contacts")]
//     public async Task<EmployeeFullDto> UpdateMemberContacts(ContactsRequestDto inDto)
//     {
//         var user = await GetUserInfoAsync(inDto.UserId);
//         
//         if (user.Id == tenantManager.GetCurrentTenant().OwnerId && user.Id != authContext.CurrentAccount.ID)
//         {
//             throw new SecurityException();
//         }
//         
//         if (_userManager.IsSystemUser(user.Id))
//         {
//             throw new SecurityException();
//         }
//
//         await UpdateContactsAsync(inDto.Contacts.Contacts, user);
//         await _userManager.UpdateUserInfoWithSyncCardDavAsync(user);
//
//         return await employeeFullDtoHelper.GetFullAsync(user);
//     }
//
//     private async Task DeleteContactsAsync(IEnumerable<Contact> contacts, UserInfo user)
//     {
//         await _permissionContext.DemandPermissionsAsync(new UserSecurityProvider(user.Id), Constants.Action_EditUser);
//
//         if (contacts == null)
//         {
//             return;
//         }
//
//         user.ContactsList ??= [];
//
//         foreach (var contact in contacts)
//         {
//             var index = user.ContactsList.IndexOf(contact.Type);
//             if (index != -1)
//             {
//                 //Remove existing
//                 user.ContactsList.RemoveRange(index, 2);
//             }
//         }
//     }
// }