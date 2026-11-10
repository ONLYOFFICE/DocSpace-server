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

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.TenantId;
import com.asc.common.service.ports.output.message.publisher.AuthorizationMessagePublisher;
import com.asc.common.service.transfer.message.ClientCacheTenantRemoveEvent;
import com.asc.common.service.transfer.message.TenantClientsRemovedEvent;
import com.asc.common.service.transfer.message.UserClientsRemovedEvent;
import com.asc.registration.service.ports.input.service.ScopeApplicationService;
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
import com.asc.registration.service.transfer.request.update.DeleteTenantClientsCommand;
import com.asc.registration.service.transfer.request.update.DeleteUserClientsCommand;
import jakarta.validation.Validator;
import java.time.Instant;
import java.util.concurrent.atomic.AtomicReference;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;

class CoreClientApplicationServiceTest {
  private static final long TENANT = 7L;
  private static final String USER = "user";

  private ClientCacheService clientCacheService;
  private AuthorizationMessagePublisher<TenantClientsRemovedEvent> tenantClientsPublisher;
  private AuthorizationMessagePublisher<UserClientsRemovedEvent> userClientsPublisher;
  private AuthorizationMessagePublisher<ClientCacheTenantRemoveEvent> cacheTenantRemovePublisher;
  private ClientUpdateCommandHandler clientUpdateCommandHandler;
  private CoreClientApplicationService service;

  @BeforeEach
  @SuppressWarnings("unchecked")
  void setUp() {
    clientCacheService = mock(ClientCacheService.class);
    tenantClientsPublisher = mock(AuthorizationMessagePublisher.class);
    userClientsPublisher = mock(AuthorizationMessagePublisher.class);
    cacheTenantRemovePublisher = mock(AuthorizationMessagePublisher.class);
    clientUpdateCommandHandler = mock(ClientUpdateCommandHandler.class);
    service =
        new CoreClientApplicationService(
            mock(Validator.class),
            clientCacheService,
            mock(ScopeApplicationService.class),
            tenantClientsPublisher,
            userClientsPublisher,
            cacheTenantRemovePublisher,
            mock(ClientCreateCommandHandler.class),
            clientUpdateCommandHandler,
            mock(ClientQueryHandler.class));
  }

  private DeleteUserClientsCommand userCommand() {
    return DeleteUserClientsCommand.builder().tenantId(TENANT).userId(USER).build();
  }

  private DeleteTenantClientsCommand tenantCommand() {
    return DeleteTenantClientsCommand.builder().tenantId(TENANT).build();
  }

  @Test
  void whenUserClientsDeleted_thenCacheEvictedBeforeAndAfterDeleteAndCleanupPublishedAfter() {
    when(clientUpdateCommandHandler.deleteUserClients(any())).thenReturn(3);

    assertEquals(3, service.deleteUserClients(userCommand()));

    var order =
        inOrder(
            clientUpdateCommandHandler,
            clientCacheService,
            cacheTenantRemovePublisher,
            userClientsPublisher);
    order.verify(clientCacheService).evictAllByTenantId(new TenantId(TENANT));
    order.verify(cacheTenantRemovePublisher).publish(any(ClientCacheTenantRemoveEvent.class));
    order.verify(clientUpdateCommandHandler).deleteUserClients(any());
    order.verify(clientCacheService).evictAllByTenantId(new TenantId(TENANT));
    order.verify(cacheTenantRemovePublisher).publish(any(ClientCacheTenantRemoveEvent.class));
    order.verify(userClientsPublisher).publish(any(UserClientsRemovedEvent.class));
  }

  @Test
  void whenUserClientsDeleteFails_thenEvictedBeforeOnlyAndNoCleanupPublished() {
    when(clientUpdateCommandHandler.deleteUserClients(any()))
        .thenThrow(new IllegalStateException("rolled back"));

    assertThrows(IllegalStateException.class, () -> service.deleteUserClients(userCommand()));

    verify(clientCacheService, times(1)).evictAllByTenantId(new TenantId(TENANT));
    verify(cacheTenantRemovePublisher, times(1)).publish(any(ClientCacheTenantRemoveEvent.class));
    verifyNoInteractions(userClientsPublisher);
  }

