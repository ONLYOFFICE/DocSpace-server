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

package com.asc.registration.service;

import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.Role;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientRemovedEvent;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientDeletedEvent;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.core.domain.event.ClientUpdatedEvent;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.core.domain.value.ClientCreationInfo;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientTenantInfo;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.ports.output.resilience.RetryExecutor;
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import java.util.function.BiConsumer;
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.MethodSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

public class ClientUpdateCommandHandlerTest {
  private final UserId CREATOR_ID = new UserId("creator");

  @InjectMocks private ClientUpdateCommandHandler clientUpdateCommandHandler;
  @Mock private AuthorizationMessagePublisher<ClientRemovedEvent> authorizationMessagePublisher;
  @Mock private ClientDomainService clientDomainService;
  @Mock private EncryptionService encryptionService;
  @Mock private ClientQueryRepository clientQueryRepository;
  @Mock private ClientCommandRepository clientCommandRepository;
  @Mock private ClientDataMapper clientDataMapper;
  @Mock private RetryExecutor retryExecutor;

  private Audit audit;
  private Client client;
  private ClientResponse clientResponse;
  private ClientSecretResponse clientSecretResponse;

  @BeforeEach
  public void setUp() {
    MockitoAnnotations.openMocks(this);

    doAnswer(
            invocation -> {
              var operation = invocation.getArgument(1, Runnable.class);
              operation.run();
              return null;
            })
        .when(retryExecutor)
        .executeWithRetry(
            anyString(),
            any(Runnable.class),
            any(Class.class),
            any(java.util.function.Supplier.class));
    doAnswer(
            invocation -> {
              var operation = invocation.getArgument(1, java.util.function.Supplier.class);
              return operation.get();
            })
        .when(retryExecutor)
        .executeWithRetry(
            anyString(),
            any(java.util.function.Supplier.class),
            any(Class.class),
            any(java.util.function.Supplier.class));

    audit =
        Audit.Builder.builder()
            .auditCode(AuditCode.UPDATE_CLIENT)
            .initiator("initiator")
            .target("target")
            .ip("ip")
            .browser("browser")
            .platform("platform")
            .tenantId(1)
            .userEmail("email")
            .userName("name")
            .userId("id")
            .page("page")
            .description("description")
            .build();
    client =
        Client.Builder.builder()
            .id(new ClientId(UUID.randomUUID()))
            .secret(new ClientSecret("encryptedSecret"))
            .authenticationMethods(Set.of(AuthenticationMethod.DEFAULT_AUTHENTICATION))
            .scopes(Set.of("read", "write"))
            .clientInfo(new ClientInfo("Test Client", "Description", "Logo URL"))
            .clientTenantInfo(new ClientTenantInfo(new TenantId(1L)))
            .clientRedirectInfo(
                new ClientRedirectInfo(
                    Set.of("http://redirect.url"),
                    Set.of("http://allowed.origin"),
                    Set.of("http://logout.url")))
            .clientCreationInfo(
                ClientCreationInfo.Builder.builder()
                    .createdBy(CREATOR_ID)
                    .createdOn(ZonedDateTime.now(ZoneId.of("UTC")))
                    .build())
            .build();
    client.initialize(CREATOR_ID);
    clientResponse =
        ClientResponse.builder()
            .clientId(client.getId().getValue().toString())
            .clientSecret(client.getSecret().value())
            .build();
    clientSecretResponse =
        ClientSecretResponse.builder().clientSecret(client.getSecret().value()).build();
  }

  static Stream<Arguments> visibilityCases() {
    return Stream.of(
        Arguments.of(
            true,
            (BiConsumer<ClientDomainService, Client>)
                (service, client) ->
                    verify(service, times(1)).makeClientPublic(any(Audit.class), eq(client))),
        Arguments.of(
            false,
            (BiConsumer<ClientDomainService, Client>)
                (service, client) ->
                    verify(service, times(1)).makeClientPrivate(any(Audit.class), eq(client))));
  }

