package com.asc.registration.data.client.adapter;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.data.client.mapper.ClientDataAccessMapper;
import java.time.ZonedDateTime;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.mockito.ArgumentCaptor;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

class ClientCommandRepositoryDomainAdapterTest {
  @InjectMocks private ClientCommandRepositoryDomainAdapter clientCommandRepositoryDomainAdapter;
  @Mock private JpaClientRepository jpaClientRepository;
  @Mock private ClientDataAccessMapper clientDataAccessMapper;

  private Client client;
  private ClientId clientId;
  private TenantId tenantId;

  @BeforeEach
  void setUp() {
    MockitoAnnotations.openMocks(this);

    client = mock(Client.class);
    clientId = new ClientId(UUID.randomUUID());
    tenantId = new TenantId(1);

    when(client.getId()).thenReturn(clientId);
    when(clientDataAccessMapper.toEntity(client)).thenReturn(mock(ClientEntity.class));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);
  }

  @Test
  void saveClient() {
    ClientEntity clientEntity = mock(ClientEntity.class);
    when(jpaClientRepository.save(any(ClientEntity.class))).thenReturn(clientEntity);

    Client savedClient = clientCommandRepositoryDomainAdapter.saveClient(client);

    verify(clientDataAccessMapper).toEntity(client);
    verify(jpaClientRepository).save(any(ClientEntity.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);

    assertEquals(client, savedClient);
  }

  @Test
  void regenerateClientSecretByClientId() {
    ArgumentCaptor<String> secretCaptor = ArgumentCaptor.forClass(String.class);
    String newSecret =
        clientCommandRepositoryDomainAdapter.regenerateClientSecretByClientId(tenantId, clientId);

    verify(jpaClientRepository)
        .regenerateClientSecretByClientId(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            secretCaptor.capture(),
            any(ZonedDateTime.class));

    assertEquals(newSecret, secretCaptor.getValue());
  }

  @Test
  void changeActivation() {
    clientCommandRepositoryDomainAdapter.changeActivation(tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeActivation(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
  }

  @Test
  void deleteByTenantAndClientId() {
    when(jpaClientRepository.deleteByClientIdAndTenant(anyString(), anyInt())).thenReturn(1);

    int result = clientCommandRepositoryDomainAdapter.deleteByTenantAndClientId(tenantId, clientId);

    verify(jpaClientRepository)
        .deleteByClientIdAndTenant(eq(clientId.getValue().toString()), eq(tenantId.getValue()));

    assertEquals(1, result);
  }
}
