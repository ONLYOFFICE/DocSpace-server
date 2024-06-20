package com.asc.registration.service;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.entity.Audit;
import com.asc.common.core.domain.event.DomainEventPublisher;
import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
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
import org.junit.jupiter.api.Test;
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
  private Client client;
  private ClientResponse clientResponse;

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
            .clientVisibility(ClientVisibility.PRIVATE)
            .build();
    client.initialize("creator");
    client.encryptSecret(s -> "encryptedSecret");
    clientResponse =
        ClientResponse.builder()
            .clientId(client.getId().getValue().toString())
            .clientSecret(client.getSecret().value())
            .build();
  }

  @Test
  public void testCreateClient() {
    when(clientDataMapper.toDomain(any(CreateTenantClientCommand.class))).thenReturn(client);
    when(clientDomainService.createClient(any(Audit.class), any(Client.class)))
        .thenReturn(new ClientCreatedEvent(audit, client, ZonedDateTime.now()));
    when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);
    when(encryptionService.encrypt(anyString())).thenReturn("encryptedSecret");

    var response = clientCreateCommandHandler.createClient(audit, command);

    verify(clientDataMapper, times(1)).toDomain(any(CreateTenantClientCommand.class));
    verify(clientDomainService, times(1)).createClient(any(Audit.class), any(Client.class));
    verify(encryptionService, times(1)).encrypt(anyString());
    verify(clientCommandRepository, times(1)).saveClient(any(Client.class));
    verify(messagePublisher, times(1)).publish(any(ClientEvent.class));
    verify(clientDataMapper, times(1)).toClientResponse(any(Client.class));

    assertEquals(client.getSecret().value(), response.getClientSecret());
  }
}
