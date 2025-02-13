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
package com.asc.transfer.configuration;

import javax.sql.DataSource;
import lombok.RequiredArgsConstructor;
import org.springframework.boot.jdbc.DataSourceBuilder;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

/**
 * Primary data source configuration class.
 *
 * <p>This configuration class defines the primary {@link DataSource} bean using properties
 * specified under {@code spring.datasource} in the application's configuration files. It utilizes
 * the {@link DataSourceConfigurationProperties} to retrieve the necessary data source settings such
 * as driver class name, URL, username, and password.
 */
@Configuration
@RequiredArgsConstructor
public class PrimaryDataSourceConfiguration {
  /** The configuration properties for the data source. */
  private final DataSourceConfigurationProperties configurationProperties;

  /**
   * Creates the primary {@link DataSource} bean.
   *
   * <p>This bean is marked as primary with the {@code @Primary} annotation, ensuring that it is
   * preferred when multiple data source beans are present in the application context. The method
   * builds the data source using the driver class name, URL, username, and password obtained from
   * {@link DataSourceConfigurationProperties}.
   *
   * @return the configured primary {@link DataSource}
   */
  @Bean
  @Primary
  public DataSource dataSource() {
    return DataSourceBuilder.create()
        .driverClassName(configurationProperties.getDriverClassName())
        .url(configurationProperties.getUrl())
        .username(configurationProperties.getUsername())
        .password(configurationProperties.getPassword())
        .build();
  }
}
