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

package com.asc.authorization.container;

import net.devh.boot.grpc.client.autoconfigure.GrpcClientMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerMetricAutoConfiguration;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerSecurityAutoConfiguration;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.persistence.autoconfigure.EntityScan;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.context.annotation.ImportRuntimeHints;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.scheduling.annotation.EnableScheduling;
import org.springframework.transaction.annotation.EnableTransactionManagement;

/**
 * Main class for the Authorization Service application.
 *
 * <p>This class is responsible for bootstrapping the Spring Boot application. It configures entity
 * scanning, JPA repositories, Feign clients, and the base packages to be scanned.
 */
@EnableCaching
@EnableScheduling
@EnableTransactionManagement
@ImportRuntimeHints(AuthorizationServiceRuntimeHints.class)
@EntityScan(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
@EnableJpaRepositories(basePackages = {"com.asc.authorization.data", "com.asc.common.data"})
@SpringBootApplication(
    scanBasePackages = {"com.asc.authorization", "com.asc.common"},
    exclude = {
      GrpcServerSecurityAutoConfiguration.class,
      GrpcServerMetricAutoConfiguration.class,
      GrpcClientMetricAutoConfiguration.class
    })
public class AuthorizationServiceApplication {

  /**
   * The main method serves as the entry point for the Spring Boot application.
   *
   * @param args command line arguments passed to the application
   */
  public static void main(String[] args) {
    // TODO: Upgrade the dependency and remove the property
    System.setProperty("io.grpc.netty.shaded.io.netty.noUnsafe", "true");
    SpringApplication.run(AuthorizationServiceApplication.class, args);
  }
}
