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

/**
 * The database the identity tables live in, for the few statements whose SQL differs between MySQL
 * and PostgreSQL. Everything else goes through Hibernate, whose dialect covers it.
 */
public enum DatabaseDialect {
  MYSQL,
  POSTGRESQL;

  /**
   * Reads the database from the product name its JDBC driver reports ({@link
   * java.sql.DatabaseMetaData#getDatabaseProductName()}): {@code PostgreSQL} is PostgreSQL,
   * anything else MySQL, the database the service ran on before PostgreSQL was supported.
   *
   * @param productName the product name, may be {@code null}
   * @return the database of the product name
   */
  public static DatabaseDialect fromProductName(String productName) {
    return "PostgreSQL".equalsIgnoreCase(productName) ? POSTGRESQL : MYSQL;
  }

  /**
   * Whether a PostgreSQL JDBC URL sends string parameters untyped ({@code stringtype=unspecified}),
   * so that the server types them by their columns: sent as {@code varchar}, a comparison with a
   * {@code citext} column becomes {@code text = text}, which tells letter case apart and passes the
   * column's index by, and a string bound to a {@code uuid} column fails.
   *
   * @param jdbcUrl the URL of the connection, may be {@code null}
   * @return {@code true} when the URL carries {@code stringtype=unspecified}
   */
  public static boolean sendsStringsUntyped(String jdbcUrl) {
    if (jdbcUrl == null) return false;
    var query = jdbcUrl.indexOf('?');
    if (query < 0) return false;
    for (var parameter : jdbcUrl.substring(query + 1).split("&")) {
      if (parameter.equalsIgnoreCase("stringtype=unspecified")) return true;
    }
    return false;
  }

  /**
   * A {@code DELETE} of at most {@code :limit} rows that match a condition. MySQL takes the {@code
   * LIMIT} in the statement itself. PostgreSQL has no {@code DELETE ... LIMIT}, so it deletes the
   * rows whose keys a limited {@code SELECT} returns, and checks the condition again on the row it
   * deletes: a row that another transaction updates in the meantime keeps its key, so it is still
   * deleted when it still matches, as on MySQL (a physical row id such as {@code ctid} would change
   * with the update, and the row would be skipped).
   *
   * @param table the table to delete from
   * @param keyColumns the columns of the table's primary key, comma-separated
   * @param condition the {@code WHERE} condition, without the keyword
   * @return the statement, with a {@code :limit} parameter
   */
  public String limitedDelete(String table, String keyColumns, String condition) {
    return switch (this) {
      case MYSQL -> "DELETE FROM " + table + " WHERE " + condition + " LIMIT :limit";
      case POSTGRESQL ->
          "DELETE FROM "
              + table
              + " WHERE "
              + condition
              + " AND ("
              + keyColumns
              + ") IN (SELECT "
              + keyColumns
              + " FROM "
              + table
              + " WHERE "
              + condition
              + " LIMIT :limit)";
    };
  }
}
