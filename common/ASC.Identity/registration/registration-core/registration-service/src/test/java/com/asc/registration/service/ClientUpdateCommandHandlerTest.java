// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.registration.service;

import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
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
import com.asc.registration.service.transfer.request.update.*;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

public class ClientUpdateCommandHandlerTest {
  @InjectMocks private ClientUpdateCommandHandler clientUpdateCommandHandler;
  @Mock private ClientDomainService clientDomainService;
  @Mock private EncryptionService encryptionService;
  @Mock private ClientQueryRepository clientQueryRepository;
  @Mock private ClientCommandRepository clientCommandRepository;
  @Mock private DomainEventPublisher<ClientEvent> messagePublisher;
  @Mock private ClientDataMapper clientDataMapper;

  private Audit audit;
  private Client client;
  private ClientResponse clientResponse;
  private ClientSecretResponse clientSecretResponse;

  @BeforeEach
  public void setUp() {
    MockitoAnnotations.openMocks(this);

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
            .clientTenantInfo(new ClientTenantInfo(new TenantId(1)))
            .clientRedirectInfo(
                new ClientRedirectInfo(
                    Set.of("http://redirect.url"),
                    Set.of("http://allowed.origin"),
                    Set.of("http://logout.url")))
            .clientCreationInfo(
                ClientCreationInfo.Builder.builder()
                    .createdBy("creator")
                    .createdOn(ZonedDateTime.now(ZoneId.of("UTC")))
                    .build())
            .build();
    client.initialize("creator");
    clientResponse =
        ClientResponse.builder()
            .clientId(client.getId().getValue().toString())
            .clientSecret(client.getSecret().value())
            .build();
    clientSecretResponse =
        ClientSecretResponse.builder().clientSecret(client.getSecret().value()).build();
  }

  @Test
  public void testRegenerateSecret() {
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
    when(clientCommandRepository.saveClient(any(Client.class))).thenReturn(client);
    when(encryptionService.encrypt(anyString())).thenReturn("encryptedSecret");
    when(clientDataMapper.toClientSecret(any(Client.class))).thenReturn(clientSecretResponse);

    var response = clientUpdateCommandHandler.regenerateSecret(audit, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDomainService, times(1))
        .regenerateClientSecret(any(Audit.class), any(Client.class));
    verify(encryptionService, times(1)).encrypt(anyString());
    verify(clientCommandRepository, times(1)).saveClient(any(Client.class));
    verify(messagePublisher, times(1)).publish(any(ClientEvent.class));
    verify(clientDataMapper, times(1)).toClientSecret(any(Client.class));

    assertNotEquals(client.getSecret().value(), response.getClientSecret());
  }

  @Test
  public void testRegenerateSecretClientNotFound() {
    var command =
        RegenerateTenantClientSecretCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .build();

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ClientNotFoundException.class,
        () -> clientUpdateCommandHandler.regenerateSecret(audit, command));
  }

  @Test
  public void testChangeVisibilityToPublic() {
    var command =
        ChangeTenantClientVisibilityCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .isPublic(true)
            .build();
    var clientUpdatedEvent = mock(ClientUpdatedEvent.class);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDomainService.makeClientPublic(any(Audit.class), any(Client.class)))
        .thenReturn(clientUpdatedEvent);

    clientUpdateCommandHandler.changeVisibility(audit, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDomainService, times(1)).makeClientPublic(any(Audit.class), any(Client.class));
    verify(clientCommandRepository, times(1)).saveClient(any(Client.class));
    verify(messagePublisher, times(1)).publish(any(ClientEvent.class));
  }

  @Test
  public void testChangeVisibilityToPrivate() {
    var command =
        ChangeTenantClientVisibilityCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .isPublic(false)
            .build();
    var clientUpdatedEvent = mock(ClientUpdatedEvent.class);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDomainService.makeClientPrivate(any(Audit.class), any(Client.class)))
        .thenReturn(clientUpdatedEvent);

    clientUpdateCommandHandler.changeVisibility(audit, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDomainService, times(1)).makeClientPrivate(any(Audit.class), any(Client.class));
    verify(clientCommandRepository, times(1)).saveClient(any(Client.class));
    verify(messagePublisher, times(1)).publish(any(ClientEvent.class));
  }

  @Test
  public void testChangeVisibilityClientNotFound() {
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
        () -> clientUpdateCommandHandler.changeVisibility(audit, command));
  }

  @Test
  public void testChangeActivation() {
    var command =
        ChangeTenantClientActivationCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .enabled(true)
            .build();

    var clientUpdatedEvent = mock(ClientUpdatedEvent.class);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDomainService.enableClient(any(Audit.class), any(Client.class)))
        .thenReturn(clientUpdatedEvent);

    clientUpdateCommandHandler.changeActivation(audit, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDomainService, times(1)).enableClient(any(Audit.class), any(Client.class));
    verify(clientCommandRepository, times(1)).saveClient(any(Client.class));
    verify(messagePublisher, times(1)).publish(any(ClientEvent.class));
  }

  @Test
  public void testChangeActivationClientNotFound() {
    var command =
        ChangeTenantClientActivationCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .enabled(true)
            .build();

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ClientNotFoundException.class,
        () -> clientUpdateCommandHandler.changeActivation(audit, command));
  }

  @Test
  public void testUpdateClientClientNotFound() {
    var command =
        UpdateTenantClientCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .name("Updated Client")
            .description("Updated Description")
            .logo("Updated Logo URL")
            .allowedOrigins(Set.of("http://allowed.origin"))
            .allowPkce(true)
            .isPublic(true)
            .build();

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ClientNotFoundException.class,
        () -> clientUpdateCommandHandler.updateClient(audit, command));
  }

  @Test
  public void testDeleteClient() {
    var command =
        DeleteTenantClientCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .build();
    var clientDeletedEvent = mock(ClientDeletedEvent.class);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDomainService.invalidateClient(any(Audit.class), any(Client.class)))
        .thenReturn(clientDeletedEvent);

    clientUpdateCommandHandler.deleteClient(audit, command);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDomainService, times(1)).invalidateClient(any(Audit.class), any(Client.class));
    verify(clientCommandRepository, times(1)).saveClient(any(Client.class));
    verify(messagePublisher, times(1)).publish(any(ClientEvent.class));
  }

  @Test
  public void testDeleteClientClientNotFound() {
    var command =
        DeleteTenantClientCommand.builder()
            .tenantId(1)
            .clientId(client.getId().getValue().toString())
            .build();

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(
        ClientNotFoundException.class,
        () -> clientUpdateCommandHandler.deleteClient(audit, command));
  }
}
