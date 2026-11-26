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

package com.asc.authorization.data.authorization.repository;

import java.time.ZonedDateTime;

/**
 * The chunked removals of authorizations. Each deletes up to {@code limit} rows; bulk removals call
 * it repeatedly until it deletes nothing, each call in its own short transaction, so that no single
 * transaction grows with the number of rows. Their SQL differs between MySQL and PostgreSQL ({@link
 * com.asc.common.data.dialect.DatabaseDialect#limitedDelete}), so they are implemented in {@link
 * AuthorizationLimitedDeletesImpl} rather than declared with {@code @Query}.
 */
public interface AuthorizationLimitedDeletes {
  /**
   * Deletes up to {@code limit} authorizations associated with a specific registered client.
   *
   * @param clientId the ID of the registered client whose authorizations are to be deleted.
   * @param limit the maximum number of rows to delete.
   * @return the number of rows deleted.
   */
  int deleteAuthorizationsByClientId(String clientId, int limit);

  /**
   * Deletes up to {@code limit} authorizations associated with a specific principal (user).
   *
   * @param principalId the ID of the principal (user) whose authorizations are to be deleted.
   * @param cutoff only authorizations granted up to this moment are deleted.
   * @param limit the maximum number of rows to delete.
   * @return the number of rows deleted.
   */
  int deleteAuthorizationsByPrincipalId(String principalId, ZonedDateTime cutoff, int limit);

  /**
   * Deletes up to {@code limit} authorizations associated with a specific tenant.
   *
   * @param tenantId the ID of the tenant whose authorizations are to be deleted.
   * @param cutoff only authorizations granted up to this moment are deleted.
   * @param limit the maximum number of rows to delete.
   * @return the number of rows deleted.
   */
  int deleteAuthorizationsByTenantId(long tenantId, ZonedDateTime cutoff, int limit);

  /**
   * Deletes up to {@code limit} authorizations granted to a client owned by a specific tenant,
   * whichever tenant the authorizing user belonged to.
   *
   * @param ownerTenantId the ID of the tenant owning the clients.
   * @param cutoff only authorizations granted up to this moment are deleted.
   * @param limit the maximum number of rows to delete.
   * @return the number of rows deleted.
   */
  int deleteAuthorizationsByOwnerTenantId(long ownerTenantId, ZonedDateTime cutoff, int limit);

  /**
   * Deletes up to {@code limit} authorizations granted to a client created by a specific user,
   * whichever tenant the authorizing user belonged to.
   *
   * @param ownerTenantId the ID of the tenant owning the clients.
   * @param ownerUserId the ID of the user who created the clients.
   * @param cutoff only authorizations granted up to this moment are deleted.
   * @param limit the maximum number of rows to delete.
   * @return the number of rows deleted.
   */
  int deleteAuthorizationsByOwner(
      long ownerTenantId, String ownerUserId, ZonedDateTime cutoff, int limit);
}
