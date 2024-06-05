package com.asc.registration.service;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.anyInt;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.utilities.cipher.EncryptionService;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.request.fetch.TenantClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

@ExtendWith(MockitoExtension.class)
class ClientQueryHandlerTest {
  @InjectMocks private ClientQueryHandler clientQueryHandler;
  @Mock private EncryptionService encryptionService;
  @Mock private ClientQueryRepository clientQueryRepository;
  @Mock private ClientDataMapper clientDataMapper;

  private TenantClientQuery tenantClientQuery;
  private TenantClientInfoQuery tenantClientInfoQuery;
  private TenantClientsPaginationQuery tenantClientsPaginationQuery;
  private Client client;
  private ClientResponse clientResponse;
  private ClientInfoResponse clientInfoResponse;
  private PageableResponse<Client> pageableResponse;

  @BeforeEach
  void setUp() {
    tenantClientQuery = new TenantClientQuery(1, UUID.randomUUID().toString());
    tenantClientInfoQuery = new TenantClientInfoQuery(UUID.randomUUID().toString());
    tenantClientsPaginationQuery = new TenantClientsPaginationQuery(1, 0, 10);

    client = mock(Client.class);
    clientResponse = mock(ClientResponse.class);
    clientInfoResponse = mock(ClientInfoResponse.class);
    pageableResponse = new PageableResponse<>(Set.of(client), 0, 10, 1, null);
  }

  @Test
  void getClient() {
    when(clientQueryRepository.findClientByClientIdAndTenant(
            any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);
    when(clientResponse.getClientSecret()).thenReturn("encryptedSecret");
    when(encryptionService.decrypt("encryptedSecret")).thenReturn("decryptedSecret");

    ClientResponse response = clientQueryHandler.getClient(tenantClientQuery);

    verify(clientQueryRepository)
        .findClientByClientIdAndTenant(any(ClientId.class), any(TenantId.class));
    verify(clientDataMapper).toClientResponse(any(Client.class));
    verify(encryptionService).decrypt("encryptedSecret");
    assertNotNull(response);
  }

  @Test
  void getClientInfo() {
    when(clientQueryRepository.findById(any(ClientId.class))).thenReturn(Optional.of(client));
    when(clientDataMapper.toClientInfoResponse(any(Client.class))).thenReturn(clientInfoResponse);

    ClientInfoResponse response = clientQueryHandler.getClientInfo(tenantClientInfoQuery);

    verify(clientQueryRepository).findById(any(ClientId.class));
    verify(clientDataMapper).toClientInfoResponse(any(Client.class));
    assertNotNull(response);
  }

  @Test
  void getClients() {
    when(clientQueryRepository.findAllByTenant(any(TenantId.class), anyInt(), anyInt()))
        .thenReturn(pageableResponse);
    when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);

    PageableResponse<ClientResponse> response =
        clientQueryHandler.getClients(tenantClientsPaginationQuery);

    verify(clientQueryRepository).findAllByTenant(any(TenantId.class), anyInt(), anyInt());
    verify(clientDataMapper, times(1)).toClientResponse(any(Client.class));
    assertNotNull(response);
    assertEquals(0, response.getPage());
    assertEquals(10, response.getLimit());
    assertEquals(1, response.getNext());
    assertNull(response.getPrevious());
  }
}
