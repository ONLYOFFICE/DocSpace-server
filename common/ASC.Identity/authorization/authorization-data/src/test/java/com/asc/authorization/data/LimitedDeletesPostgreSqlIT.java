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

import com.asc.authorization.data.authorization.repository.AuthorizationLimitedDeletesImpl;
import com.asc.authorization.data.consent.repository.ConsentLimitedDeletesImpl;
import com.asc.common.data.dialect.DatabaseDialect;
import jakarta.persistence.EntityManager;
import jakarta.persistence.Query;
import java.sql.Connection;
import java.sql.DriverManager;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.util.HashMap;
import java.util.Map;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;
import org.junit.jupiter.api.AfterAll;
import org.junit.jupiter.api.BeforeAll;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.Mockito;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.jdbc.datasource.SingleConnectionDataSource;
import org.springframework.test.util.ReflectionTestUtils;
import org.testcontainers.junit.jupiter.Testcontainers;
import org.testcontainers.postgresql.PostgreSQLContainer;

/**
 * Runs the PostgreSQL statements of the chunked removals on a real PostgreSQL, with the column
 * types the EF migrations give the tables there ({@code citext}, {@code timestamptz}) and the
 * {@code stringtype=unspecified} connection the services use. The statements are taken from the
 * fragments themselves, so a change of the dialect is what these tests see; they are bound here by
 * Spring's {@code NamedParameterJdbcTemplate}, not by Hibernate as in the services, so Hibernate's
 * binding of the repeated parameters is not what these tests check.
 */
@Testcontainers(disabledWithoutDocker = true)
class LimitedDeletesPostgreSqlIT {
  private static final ZonedDateTime CUTOFF =
      ZonedDateTime.of(2030, 1, 1, 0, 0, 0, 0, ZoneOffset.UTC);
  private static final OffsetDateTime GRANTED =
      OffsetDateTime.of(2026, 1, 1, 0, 0, 0, 0, ZoneOffset.UTC);

  private static PostgreSQLContainer postgres;

  @BeforeAll
  static void start() throws Exception {
    postgres = new PostgreSQLContainer("postgres:17");
    postgres.start();
    try (var connection = connect()) {
      connection
          .createStatement()
          .execute(
              """
              CREATE EXTENSION IF NOT EXISTS citext;
              CREATE TABLE identity_consents (
                principal_id citext NOT NULL,
                registered_client_id citext NOT NULL,
                modified_at timestamptz NULL,
                owner_tenant_id bigint NULL,
                owner_user_id citext NULL,
                PRIMARY KEY (principal_id, registered_client_id));
              CREATE TABLE identity_authorizations (
                principal_id citext NOT NULL,
                registered_client_id citext NOT NULL,
                authorization_grant_type citext NOT NULL,
                tenant_id integer NOT NULL,
                owner_tenant_id bigint NULL,
                owner_user_id citext NULL,
                authorization_code_issued_at timestamptz NULL,
                modified_at timestamptz NULL,
                refresh_token_value citext NULL,
                PRIMARY KEY (principal_id, registered_client_id, authorization_grant_type));
              """);
    }
  }

  @AfterAll
  static void stop() {
    if (postgres != null) postgres.stop();
  }

  private static Connection connect() throws Exception {
    return DriverManager.getConnection(
        postgres.getJdbcUrl() + "&stringtype=unspecified",
        postgres.getUsername(),
        postgres.getPassword());
  }

  @BeforeEach
  void clean() throws Exception {
    try (var connection = connect()) {
      connection
          .createStatement()
          .execute("TRUNCATE identity_consents; TRUNCATE identity_authorizations;");
    }
  }

  /** The SQL a fragment sends, with its parameters, recorded from its calls. */
  private record Recorded(String sql, Map<String, Object> parameters) {}

  private static Recorded record(java.util.function.Consumer<EntityManager> call) {
    var parameters = new HashMap<String, Object>();
    var sql = new String[1];
    var query = Mockito.mock(Query.class);
    Mockito.when(query.setParameter(Mockito.anyString(), Mockito.any()))
        .thenAnswer(
            invocation -> {
              Object value = invocation.getArgument(1);
              parameters.put(
                  invocation.getArgument(0),
                  value instanceof ZonedDateTime time ? time.toOffsetDateTime() : value);
              return query;
            });
    var entityManager = Mockito.mock(EntityManager.class);
    Mockito.when(entityManager.createNativeQuery(Mockito.anyString()))
        .thenAnswer(
            invocation -> {
              sql[0] = invocation.getArgument(0);
              return query;
            });
    call.accept(entityManager);
    return new Recorded(sql[0], parameters);
  }

