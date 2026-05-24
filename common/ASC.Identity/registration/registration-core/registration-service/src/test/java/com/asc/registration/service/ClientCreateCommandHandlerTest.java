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
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.ClientDomainService;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.event.ClientCreatedEvent;
import com.asc.registration.core.domain.event.ClientEvent;
import com.asc.registration.core.domain.value.ClientCreationInfo;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientTenantInfo;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientCommandRepository;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

public class ClientCreateCommandHandlerTest {
  @InjectMocks private ClientCreateCommandHandler clientCreateCommandHandler;
  @Mock private ClientDomainService clientDomainService;
  @Mock private EncryptionService encryptionService;
  @Mock private ClientCommandRepository clientCommandRepository;
  @Mock private DomainEventPublisher<ClientEvent> messagePublisher;
  @Mock private ClientDataMapper clientDataMapper;

  private CreateTenantClientCommand command;
  private Audit audit;

  @BeforeEach
  public void setUp() {
    MockitoAnnotations.openMocks(this);

    audit =
        Audit.Builder.builder()
            .auditCode(AuditCode.CREATE_CLIENT)
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
    command =
        CreateTenantClientCommand.builder()
            .tenantId(1)
            .name("Test Client")
            .description("Test Description")
            .build();
  }

  private static Client buildClient(String plaintextSecret) {
    var client =
        Client.Builder.builder()
            .id(new ClientId(UUID.randomUUID()))
            .secret(new ClientSecret(plaintextSecret))
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
                    .createdBy(new UserId("creator"))
                    .createdOn(ZonedDateTime.now(ZoneId.of("UTC")))
                    .build())
            .clientVisibility(ClientVisibility.PRIVATE)
            .build();
    client.initialize(new UserId("creator"));
    client.encryptSecret(s -> plaintextSecret);
    return client;
  }

  @ParameterizedTest
  @ValueSource(
      strings = {"encryptedSecret", "another-plain-secret", "1234567890", "secret-with-symbols!@#"})
  public void whenClientIsCreated_thenReturnClientResponse(String plaintextSecret) {
    var client = buildClient(plaintextSecret);
    var clientResponse =
        ClientResponse.builder()
            .clientId(client.getId().getValue().toString())
            .clientSecret(client.getSecret().value())
            .build();

    when(clientDataMapper.toDomain(any(CreateTenantClientCommand.class))).thenReturn(client);
    when(clientDomainService.createClient(any(Audit.class), any(Client.class)))
        .thenReturn(new ClientCreatedEvent(audit, client, ZonedDateTime.now()));
    when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);
    when(encryptionService.encrypt(anyString())).thenReturn("encrypted-at-rest");

    var response = clientCreateCommandHandler.createClient(audit, command);

    verify(clientDataMapper, times(1)).toDomain(any(CreateTenantClientCommand.class));
    verify(clientDomainService, times(1)).createClient(any(Audit.class), any(Client.class));
    verify(encryptionService, times(1)).encrypt(anyString());
    verify(clientCommandRepository, times(1)).saveClient(any(ClientEvent.class), any(Client.class));
    verify(clientDataMapper, times(1)).toClientResponse(any(Client.class));

    assertEquals(plaintextSecret, response.getClientSecret());
  }
}
