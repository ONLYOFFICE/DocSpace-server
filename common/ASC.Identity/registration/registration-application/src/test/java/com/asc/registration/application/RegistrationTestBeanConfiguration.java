// (c) Copyright Ascensio System SIA 2009-2026
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.registration.application;

import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.application.configuration.ScopeCacheInitializer;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.CoreClientDomainService;
import com.asc.registration.service.ClientCreateCommandHandler;
import com.asc.registration.service.ClientQueryHandler;
import com.asc.registration.service.ClientUpdateCommandHandler;
import com.asc.registration.service.CoreClientApplicationService;
import com.asc.registration.service.CoreScopeApplicationService;
import com.asc.registration.service.ScopeQueryHandler;
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
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Bean;

@TestConfiguration
public class RegistrationTestBeanConfiguration {
  @Bean
  public ClientDomainService clientDomainService() {
    return new CoreClientDomainService();
  }

  @Bean
  public ClientDataMapper clientDataMapper() {
    return new ClientDataMapper();
  }

  @Bean
  public ScopeDataMapper scopeDataMapper() {
    return new ScopeDataMapper();
  }

  @Bean
  public ClientCreateCommandHandler clientCreateCommandHandler(
      ClientCommandRepository clientCommandRepository,
      ClientDomainService clientDomainService,
      EncryptionService encryptionService,
      ClientDataMapper clientDataMapper) {
    return new ClientCreateCommandHandler(
        clientCommandRepository, clientDomainService, encryptionService, clientDataMapper);
  }

  @Bean
  public ClientQueryHandler clientQueryHandler(
      ClientQueryRepository clientQueryRepository,
      ClientCacheService clientCacheService,
      EncryptionService encryptionService,
      ClientDataMapper clientDataMapper) {
    return new ClientQueryHandler(
        clientQueryRepository, clientCacheService, encryptionService, clientDataMapper);
  }

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

  @Bean
  public ScopeQueryHandler scopeQueryHandler(
      ScopeQueryRepository scopeQueryRepository, ScopeDataMapper scopeDataMapper) {
    return new ScopeQueryHandler(scopeQueryRepository, scopeDataMapper);
  }

  @Bean
  public ClientApplicationService clientApplicationService(
      Validator validator,
      ClientCacheService clientCacheService,
      ScopeApplicationService scopeApplicationService,
      AuthorizationMessagePublisher<TenantClientsRemovedEvent> tenantClientsRemovedPublisher,
      AuthorizationMessagePublisher<UserClientsRemovedEvent> userClientsRemovedPublisher,
      AuthorizationMessagePublisher<ClientCacheTenantRemoveEvent> clientCacheTenantRemovePublisher,
      ClientCreateCommandHandler clientCreateCommandHandler,
      ClientUpdateCommandHandler clientUpdateCommandHandler,
      ClientQueryHandler clientQueryHandler) {
    return new CoreClientApplicationService(
        validator,
        clientCacheService,
        scopeApplicationService,
        tenantClientsRemovedPublisher,
        userClientsRemovedPublisher,
        clientCacheTenantRemovePublisher,
        clientCreateCommandHandler,
        clientUpdateCommandHandler,
        clientQueryHandler);
  }

  @Bean
  public ScopeApplicationService scopeApplicationService(
      ScopeCacheService scopeCacheService, ScopeQueryHandler scopeQueryHandler) {
    return new CoreScopeApplicationService(scopeCacheService, scopeQueryHandler);
  }

  @Bean
  public ScopeCacheInitializer scopeCacheInitializer(
      ScopeApplicationService scopeApplicationService) {
    return new ScopeCacheInitializer(scopeApplicationService);
  }
}
