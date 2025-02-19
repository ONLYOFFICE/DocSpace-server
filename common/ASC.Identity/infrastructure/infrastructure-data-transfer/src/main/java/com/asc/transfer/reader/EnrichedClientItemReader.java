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
package com.asc.transfer.reader;

import com.asc.transfer.entity.AuthenticationMethod;
import com.asc.transfer.entity.ClientEntity;
import com.asc.transfer.entity.ScopeEntity;
import java.util.*;
import java.util.stream.Collectors;
import org.springframework.batch.item.ExecutionContext;
import org.springframework.batch.item.ItemStreamException;
import org.springframework.batch.item.ItemStreamReader;
import org.springframework.jdbc.core.namedparam.MapSqlParameterSource;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

/**
 * An {@link ItemStreamReader} implementation that reads {@link ClientEntity} items in batches and
 * enriches them with additional data retrieved via JDBC.
 *
 * <p>This reader wraps a delegate {@link ItemStreamReader} that provides the raw client data. After
 * reading a batch of items, it enriches each {@link ClientEntity} by performing additional queries
 * to populate authentication methods, redirect URIs, allowed origins, and scopes.
 */
public class EnrichedClientItemReader implements ItemStreamReader<ClientEntity> {

  /** The delegate reader that provides the raw {@link ClientEntity} items. */
  private final ItemStreamReader<ClientEntity> delegateReader;

  /** The JDBC template used to execute additional queries for enriching client data. */
  private final NamedParameterJdbcTemplate namedParameterJdbcTemplate;

  /** The number of items to read in each batch. */
  private final int batchSize;

  /** A buffer that holds the current batch of {@link ClientEntity} items. */
  private List<ClientEntity> buffer;

  /** The index of the next item to return from the buffer. */
  private int currentIndex = 0;

  /**
   * Constructs an {@code EnrichedClientItemReader} with the given delegate reader, JDBC template,
   * and batch size.
   *
   * @param delegateReader the delegate reader {@link ItemStreamReader} providing raw client data
   * @param namedParameterJdbcTemplate the {@link NamedParameterJdbcTemplate} for executing
   *     enrichment queries
   * @param batchSize the number of items to read in each batch
   */
  public EnrichedClientItemReader(
      ItemStreamReader<ClientEntity> delegateReader,
      NamedParameterJdbcTemplate namedParameterJdbcTemplate,
      int batchSize) {
    this.delegateReader = delegateReader;
    this.namedParameterJdbcTemplate = namedParameterJdbcTemplate;
    this.batchSize = batchSize;
  }

  /**
   * Reads and returns the next enriched {@link ClientEntity} from the stream.
   *
   * <p>This method reads a batch of items from the delegate reader when necessary, enriches the
   * batch with additional data, and then returns items one by one from the buffer. When no more
   * items are available, it returns {@code null}.
   *
   * @return the next enriched {@link ClientEntity}, or {@code null} if no more items are available
   * @throws Exception if an error occurs during reading or enrichment
   */
  public ClientEntity read() throws Exception {
    if (buffer == null || currentIndex >= buffer.size()) {
      buffer = new ArrayList<>();
      ClientEntity item;
      for (int i = 0; i < batchSize; i++) {
        item = delegateReader.read();
        if (item == null) break;
        buffer.add(item);
      }
      if (buffer.isEmpty()) return null;
      enrichBuffer(buffer);
      currentIndex = 0;
    }

    return buffer.get(currentIndex++);
  }

  /**
   * Enriches a list of {@link ClientEntity} objects with additional data.
   *
   * <p>This method performs several additional queries to retrieve collections of values related to
   * each client, including authentication methods, redirect URIs, allowed origins, and scopes. It
   * then sets these values on each {@link ClientEntity}.
   *
   * @param clients the list of {@link ClientEntity} objects to enrich
   */
  private void enrichBuffer(List<ClientEntity> clients) {
    var clientIds = clients.stream().map(ClientEntity::getClientId).collect(Collectors.toList());

    var authMap =
        getCollectionMap(
            "SELECT client_id, authentication_method FROM identity_client_authentication_methods WHERE client_id IN (:ids)",
            clientIds);
    var redirectMap =
        getCollectionMap(
            "SELECT client_id, redirect_uri FROM identity_client_redirect_uris WHERE client_id IN (:ids)",
            clientIds);
    var allowedMap =
        getCollectionMap(
            "SELECT client_id, allowed_origin FROM identity_client_allowed_origins WHERE client_id IN (:ids)",
            clientIds);
    var scopesMap =
        getCollectionMap(
            "SELECT client_id, scope_name FROM identity_client_scopes WHERE client_id IN (:ids)",
            clientIds);

    for (var client : clients) {
      var id = client.getClientId();

      var authStrings = authMap.getOrDefault(id, Collections.emptySet());
      var authEnums =
          authStrings.stream().map(AuthenticationMethod::fromMethod).collect(Collectors.toSet());
      client.setAuthenticationMethods(authEnums);

      client.setRedirectUris(redirectMap.getOrDefault(id, Collections.emptySet()));
      client.setAllowedOrigins(allowedMap.getOrDefault(id, Collections.emptySet()));

      var scopeNames = scopesMap.getOrDefault(id, Collections.emptySet());
      var scopeEntities =
          scopeNames.stream()
              .map(
                  name -> {
                    var scope = new ScopeEntity();
                    scope.setName(name);
                    return scope;
                  })
              .collect(Collectors.toSet());
      client.setScopes(scopeEntities);
    }
  }

  /**
   * Executes a query to retrieve a collection of values and maps the results to a {@code Map} keyed
   * by client ID.
   *
   * <p>This method executes the provided SQL query using the list of client IDs as a parameter and
   * constructs a map where each key is a client ID and the corresponding value is a {@link Set} of
   * strings retrieved from the query.
   *
   * @param sql the SQL query to execute; the query must select the client ID and one additional
   *     column
   * @param clientIds the list of client IDs to use as a parameter in the query
   * @return a {@code Map} where each key is a client ID and the corresponding value is a {@link
   *     Set} of strings
   */
  private Map<String, Set<String>> getCollectionMap(String sql, List<String> clientIds) {
    var map = new HashMap<String, Set<String>>();
    var params = new MapSqlParameterSource("ids", clientIds);
    namedParameterJdbcTemplate.query(
        sql,
        params,
        rs -> {
          String clientId = rs.getString("client_id");
          String value = rs.getString(2);
          map.computeIfAbsent(clientId, k -> new HashSet<>()).add(value);
        });
    return map;
  }

  /**
   * Opens the underlying delegate reader with the given {@link ExecutionContext}.
   *
   * @param executionContext the context for the current execution
   * @throws ItemStreamException if an error occurs while opening the delegate reader
   */
  public void open(ExecutionContext executionContext) throws ItemStreamException {
    if (delegateReader != null) delegateReader.open(executionContext);
  }

  /**
   * Updates the underlying delegate reader with the given {@link ExecutionContext}.
   *
   * @param executionContext the context for the current execution
   * @throws ItemStreamException if an error occurs while updating the delegate reader
   */
  public void update(ExecutionContext executionContext) throws ItemStreamException {
    if (delegateReader != null) delegateReader.update(executionContext);
  }

  /**
   * Closes the underlying delegate reader.
   *
   * @throws ItemStreamException if an error occurs while closing the delegate reader
   */
  public void close() throws ItemStreamException {
    if (delegateReader != null) delegateReader.close();
  }
}