  private static Recorded consentsOfPrincipal(String principalId, int limit) {
    return record(
        entityManager -> {
          var deletes = new ConsentLimitedDeletesImpl(DatabaseDialect.POSTGRESQL);
          ReflectionTestUtils.setField(deletes, "entityManager", entityManager);
          deletes.deleteConsentsByPrincipalId(principalId, CUTOFF, limit);
        });
  }

  private static Recorded authorizationsOfPrincipal(String principalId, int limit) {
    return record(
        entityManager -> {
          var deletes = new AuthorizationLimitedDeletesImpl(DatabaseDialect.POSTGRESQL);
          ReflectionTestUtils.setField(deletes, "entityManager", entityManager);
          deletes.deleteAuthorizationsByPrincipalId(principalId, CUTOFF, limit);
        });
  }

  private static int execute(Connection connection, Recorded statement) {
    return new NamedParameterJdbcTemplate(new SingleConnectionDataSource(connection, true))
        .update(statement.sql(), statement.parameters());
  }

  /** Polls until a session of the database waits for a row lock, for at most ten seconds. */
  private static boolean waitForALockWait() throws Exception {
    var deadline = System.nanoTime() + TimeUnit.SECONDS.toNanos(10);
    try (var observer = connect()) {
      while (true) {
        try (var result =
            observer
                .createStatement()
                .executeQuery(
                    "SELECT count(*) FROM pg_stat_activity WHERE wait_event_type = 'Lock'")) {
          result.next();
          if (result.getLong(1) > 0) return true;
        }
        if (System.nanoTime() > deadline) return false;
        Thread.sleep(50);
      }
    }
  }

  private static long count(String table) throws Exception {
    try (var connection = connect();
        var result = connection.createStatement().executeQuery("SELECT count(*) FROM " + table)) {
      result.next();
      return result.getLong(1);
    }
  }

  @Test
  void consents_DeletesAtMostTheLimitThenTheRest() throws Exception {
    try (var connection = connect()) {
      for (var i = 0; i < 5; i++)
        connection
            .createStatement()
            .execute(
                "INSERT INTO identity_consents (principal_id, registered_client_id, modified_at)"
                    + " VALUES ('User', 'client"
                    + i
                    + "', now())");
      connection
          .createStatement()
          .execute(
              "INSERT INTO identity_consents (principal_id, registered_client_id, modified_at)"
                  + " VALUES ('other', 'client0', now())");

      // The letter case of the principal does not matter, as with MySQL's _ci collation.
      assertThat(execute(connection, consentsOfPrincipal("user", 3))).isEqualTo(3);
      assertThat(execute(connection, consentsOfPrincipal("user", 3))).isEqualTo(2);
      assertThat(execute(connection, consentsOfPrincipal("user", 3))).isZero();
    }
    assertThat(count("identity_consents")).isEqualTo(1);
  }

  /**
   * A row updated by another transaction while the removal waits for its lock: PostgreSQL gives the
   * updated row a new physical id, so a removal by that id would skip it and leave it behind. By
   * the key it is deleted.
   */
  @Test
  void authorizations_RowUpdatedByAnotherTransaction_IsStillDeleted() throws Exception {
    try (var setup = connect()) {
      for (var i = 0; i < 3; i++)
        setup
            .createStatement()
            .execute(
                "INSERT INTO identity_authorizations (principal_id, registered_client_id,"
                    + " authorization_grant_type, tenant_id, authorization_code_issued_at,"
                    + " refresh_token_value) VALUES ('user', 'client"
                    + i
                    + "', 'authorization_code', 1, '"
                    + GRANTED
                    + "', 'token')");
    }

    try (var refresh = connect();
        var removal = connect()) {
      refresh.setAutoCommit(false);
      // A token refresh of client1 that has written its row but not committed yet.
      refresh
          .createStatement()
          .execute(
              "UPDATE identity_authorizations SET refresh_token_value = 'refreshed',"
                  + " modified_at = now() WHERE registered_client_id = 'client1'");

      var statement = authorizationsOfPrincipal("user", 10);
      var deleted = CompletableFuture.supplyAsync(() -> execute(removal, statement));

      // The removal has read the rows of its limited SELECT and waits for the lock on client1.
      assertThat(waitForALockWait()).isTrue();
      assertThat(deleted).isNotDone();
      refresh.commit();

      assertThat(deleted.get(30, TimeUnit.SECONDS)).isEqualTo(3);
    }
    assertThat(count("identity_authorizations")).isZero();
  }
}
