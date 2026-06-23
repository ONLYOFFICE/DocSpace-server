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

package com.asc.registration.container;

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
 * Configuration class for defining application beans.
 *
 * <p>This class provides the necessary bean definitions required by the application, enabling
 * dependency injection and configuration of core services.
 */
@Configuration
public class BeanConfiguration {

  /**
   * Creates and registers a {@link ClientDomainService} bean in the application context.
   *
   * @return a new instance of {@link CoreClientDomainService}, which implements {@link
   *     ClientDomainService}.
   */
  @Bean
  public ClientDomainService clientDomainService() {
    return new CoreClientDomainService();
  }

  /**
   * Creates and registers a {@link ClientDataMapper} bean for mapping client domain objects to
   * DTOs.
   *
   * @return a new instance of {@link ClientDataMapper}.
   */
  @Bean
  public ClientDataMapper clientDataMapper() {
    return new ClientDataMapper();
  }

  /**
   * Creates and registers a {@link ScopeDataMapper} bean for mapping scope domain objects to DTOs.
   *
   * @return a new instance of {@link ScopeDataMapper}.
   */
  @Bean
  public ScopeDataMapper scopeDataMapper() {
    return new ScopeDataMapper();
  }

  /**
   * Creates and registers a {@link ClientCreateCommandHandler} bean for handling client creation
   * commands.
   *
   * @param clientCommandRepository the repository for persisting client commands.
   * @param clientDomainService the domain service for client business logic.
   * @param encryptionService the service for encrypting sensitive client data.
   * @param clientDataMapper the mapper for converting between domain objects and DTOs.
   * @return a new instance of {@link ClientCreateCommandHandler}.
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
   * Creates and registers a {@link ClientQueryHandler} bean for handling client query operations.
   *
   * @param clientQueryRepository the repository for querying client data.
   * @param clientCacheService the cache service for retrieving cached client data.
   * @param encryptionService the service for decrypting sensitive client data.
   * @param clientDataMapper the mapper for converting between domain objects and DTOs.
   * @return a new instance of {@link ClientQueryHandler}.
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
   * Creates and registers a {@link ClientUpdateCommandHandler} bean for handling client update
   * commands.
   *
   * @param clientCommandRepository the repository for persisting client commands.
   * @param clientQueryRepository the repository for querying existing client data.
   * @param clientDomainService the domain service for client business logic.
   * @param encryptionService the service for encrypting sensitive client data.
   * @param authorizationMessagePublisher the message publisher for client removal events.
   * @param clientDataMapper the mapper for converting between domain objects and DTOs.
   * @param retryExecutor the executor for handling retry logic on failed operations.
   * @return a new instance of {@link ClientUpdateCommandHandler}.
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
   * Creates and registers a {@link ScopeQueryHandler} bean for handling scope query operations.
   *
   * @param scopeQueryRepository the repository for querying scope data.
   * @param scopeDataMapper the mapper for converting between scope domain objects and DTOs.
   * @return a new instance of {@link ScopeQueryHandler}.
   */
  @Bean
  public ScopeQueryHandler scopeQueryHandler(
      ScopeQueryRepository scopeQueryRepository, ScopeDataMapper scopeDataMapper) {
    return new ScopeQueryHandler(scopeQueryRepository, scopeDataMapper);
  }

  /**
   * Creates and registers a {@link ClientApplicationService} bean as the main entry point for
   * client-related operations.
   *
   * @param validator the Jakarta Bean Validation validator.
   * @param tenantClientsRemovedEventAuthorizationMessagePublisher the message publisher for tenant
   *     client removal events.
   * @param userClientsRemovedEventAuthorizationMessagePublisher the message publisher for user
   *     client removal events.
   * @param clientCreateCommandHandler the handler for client creation commands.
   * @param clientUpdateCommandHandler the handler for client update commands.
   * @param clientQueryHandler the handler for client query operations.
   * @return a new instance of {@link CoreClientApplicationService}.
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
   * Creates and registers a {@link ScopeApplicationService} bean as the main entry point for
   * scope-related operations.
   *
   * @param scopeCacheService the caching service.
   * @param scopeQueryHandler the handler for scope query operations.
   * @return a new instance of {@link CoreScopeApplicationService}.
   */
  @Bean
  public ScopeApplicationService scopeApplicationService(
      ScopeCacheService scopeCacheService, ScopeQueryHandler scopeQueryHandler) {
    return new CoreScopeApplicationService(scopeCacheService, scopeQueryHandler);
  }
}