  @Test
  void whenTenantClientsDeleted_thenCacheEvictedBeforeAndAfterDeleteAndCleanupPublishedAfter() {
    when(clientUpdateCommandHandler.deleteTenantClients(TENANT)).thenReturn(5);

    assertEquals(5, service.deleteTenantClients(tenantCommand()));

    var order =
        inOrder(
            clientUpdateCommandHandler,
            clientCacheService,
            cacheTenantRemovePublisher,
            tenantClientsPublisher);
    order.verify(clientCacheService).evictAllByTenantId(new TenantId(TENANT));
    order.verify(cacheTenantRemovePublisher).publish(any(ClientCacheTenantRemoveEvent.class));
    order.verify(clientUpdateCommandHandler).deleteTenantClients(TENANT);
    order.verify(clientCacheService).evictAllByTenantId(new TenantId(TENANT));
    order.verify(cacheTenantRemovePublisher).publish(any(ClientCacheTenantRemoveEvent.class));
    order.verify(tenantClientsPublisher).publish(any(TenantClientsRemovedEvent.class));
  }

  @Test
  void whenTenantClientsDeleteFails_thenEvictedBeforeOnlyAndNoCleanupPublished() {
    when(clientUpdateCommandHandler.deleteTenantClients(TENANT))
        .thenThrow(new IllegalStateException("rolled back"));

    assertThrows(IllegalStateException.class, () -> service.deleteTenantClients(tenantCommand()));

    verify(clientCacheService, times(1)).evictAllByTenantId(new TenantId(TENANT));
    verify(cacheTenantRemovePublisher, times(1)).publish(any(ClientCacheTenantRemoveEvent.class));
    verifyNoInteractions(tenantClientsPublisher);
  }

  @Test
  void whenUserClientsDeleted_thenCleanupCarriesMomentAfterDelete() {
    var deletedAt = new AtomicReference<Instant>();
    when(clientUpdateCommandHandler.deleteUserClients(any()))
        .thenAnswer(
            _ -> {
              deletedAt.set(Instant.now());
              return 1;
            });

    service.deleteUserClients(userCommand());

    var event = ArgumentCaptor.forClass(UserClientsRemovedEvent.class);
    verify(userClientsPublisher).publish(event.capture());
    assertEquals(USER, event.getValue().getUserId());
    assertEquals(TENANT, event.getValue().getTenantId());
    assertTrue(!event.getValue().getRemovedAt().isBefore(deletedAt.get()));
    assertTrue(!event.getValue().getRemovedAt().isAfter(Instant.now()));
  }

  @Test
  void whenTenantClientsDeleted_thenCleanupCarriesMomentAfterDelete() {
    var deletedAt = new AtomicReference<Instant>();
    when(clientUpdateCommandHandler.deleteTenantClients(TENANT))
        .thenAnswer(
            _ -> {
              deletedAt.set(Instant.now());
              return 1;
            });

    service.deleteTenantClients(tenantCommand());

    var event = ArgumentCaptor.forClass(TenantClientsRemovedEvent.class);
    verify(tenantClientsPublisher).publish(event.capture());
    assertEquals(TENANT, event.getValue().getTenantId());
    assertTrue(!event.getValue().getRemovedAt().isBefore(deletedAt.get()));
    assertTrue(!event.getValue().getRemovedAt().isAfter(Instant.now()));
  }

  @Test
  void givenCacheFailingBeforeDelete_whenTenantClientsDeleted_thenStillDeleted() {
    when(clientUpdateCommandHandler.deleteTenantClients(TENANT)).thenReturn(5);
    doThrow(new IllegalStateException("cache down"))
        .doNothing()
        .when(clientCacheService)
        .evictAllByTenantId(new TenantId(TENANT));

    assertEquals(5, service.deleteTenantClients(tenantCommand()));

    verify(tenantClientsPublisher).publish(any(TenantClientsRemovedEvent.class));
  }
}
