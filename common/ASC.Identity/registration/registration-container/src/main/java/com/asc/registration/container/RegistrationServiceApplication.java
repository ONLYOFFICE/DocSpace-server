// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.container;

import io.swagger.v3.oas.annotations.OpenAPIDefinition;
import io.swagger.v3.oas.annotations.enums.SecuritySchemeIn;
import io.swagger.v3.oas.annotations.enums.SecuritySchemeType;
import io.swagger.v3.oas.annotations.info.Contact;
import io.swagger.v3.oas.annotations.info.Info;
import io.swagger.v3.oas.annotations.info.License;
import io.swagger.v3.oas.annotations.security.SecurityScheme;
import io.swagger.v3.oas.annotations.security.SecuritySchemes;
import io.swagger.v3.oas.annotations.servers.Server;
import net.devh.boot.grpc.server.autoconfigure.GrpcServerSecurityAutoConfiguration;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.domain.EntityScan;
import org.springframework.cache.annotation.EnableCaching;
import org.springframework.data.jpa.repository.config.EnableJpaRepositories;
import org.springframework.retry.annotation.EnableRetry;
import org.springframework.transaction.annotation.EnableTransactionManagement;

/**
 * Entry point for the ASC Identity Registration Service application.
 *
 * <p>This application provides APIs for managing registered clients in the ASC Identity ecosystem.
 * It includes features like caching, retry, transaction management, and OpenAPI documentation.
 */
@EnableRetry
@EnableCaching
@EnableTransactionManagement
@EntityScan(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
@EnableJpaRepositories(basePackages = {"com.asc.registration.data", "com.asc.common.data"})
@SpringBootApplication(
    scanBasePackages = {"com.asc.registration", "com.asc.common"},
    exclude = {GrpcServerSecurityAutoConfiguration.class})
@OpenAPIDefinition(
    info =
        @Info(
            title = "Registered Clients API",
            version = "1.0.0",
            description = "ASC.Identity Clients Registration API",
            contact =
                @Contact(
                    name = "ONLYOFFICE Support",
                    email = "support@onlyoffice.com",
                    url = "https://onlyoffice.com"),
            license = @License(name = "Apache 2.0")),
    servers = {@Server(description = "ASC.Identity Clients Registration API")})
@SecuritySchemes({
  @SecurityScheme(
      name = "ascAuthAdmin",
      paramName = "X-Signature",
      description = "ASC JWT Signature for Admin Authorization",
      type = SecuritySchemeType.APIKEY,
      in = SecuritySchemeIn.HEADER),
  @SecurityScheme(
      name = "ascAuthUser",
      paramName = "X-Signature",
      description = "ASC JWT Signature for User Authorization",
      type = SecuritySchemeType.APIKEY,
      in = SecuritySchemeIn.HEADER)
})
public class RegistrationServiceApplication {

  /**
   * The main method to start the Registration Service application.
   *
   * @param args command-line arguments passed to the application.
   */
  public static void main(String[] args) {
    SpringApplication.run(RegistrationServiceApplication.class, args);
  }
}
