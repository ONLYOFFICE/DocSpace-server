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

import com.asc.transfer.entity.ClientEntity;
import com.asc.transfer.mapper.ClientRowMapper;
import java.util.Objects;
import javax.sql.DataSource;
import lombok.RequiredArgsConstructor;
import org.springframework.batch.item.database.JdbcPagingItemReader;
import org.springframework.batch.item.database.support.SqlPagingQueryProviderFactoryBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

/**
 * Configuration class for setting up the batch reader and related JDBC templates.
 *
 * <p>This class defines beans required for reading {@code ClientEntity} objects using a paging item
 * reader, as well as a JDBC template for executing SQL queries with named parameters.
 */
@Configuration
@RequiredArgsConstructor
public class BatchReaderConfiguration {

  /** The batch processing configuration that holds properties such as page size. */
  private final BatchProcessingConfiguration batchProcessingConfiguration;

  /**
   * Creates and configures a {@link SqlPagingQueryProviderFactoryBean} to generate SQL queries for
   * paging through {@code ClientEntity} records.
   *
   * <p>The query provider is configured with a SELECT clause to retrieve client details, a FROM
   * clause specifying the {@code identity_clients} table, and a sort key based on {@code
   * client_id}. It is also set to use MySQL as the database type.
   *
   * @param dataSource the data source used to set up the query provider
   * @return a configured {@link SqlPagingQueryProviderFactoryBean} instance
   */
  private SqlPagingQueryProviderFactoryBean getProviderFactoryBean(DataSource dataSource) {
    var queryProvider = new SqlPagingQueryProviderFactoryBean();
    queryProvider.setDataSource(dataSource);
    queryProvider.setSelectClause(
        "SELECT client_id, tenant_id, client_secret, name, description, logo, website_url, terms_url, policy_url, logout_redirect_uri, is_public, is_enabled, created_on, created_by, modified_on, modified_by, version");
    queryProvider.setFromClause("FROM identity_clients");
    queryProvider.setSortKey("client_id");
    queryProvider.setDatabaseType("mysql");
    return queryProvider;
  }

  /**
   * Creates a {@link JdbcPagingItemReader} bean to read {@code ClientEntity} records from the
   * database.
   *
   * <p>The reader is configured with the provided data source, a page size from the batch
   * processing configuration, and a {@link ClientRowMapper} to map rows to {@code ClientEntity}
   * objects. It uses a query provider generated by {@link #getProviderFactoryBean(DataSource)} to
   * construct the SQL query, ensuring that the query provider object is non-null.
   *
   * @param dataSource the data source from which client data will be read
   * @return a configured {@link JdbcPagingItemReader} for reading {@code ClientEntity} objects
   * @throws Exception if an error occurs while configuring the query provider
   */
  @Bean
  public JdbcPagingItemReader<ClientEntity> delegateClientReader(DataSource dataSource)
      throws Exception {
    var reader = new JdbcPagingItemReader<ClientEntity>();
    reader.setDataSource(dataSource);
    reader.setPageSize(batchProcessingConfiguration.getPageSize());
    reader.setRowMapper(new ClientRowMapper());

    var queryProvider = getProviderFactoryBean(dataSource);
    reader.setQueryProvider(Objects.requireNonNull(queryProvider.getObject()));
    return reader;
  }

  /**
   * Creates a {@link NamedParameterJdbcTemplate} bean for executing SQL queries that use named
   * parameters.
   *
   * @param dataSource the data source to be used by the JDBC template
   * @return a configured {@link NamedParameterJdbcTemplate} instance
   */
  @Bean
  public NamedParameterJdbcTemplate namedParameterJdbcTemplate(DataSource dataSource) {
    return new NamedParameterJdbcTemplate(dataSource);
  }
}
