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

package com.asc.authorization.data.consent.repository;

import java.time.ZonedDateTime;

/**
 * The chunked removals of consents whose SQL differs between MySQL and PostgreSQL ({@link
 * com.asc.common.data.dialect.DatabaseDialect#limitedDelete}), implemented in {@link
 * ConsentLimitedDeletesImpl} rather than declared with {@code @Query}. Each deletes up to {@code
 * limit} consents; bulk removals call it repeatedly until it deletes nothing, each call in its own
 * short transaction. Every deleted consent also cascades to its rows in {@code
 * identity_consent_scopes}, so the limit bounds those as well.
 */
public interface ConsentLimitedDeletes {
  /**
   * Deletes up to {@code limit} consents associated with a specific client.
   *
   * @param registeredClientId The unique identifier of the registered client for which the consents
   *     are to be deleted.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  int deleteConsentsByClientId(String registeredClientId, int limit);

  /**
   * Deletes up to {@code limit} consents associated with a specific principal (user).
   *
   * @param principalId The unique identifier of the principal (user) for which the consents are to
   *     be deleted.
   * @param cutoff Only consents given up to this moment are deleted.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  int deleteConsentsByPrincipalId(String principalId, ZonedDateTime cutoff, int limit);

  /**
   * Deletes up to {@code limit} consents given to a client owned by a specific tenant, whichever
   * tenant the consenting user belonged to.
   *
   * @param ownerTenantId The ID of the tenant owning the clients.
   * @param cutoff Only consents given up to this moment are deleted.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  int deleteConsentsByOwnerTenantId(long ownerTenantId, ZonedDateTime cutoff, int limit);

  /**
   * Deletes up to {@code limit} consents given to a client created by a specific user, whichever
   * tenant the consenting user belonged to.
   *
   * @param ownerTenantId The ID of the tenant owning the clients.
   * @param ownerUserId The ID of the user who created the clients.
   * @param cutoff Only consents given up to this moment are deleted.
   * @param limit The maximum number of consents to delete.
   * @return The number of consents deleted.
   */
  int deleteConsentsByOwner(
      long ownerTenantId, String ownerUserId, ZonedDateTime cutoff, int limit);
}
