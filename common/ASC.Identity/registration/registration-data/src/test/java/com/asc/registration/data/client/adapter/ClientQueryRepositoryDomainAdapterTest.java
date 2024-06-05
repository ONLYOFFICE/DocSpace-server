package com.asc.registration.data.client.adapter;

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import com.asc.registration.service.transfer.response.PageableResponse;
import java.util.Collections;
import java.util.Optional;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.data.domain.Page;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.Pageable;

class ClientQueryRepositoryDomainAdapterTest {
  @InjectMocks private ClientQueryRepositoryDomainAdapter clientQueryRepositoryDomainAdapter;
  @Mock private JpaClientRepository jpaClientRepository;
  @Mock private ClientDataAccessMapper clientDataAccessMapper;

  private Client client;
  private ClientId clientId;
  private TenantId tenantId;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    clientId = new ClientId(UUID.randomUUID());
    tenantId = new TenantId(1);

    client = Client.Builder.builder().id(clientId).build();
  }

  @Test
  void findById() {
    when(jpaClientRepository.findById(clientId.getValue().toString()))
        .thenReturn(Optional.of(mock(ClientEntity.class)));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    Optional<Client> result = clientQueryRepositoryDomainAdapter.findById(clientId);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
  }

  @Test
  void findAllByTenant() {
    Page<ClientEntity> clientEntities =
        new PageImpl<>(Collections.singletonList(mock(ClientEntity.class)));
    when(jpaClientRepository.findAllByTenant(eq(tenantId.getValue()), any(Pageable.class)))
        .thenReturn(clientEntities);
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    PageableResponse<Client> result =
        clientQueryRepositoryDomainAdapter.findAllByTenant(tenantId, 0, 10);

    assertNotNull(result.getData());
  }

  @Test
  void findClientByClientIdAndTenant() {
    when(jpaClientRepository.findClientByClientIdAndTenant(
            clientId.getValue().toString(), tenantId.getValue()))
        .thenReturn(Optional.of(mock(ClientEntity.class)));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    Optional<Client> result =
        clientQueryRepositoryDomainAdapter.findClientByClientIdAndTenant(clientId, tenantId);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
  }
}
