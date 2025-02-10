// (c) Copyright Ascensio System SIA 2009-2025
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
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

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
