package com.asc.registration.data.client.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;
import static org.mockito.Mockito.anyBoolean;
import static org.mockito.Mockito.eq;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import java.util.ArrayList;
import java.util.Optional;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;
import org.springframework.data.domain.PageImpl;
import org.springframework.data.domain.PageRequest;

class ClientQueryRepositoryDomainAdapterTest {
  @InjectMocks private ClientQueryRepositoryDomainAdapter adapter;
  @Mock private JpaClientRepository jpaClientRepository;
  @Mock private ClientDataAccessMapper clientDataAccessMapper;

  private ClientId clientId;
  private TenantId tenantId;
  private ClientEntity clientEntity;
  private Client client;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);
    clientId = new ClientId(UUID.randomUUID());
    tenantId = new TenantId(1);
    clientEntity = mock(ClientEntity.class);
    client = mock(Client.class);
  }

  @Test
  void findByIdAndVisibility() {
    when(jpaClientRepository.findByIdAndVisibility(anyString(), anyBoolean()))
        .thenReturn(Optional.of(clientEntity));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var result = adapter.findByIdAndVisibility(clientId, ClientVisibility.PUBLIC);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
    verify(jpaClientRepository).findByIdAndVisibility(clientId.getValue().toString(), true);
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findById() {
    when(jpaClientRepository.findById(anyString())).thenReturn(Optional.of(clientEntity));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var result = adapter.findById(clientId);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
    verify(jpaClientRepository).findById(clientId.getValue().toString());
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findAllPublicAndPrivateByTenantId() {
    var page = new PageImpl<>(new ArrayList<>(Set.of(clientEntity)));

    when(jpaClientRepository.findAllPublicAndPrivateByTenant(anyInt(), any(PageRequest.class)))
        .thenReturn(page);
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var response = adapter.findAllPublicAndPrivateByTenantId(tenantId, 0, 10);

    assertTrue(response.getData().iterator().hasNext());
    assertEquals(response.getData().iterator().next(), client);
    verify(jpaClientRepository)
        .findAllPublicAndPrivateByTenant(eq(tenantId.getValue()), any(PageRequest.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findAllByTenantId() {
    var page = new PageImpl<>(new ArrayList<>(Set.of(clientEntity)));

    when(jpaClientRepository.findAllByTenantId(anyInt(), any(PageRequest.class))).thenReturn(page);
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var response = adapter.findAllByTenantId(tenantId, 0, 10);

    assertTrue(response.getData().iterator().hasNext());
    assertEquals(response.getData().iterator().next(), client);
    verify(jpaClientRepository).findAllByTenantId(eq(tenantId.getValue()), any(PageRequest.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }

  @Test
  void findByClientIdAndTenantId() {
    when(jpaClientRepository.findClientByClientIdAndTenantId(anyString(), anyInt()))
        .thenReturn(Optional.of(clientEntity));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);

    var result = adapter.findByClientIdAndTenantId(clientId, tenantId);

    assertTrue(result.isPresent());
    assertEquals(client, result.get());
    verify(jpaClientRepository)
        .findClientByClientIdAndTenantId(clientId.getValue().toString(), tenantId.getValue());
    verify(clientDataAccessMapper).toDomain(clientEntity);
  }
}
