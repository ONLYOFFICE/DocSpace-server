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

import java.sql.DatabaseMetaData;
import javax.sql.DataSource;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.support.JdbcUtils;
import org.springframework.jdbc.support.MetaDataAccessException;

/** Exposes the {@link DatabaseDialect} of the configured data source. */
@Slf4j
@Configuration
public class DatabaseDialectConfiguration {

  /**
   * Asks the data source which database it connects to, the same answer Hibernate takes its dialect
   * from, whatever configuration key the URL came from.
   *
   * @param dataSource the data source of the service
   * @return the database the data source connects to
   * @throws MetaDataAccessException when the database cannot be reached at startup
   */
  @Bean
  public DatabaseDialect databaseDialect(DataSource dataSource) throws MetaDataAccessException {
    String[] connection =
        JdbcUtils.extractDatabaseMetaData(
            dataSource,
            (DatabaseMetaData metaData) ->
                new String[] {metaData.getDatabaseProductName(), metaData.getURL()});
    var dialect = DatabaseDialect.fromProductName(connection[0]);
    if (dialect == DatabaseDialect.POSTGRESQL
        && !DatabaseDialect.sendsStringsUntyped(connection[1]))
      log.warn(
          "The PostgreSQL URL does not set stringtype=unspecified (JDBC_PARAMETERS):"
              + " string comparisons with citext columns tell letter case apart and skip their"
              + " indexes, and strings bound to uuid columns fail");
    return dialect;
  }
}
