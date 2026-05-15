// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
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

package com.asc.registration.core.domain;

import com.asc.common.core.domain.entity.Audit;
import com.asc.registration.core.domain.entity.Scope;
import com.asc.registration.core.domain.event.ScopeCreatedEvent;
import com.asc.registration.core.domain.event.ScopeDeletedEvent;
import com.asc.registration.core.domain.event.ScopeUpdatedEvent;

/** Interface for Scope domain service. */
public interface ScopeDomainService {
  /**
   * Creates a new scope.
   *
   * @param audit the audit information related to the creation
   * @param scope the scope to be created
   * @return an event indicating the scope was created
   */
  ScopeCreatedEvent createScope(Audit audit, Scope scope);

  /**
   * Updates the group of an existing scope.
   *
   * @param audit the audit information related to the update
   * @param scope the scope to be updated
   * @param newGroup the new group for the scope
   * @return an event indicating the scope's group was updated
   */
  ScopeUpdatedEvent updateScopeGroup(Audit audit, Scope scope, String newGroup);

  /**
   * Updates the type of an existing scope.
   *
   * @param audit the audit information related to the update
   * @param scope the scope to be updated
   * @param newType the new type for the scope
   * @return an event indicating the scope's type was updated
   */
  ScopeUpdatedEvent updateScopeType(Audit audit, Scope scope, String newType);

  /**
   * Deletes an existing scope.
   *
   * @param audit the audit information related to the deletion
   * @param scope the scope to be deleted
   * @return an event indicating the scope was deleted
   */
  ScopeDeletedEvent deleteScope(Audit audit, Scope scope);
}
