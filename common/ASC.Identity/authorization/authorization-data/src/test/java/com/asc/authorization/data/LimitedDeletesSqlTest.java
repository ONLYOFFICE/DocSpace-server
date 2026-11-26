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

package com.asc.authorization.data;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import com.asc.authorization.data.authorization.repository.AuthorizationLimitedDeletesImpl;
import com.asc.authorization.data.consent.repository.ConsentLimitedDeletesImpl;
import com.asc.common.data.dialect.DatabaseDialect;
import jakarta.persistence.EntityManager;
import jakarta.persistence.Query;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;
import org.springframework.test.util.ReflectionTestUtils;

/**
 * Pins the SQL of the chunked removals: on MySQL the statements the repositories declared with
 * {@code @Query} before PostgreSQL was supported, on PostgreSQL the same conditions, checked again
 * on the row a limited {@code SELECT} of the primary key returns.
 */
class LimitedDeletesSqlTest {
  private static final String GRANTED_BY_CUTOFF =
      " AND (COALESCE(authorization_code_issued_at, modified_at) IS NULL"
          + " OR COALESCE(authorization_code_issued_at, modified_at) <= :cutoff)";
  private static final String GIVEN_BY_CUTOFF =
      " AND (modified_at IS NULL OR modified_at <= :cutoff)";
  private static final ZonedDateTime CUTOFF =
      ZonedDateTime.of(2026, 11, 26, 0, 0, 0, 0, ZoneOffset.UTC);

  private final List<String> statements = new ArrayList<>();

  private EntityManager recordingEntityManager() {
    var query = mock(Query.class);
    when(query.setParameter(anyString(), any())).thenReturn(query);
    when(query.executeUpdate()).thenReturn(0);
    var entityManager = mock(EntityManager.class);
    when(entityManager.createNativeQuery(anyString()))
        .thenAnswer(
            invocation -> {
              statements.add(invocation.getArgument(0));
              return query;
            });
    return entityManager;
  }

  private List<String> authorizationStatements(DatabaseDialect dialect) {
    var deletes = new AuthorizationLimitedDeletesImpl(dialect);
    ReflectionTestUtils.setField(deletes, "entityManager", recordingEntityManager());
    deletes.deleteAuthorizationsByClientId("client", 1000);
    deletes.deleteAuthorizationsByPrincipalId("user", CUTOFF, 1000);
    deletes.deleteAuthorizationsByTenantId(1, CUTOFF, 1000);
    deletes.deleteAuthorizationsByOwnerTenantId(1, CUTOFF, 1000);
    deletes.deleteAuthorizationsByOwner(1, "user", CUTOFF, 1000);
    return List.copyOf(statements);
  }

  private List<String> consentStatements(DatabaseDialect dialect) {
    var deletes = new ConsentLimitedDeletesImpl(dialect);
    ReflectionTestUtils.setField(deletes, "entityManager", recordingEntityManager());
    deletes.deleteConsentsByClientId("client", 500);
    deletes.deleteConsentsByPrincipalId("user", CUTOFF, 500);
    deletes.deleteConsentsByOwnerTenantId(1, CUTOFF, 500);
    deletes.deleteConsentsByOwner(1, "user", CUTOFF, 500);
    return List.copyOf(statements);
  }

  @Test
  void authorizations_MySql_AreTheStatementsOfBefore() {
    assertThat(authorizationStatements(DatabaseDialect.MYSQL))
        .containsExactly(
            "DELETE FROM identity_authorizations WHERE registered_client_id = :clientId LIMIT :limit",
            "DELETE FROM identity_authorizations WHERE principal_id = :principalId"
                + GRANTED_BY_CUTOFF
                + " LIMIT :limit",
            "DELETE FROM identity_authorizations WHERE tenant_id = :tenantId"
                + GRANTED_BY_CUTOFF
                + " LIMIT :limit",
            "DELETE FROM identity_authorizations WHERE owner_tenant_id = :ownerTenantId"
                + GRANTED_BY_CUTOFF
                + " LIMIT :limit",
            "DELETE FROM identity_authorizations"
                + " WHERE owner_tenant_id = :ownerTenantId AND owner_user_id = :ownerUserId"
                + GRANTED_BY_CUTOFF
                + " LIMIT :limit");
  }

  @Test
  void consents_MySql_AreTheStatementsOfBefore() {
    assertThat(consentStatements(DatabaseDialect.MYSQL))
        .containsExactly(
            "DELETE FROM identity_consents WHERE registered_client_id = :registeredClientId LIMIT :limit",
            "DELETE FROM identity_consents WHERE principal_id = :principalId"
                + GIVEN_BY_CUTOFF
                + " LIMIT :limit",
            "DELETE FROM identity_consents WHERE owner_tenant_id = :ownerTenantId"
                + GIVEN_BY_CUTOFF
                + " LIMIT :limit",
            "DELETE FROM identity_consents"
                + " WHERE owner_tenant_id = :ownerTenantId AND owner_user_id = :ownerUserId"
                + GIVEN_BY_CUTOFF
                + " LIMIT :limit");
  }

  @Test
  void authorizations_PostgreSql_DeleteByTheKeyAndCheckTheConditionAgain() {
    assertThat(authorizationStatements(DatabaseDialect.POSTGRESQL))
        .containsExactly(
            postgreSql(
                "identity_authorizations",
                "principal_id, registered_client_id, authorization_grant_type",
                "registered_client_id = :clientId"),
            postgreSql(
                "identity_authorizations",
                "principal_id, registered_client_id, authorization_grant_type",
                "principal_id = :principalId" + GRANTED_BY_CUTOFF),
            postgreSql(
                "identity_authorizations",
                "principal_id, registered_client_id, authorization_grant_type",
                "tenant_id = :tenantId" + GRANTED_BY_CUTOFF),
            postgreSql(
                "identity_authorizations",
                "principal_id, registered_client_id, authorization_grant_type",
                "owner_tenant_id = :ownerTenantId" + GRANTED_BY_CUTOFF),
            postgreSql(
                "identity_authorizations",
                "principal_id, registered_client_id, authorization_grant_type",
                "owner_tenant_id = :ownerTenantId AND owner_user_id = :ownerUserId"
                    + GRANTED_BY_CUTOFF));
  }

  @Test
  void consents_PostgreSql_DeleteByTheKeyAndCheckTheConditionAgain() {
    assertThat(consentStatements(DatabaseDialect.POSTGRESQL))
        .containsExactly(
            postgreSql(
                "identity_consents",
                "principal_id, registered_client_id",
                "registered_client_id = :registeredClientId"),
            postgreSql(
                "identity_consents",
                "principal_id, registered_client_id",
                "principal_id = :principalId" + GIVEN_BY_CUTOFF),
            postgreSql(
                "identity_consents",
                "principal_id, registered_client_id",
                "owner_tenant_id = :ownerTenantId" + GIVEN_BY_CUTOFF),
            postgreSql(
                "identity_consents",
                "principal_id, registered_client_id",
                "owner_tenant_id = :ownerTenantId AND owner_user_id = :ownerUserId"
                    + GIVEN_BY_CUTOFF));
  }

  /** The expected PostgreSQL statement, spelled out here rather than built by the dialect. */
  private static String postgreSql(String table, String key, String condition) {
    return "DELETE FROM "
        + table
        + " WHERE "
        + condition
        + " AND ("
        + key
        + ") IN (SELECT "
        + key
        + " FROM "
        + table
        + " WHERE "
        + condition
        + " LIMIT :limit)";
  }
}
