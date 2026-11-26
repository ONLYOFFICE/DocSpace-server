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

import static com.asc.authorization.data.consent.repository.JpaConsentRepository.GIVEN_BY_CUTOFF;

import com.asc.common.data.dialect.DatabaseDialect;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import java.time.ZonedDateTime;

/** {@link ConsentLimitedDeletes} with the SQL of the configured database. */
public class ConsentLimitedDeletesImpl implements ConsentLimitedDeletes {
  private static final String TABLE = "identity_consents";
  private static final String KEY = "principal_id, registered_client_id";

  private final String byClientId;
  private final String byPrincipalId;
  private final String byOwnerTenantId;
  private final String byOwner;

  @PersistenceContext private EntityManager entityManager;

  /**
   * @param dialect the database the statements are written for
   */
  public ConsentLimitedDeletesImpl(DatabaseDialect dialect) {
    byClientId = dialect.limitedDelete(TABLE, KEY, "registered_client_id = :registeredClientId");
    byPrincipalId =
        dialect.limitedDelete(TABLE, KEY, "principal_id = :principalId" + GIVEN_BY_CUTOFF);
    byOwnerTenantId =
        dialect.limitedDelete(TABLE, KEY, "owner_tenant_id = :ownerTenantId" + GIVEN_BY_CUTOFF);
    byOwner =
        dialect.limitedDelete(
            TABLE,
            KEY,
            "owner_tenant_id = :ownerTenantId AND owner_user_id = :ownerUserId" + GIVEN_BY_CUTOFF);
  }

  @Override
  public int deleteConsentsByClientId(String registeredClientId, int limit) {
    return entityManager
        .createNativeQuery(byClientId)
        .setParameter("registeredClientId", registeredClientId)
        .setParameter("limit", limit)
        .executeUpdate();
  }

  @Override
  public int deleteConsentsByPrincipalId(String principalId, ZonedDateTime cutoff, int limit) {
    return entityManager
        .createNativeQuery(byPrincipalId)
        .setParameter("principalId", principalId)
        .setParameter("cutoff", cutoff)
        .setParameter("limit", limit)
        .executeUpdate();
  }

  @Override
  public int deleteConsentsByOwnerTenantId(long ownerTenantId, ZonedDateTime cutoff, int limit) {
    return entityManager
        .createNativeQuery(byOwnerTenantId)
        .setParameter("ownerTenantId", ownerTenantId)
        .setParameter("cutoff", cutoff)
        .setParameter("limit", limit)
        .executeUpdate();
  }

  @Override
  public int deleteConsentsByOwner(
      long ownerTenantId, String ownerUserId, ZonedDateTime cutoff, int limit) {
    return entityManager
        .createNativeQuery(byOwner)
        .setParameter("ownerTenantId", ownerTenantId)
        .setParameter("ownerUserId", ownerUserId)
        .setParameter("cutoff", cutoff)
        .setParameter("limit", limit)
        .executeUpdate();
  }
}
