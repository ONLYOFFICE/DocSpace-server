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

package com.asc.identity.minified;

import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.CoreClientDomainService;
import com.asc.registration.service.*;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.mapper.ScopeDataMapper;
import com.asc.registration.service.ports.input.service.ClientApplicationService;
import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.ports.output.repository.ScopeQueryRepository;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import com.asc.registration.service.ports.output.resilience.RetryExecutor;
import com.asc.registration.service.ports.output.resilience.ScopeCacheService;
import jakarta.validation.Validator;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

/**
 * Configuration class for defining application beans for the minified deployment.
 *
 * <p>This class provides the necessary bean definitions required by the registration service,
 * enabling dependency injection and configuration of core services.
 */
@Configuration
public class BeanConfiguration {
  /**
   * Exposes the core client domain service for the minified.
   *
   * @return {@link ClientDomainService} implementation with core business logic
   */
  @Bean
  public ClientDomainService clientDomainService() {
    return new CoreClientDomainService();
  }

  /**
   * Creates a mapper between client domain entities and persistence/application models.
   *
   * @return new {@link ClientDataMapper} instance
   */
  @Bean
  public ClientDataMapper clientDataMapper() {
    return new ClientDataMapper();
  }

  /**
   * Creates a mapper between scope domain entities and persistence/application models.
   *
   * @return new {@link ScopeDataMapper} instance
   */
  @Bean
  public ScopeDataMapper scopeDataMapper() {
    return new ScopeDataMapper();
  }

  /**
   * Registers the command handler responsible for creating clients.
   *
   * @param clientCommandRepository repository used to persist client changes
   * @param clientDomainService domain service encapsulating client business rules
   * @param encryptionService service for encrypting sensitive client data
   * @param clientDataMapper mapper between domain and persistence models
   * @return fully configured {@link ClientCreateCommandHandler}
   */
  @Bean
  public ClientCreateCommandHandler clientCreateCommandHandler(
      ClientCommandRepository clientCommandRepository,
      ClientDomainService clientDomainService,
      EncryptionService encryptionService,
      ClientDataMapper clientDataMapper) {
    return new ClientCreateCommandHandler(
        clientCommandRepository, clientDomainService, encryptionService, clientDataMapper);
  }

  /**
   * Registers the handler used for querying client information.
   *
   * @param clientQueryRepository read-side repository for clients
   * @param clientCacheService cache used to speed up client lookups
   * @param encryptionService service for decrypting client secrets where needed
   * @param clientDataMapper mapper between domain and query models
   * @return {@link ClientQueryHandler} for client read operations
   */
  @Bean
  public ClientQueryHandler clientQueryHandler(
      ClientQueryRepository clientQueryRepository,
      ClientCacheService clientCacheService,
      EncryptionService encryptionService,
      ClientDataMapper clientDataMapper) {
    return new ClientQueryHandler(
        clientQueryRepository, clientCacheService, encryptionService, clientDataMapper);
  }

  /**
   * Registers the command handler responsible for updating existing clients.
   *
   * @param clientCommandRepository write-side client repository
   * @param clientQueryRepository read-side client repository
   * @param clientDomainService domain service with validation and invariants
   * @param encryptionService encryption service for client secrets
   * @param authorizationMessagePublisher publisher used to trigger cleanup when a client is removed
   * @param clientDataMapper mapper between domain and persistence models
   * @param retryExecutor executor for retrying transient failures
   * @return configured {@link ClientUpdateCommandHandler}
   */
  @Bean
  public ClientUpdateCommandHandler clientUpdateCommandHandler(
      ClientCommandRepository clientCommandRepository,
      ClientQueryRepository clientQueryRepository,
      ClientDomainService clientDomainService,
      EncryptionService encryptionService,
      AuthorizationMessagePublisher<ClientRemovedEvent> authorizationMessagePublisher,
      ClientDataMapper clientDataMapper,
      RetryExecutor retryExecutor) {
    return new ClientUpdateCommandHandler(
        clientCommandRepository,
        clientQueryRepository,
        clientDomainService,
        encryptionService,
        authorizationMessagePublisher,
        clientDataMapper,
        retryExecutor);
  }

  /**
   * Registers the handler used for querying scopes.
   *
   * @param scopeQueryRepository read-side repository for scopes
   * @param scopeDataMapper mapper between domain and query models
   * @return {@link ScopeQueryHandler} for scope read operations
   */
  @Bean
  public ScopeQueryHandler scopeQueryHandler(
      ScopeQueryRepository scopeQueryRepository, ScopeDataMapper scopeDataMapper) {
    return new ScopeQueryHandler(scopeQueryRepository, scopeDataMapper);
  }

  /**
   * Wires the core client application service used by the minified.
   *
   * @param validator bean validation entry point
   * @param clientCacheService cache abstraction for clients
   * @param tenantClientsRemovedEventAuthorizationMessagePublisher publisher for tenant-level
   *     cleanup events
   * @param userClientsRemovedEventAuthorizationMessagePublisher publisher for user-level cleanup
   *     events
   * @param clientCacheTenantRemoveEventAuthorizationMessagePublisher publisher for cache-only
   *     tenant cleanup events
   * @param clientCreateCommandHandler handler for client creation use cases
   * @param clientUpdateCommandHandler handler for client update use cases
   * @param clientQueryHandler handler for client query use cases
   * @return configured {@link ClientApplicationService} implementation
   */
  @Bean
  public ClientApplicationService clientApplicationService(
      Validator validator,
      ClientCacheService clientCacheService,
      AuthorizationMessagePublisher<TenantClientsRemovedEvent>
          tenantClientsRemovedEventAuthorizationMessagePublisher,
      AuthorizationMessagePublisher<UserClientsRemovedEvent>
          userClientsRemovedEventAuthorizationMessagePublisher,
      AuthorizationMessagePublisher<ClientCacheTenantRemoveEvent>
          clientCacheTenantRemoveEventAuthorizationMessagePublisher,
      ClientCreateCommandHandler clientCreateCommandHandler,
      ClientUpdateCommandHandler clientUpdateCommandHandler,
      ClientQueryHandler clientQueryHandler) {
    return new CoreClientApplicationService(
        validator,
        clientCacheService,
        tenantClientsRemovedEventAuthorizationMessagePublisher,
        userClientsRemovedEventAuthorizationMessagePublisher,
        clientCacheTenantRemoveEventAuthorizationMessagePublisher,
        clientCreateCommandHandler,
        clientUpdateCommandHandler,
        clientQueryHandler);
  }

  /**
   * Wires the scope application service used by the minified.
   *
   * @param scopeCacheService cache abstraction for scopes
   * @param scopeQueryHandler handler for scope query operations
   * @return configured {@link ScopeApplicationService} implementation
   */
  @Bean
  public ScopeApplicationService scopeApplicationService(
      ScopeCacheService scopeCacheService, ScopeQueryHandler scopeQueryHandler) {
    return new CoreScopeApplicationService(scopeCacheService, scopeQueryHandler);
  }
}
