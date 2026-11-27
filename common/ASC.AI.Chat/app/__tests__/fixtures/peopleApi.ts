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

/**
 * `EmployeeFullDto` of a User as `GET api/2.0/people/@self` answers it
 * (common/ASC.Api.Core/Model/EmployeeFullDto.cs with its base EmployeeDto.cs,
 * PeopleController.SelfAsync), in the envelope: every property the two classes declare.
 */
export function selfDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    response: {
      firstName: "Jane",
      lastName: "Doe",
      userName: "jane",
      email: "jane@portal.example",
      contacts: [],
      birthday: null,
      sex: null,
      status: 1,
      activationStatus: 1,
      terminated: null,
      department: "",
      workFrom: null,
      groups: [],
      location: null,
      notes: null,
      isAdmin: false,
      isRoomAdmin: false,
      isLDAP: false,
      listAdminModules: [],
      isOwner: false,
      isVisitor: false,
      isCollaborator: true,
      cultureName: "en-US",
      mobilePhone: null,
      mobilePhoneActivationStatus: 0,
      isSSO: false,
      theme: null,
      quotaLimit: -1,
      usedSpace: 0,
      shared: null,
      isCustomQuota: false,
      aiServicesLimit: null,
      loginEventId: 15,
      authCookieLifetime: null,
      createdBy: null,
      registrationDate: "2026-11-01T10:00:00.0000000+00:00",
      hasPersonalFolder: true,
      tfaAppEnabled: false,
      id: "a0d6bc40-2b3c-4c1a-9a7d-1f1b2c3d4e5f",
      displayName: "Jane Doe",
      title: null,
      avatar: "/static/images/default_user_photo_size_82-82.png",
      avatarOriginal: "/static/images/default_user_photo_size_200-200.png",
      avatarMax: "/static/images/default_user_photo_size_200-200.png",
      avatarMedium: "/static/images/default_user_photo_size_48-48.png",
      avatarSmall: "/static/images/default_user_photo_size_32-32.png",
      profileUrl: "http://portal.example/accounts/view/jane",
      hasAvatar: false,
      isAnonim: false,
      ...overrides,
    },
    count: 1,
    status: 0,
    statusCode: 200,
  };
}
