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

package com.asc.authorization.application.configuration.cryptography;

import javax.sql.DataSource;
import net.javacrumbs.shedlock.core.LockProvider;
import net.javacrumbs.shedlock.provider.jdbctemplate.JdbcTemplateLockProvider;
import net.javacrumbs.shedlock.spring.annotation.EnableSchedulerLock;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.JdbcTemplate;

/**
 * Configuration class for setting up ShedLock to manage distributed locks for scheduled tasks.
 *
 * <p>This configuration enables scheduler locking to ensure that scheduled tasks, such as key
 * rotation, do not run concurrently in distributed environments. The default lock durations are set
 * to: - Minimum lock duration: 10 seconds - Maximum lock duration: 20 seconds
 */
@Configuration
@EnableSchedulerLock(defaultLockAtLeastFor = "PT10s", defaultLockAtMostFor = "PT20S")
public class KeyRotationSchedulerConfiguration {

  /**
   * Creates a {@link LockProvider} bean to manage distributed locks using ShedLock with JDBC.
   *
   * <p>The lock provider uses a {@link JdbcTemplate} to interact with the database and stores
   * locking information in a specified table. It also ensures that the database server's time is
   * used for lock expiration, avoiding issues with server clock differences.
   *
   * @param dataSource the {@link DataSource} used for database connectivity.
   * @return the configured {@link LockProvider} instance.
   */
  @Bean
  public LockProvider lockProvider(DataSource dataSource) {
    return new JdbcTemplateLockProvider(
        JdbcTemplateLockProvider.Configuration.builder()
            .withJdbcTemplate(new JdbcTemplate(dataSource))
            .withTableName("identity_shedlock")
            .usingDbTime()
            .build());
  }
}
