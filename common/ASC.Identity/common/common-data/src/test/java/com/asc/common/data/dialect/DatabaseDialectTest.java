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

package com.asc.common.data.dialect;

import static org.assertj.core.api.Assertions.assertThat;

import org.junit.jupiter.api.Test;

class DatabaseDialectTest {

  @Test
  void fromProductName_PostgreSql_IsPostgreSql() {
    assertThat(DatabaseDialect.fromProductName("PostgreSQL")).isEqualTo(DatabaseDialect.POSTGRESQL);
    assertThat(DatabaseDialect.fromProductName("postgresql")).isEqualTo(DatabaseDialect.POSTGRESQL);
  }

  @Test
  void fromProductName_AnythingElse_IsMySql() {
    assertThat(DatabaseDialect.fromProductName("MySQL")).isEqualTo(DatabaseDialect.MYSQL);
    assertThat(DatabaseDialect.fromProductName("")).isEqualTo(DatabaseDialect.MYSQL);
    assertThat(DatabaseDialect.fromProductName(null)).isEqualTo(DatabaseDialect.MYSQL);
  }

  @Test
  void sendsStringsUntyped_ReadsTheQueryOfTheUrl() {
    assertThat(
            DatabaseDialect.sendsStringsUntyped(
                "jdbc:postgresql://host:5432/docspace?stringtype=unspecified"))
        .isTrue();
    assertThat(
            DatabaseDialect.sendsStringsUntyped(
                "jdbc:postgresql://host/docspace?ssl=false&StringType=Unspecified"))
        .isTrue();
    assertThat(DatabaseDialect.sendsStringsUntyped("jdbc:postgresql://host/docspace?")).isFalse();
    assertThat(DatabaseDialect.sendsStringsUntyped("jdbc:postgresql://host/docspace")).isFalse();
    assertThat(
            DatabaseDialect.sendsStringsUntyped(
                "jdbc:postgresql://host/docspace?stringtype=varchar"))
        .isFalse();
    assertThat(DatabaseDialect.sendsStringsUntyped(null)).isFalse();
  }

  @Test
  void limitedDelete_MySql_KeepsTheLimitInTheStatement() {
    assertThat(
            DatabaseDialect.MYSQL.limitedDelete(
                "identity_consents", "principal_id, registered_client_id", "principal_id = :p"))
        .isEqualTo("DELETE FROM identity_consents WHERE principal_id = :p LIMIT :limit");
  }

  @Test
  void limitedDelete_PostgreSql_DeletesByTheKeysOfALimitedSelect() {
    assertThat(
            DatabaseDialect.POSTGRESQL.limitedDelete(
                "identity_consents", "principal_id, registered_client_id", "principal_id = :p"))
        .isEqualTo(
            "DELETE FROM identity_consents WHERE principal_id = :p"
                + " AND (principal_id, registered_client_id) IN (SELECT principal_id,"
                + " registered_client_id FROM identity_consents WHERE principal_id = :p"
                + " LIMIT :limit)");
  }
}