  @Test
  public void whenSecretIsRegenerated_thenNewSecretIsReturned() {
    var command =
        RegenerateTenantClientSecretCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .build();
    var clientUpdatedEvent = mock(ClientUpdatedEvent.class);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDomainService.regenerateClientSecret(any(Audit.class), any(Client.class)))
        .thenReturn(clientUpdatedEvent);
    when(clientCommandRepository.updateClient(any(ClientEvent.class), any(Client.class)))
        .thenReturn(client);
    when(encryptionService.encrypt(anyString())).thenReturn("encryptedSecret");
    when(clientDataMapper.toClientSecret(any(Client.class))).thenReturn(clientSecretResponse);

    var response = clientUpdateCommandHandler.regenerateSecret(audit, Role.ROLE_ADMIN, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDomainService, times(1))
        .regenerateClientSecret(any(Audit.class), any(Client.class));
    verify(encryptionService, times(1)).encrypt(anyString());
    verify(clientCommandRepository, times(1))
        .updateClient(any(ClientEvent.class), any(Client.class));
    verify(clientDataMapper, times(1)).toClientSecret(any(Client.class));

    assertNotEquals(client.getSecret().value(), response.getClientSecret());
  }

  @Test
  public void whenSecretIsRegeneratedForNonexistentClient_thenThrowClientNotFoundException() {
    var command =
        RegenerateTenantClientSecretCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .build();

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ClientNotFoundException.class,
        () -> clientUpdateCommandHandler.regenerateSecret(audit, Role.ROLE_ADMIN, command));
  }

  @ParameterizedTest
  @MethodSource("visibilityCases")
  public void whenVisibilityIsChanged_thenVisibilityIsUpdated(
      boolean isPublic, BiConsumer<ClientDomainService, Client> domainVerifier) {
    var command =
        ChangeTenantClientVisibilityCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .isPublic(isPublic)
            .build();
    var clientUpdatedEvent = mock(ClientUpdatedEvent.class);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    if (isPublic) {
      when(clientDomainService.makeClientPublic(any(Audit.class), any(Client.class)))
          .thenReturn(clientUpdatedEvent);
    } else {
      when(clientDomainService.makeClientPrivate(any(Audit.class), any(Client.class)))
          .thenReturn(clientUpdatedEvent);
    }

    clientUpdateCommandHandler.changeVisibility(audit, Role.ROLE_ADMIN, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    domainVerifier.accept(clientDomainService, client);
    verify(clientCommandRepository, times(1))
        .changeVisibilityByTenantIdAndClientId(
            any(ClientEvent.class), any(TenantId.class), any(ClientId.class), anyBoolean());
  }

  @Test
  public void whenVisibilityChangeForNonexistentClient_thenThrowClientNotFoundException() {
    var command =
        ChangeTenantClientVisibilityCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .isPublic(true)
            .build();

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ClientNotFoundException.class,
        () -> clientUpdateCommandHandler.changeVisibility(audit, Role.ROLE_ADMIN, command));
  }

  @Test
  public void whenClientIsDeleted_thenClientIsRemoved() {
    var command =
        DeleteTenantClientCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .build();
    var clientDeletedEvent = mock(ClientDeletedEvent.class);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDomainService.deleteClient(any(Audit.class), any(Client.class)))
        .thenReturn(clientDeletedEvent);

    clientUpdateCommandHandler.deleteClient(audit, Role.ROLE_ADMIN, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDomainService, times(1)).deleteClient(any(Audit.class), any(Client.class));
    verify(clientCommandRepository, times(1))
        .deleteByTenantIdAndClientId(
            any(ClientEvent.class), any(TenantId.class), any(ClientId.class));
    verify(authorizationMessagePublisher).publish(any(ClientRemovedEvent.class));
  }

  @Test
  public void whenDeletingNonexistentClient_thenThrowClientNotFoundException() {
    var command =
        DeleteTenantClientCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .build();

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ClientNotFoundException.class,
        () -> clientUpdateCommandHandler.deleteClient(audit, Role.ROLE_ADMIN, command));
  }
}
