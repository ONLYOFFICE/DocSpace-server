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

    clientId = new ClientId(UUID.randomUUID());
    tenantId = new TenantId(1);
    client = mock(Client.class);

    when(client.getId()).thenReturn(clientId);
    when(clientDataAccessMapper.toEntity(any(Client.class))).thenReturn(mock(ClientEntity.class));
    when(clientDataAccessMapper.toDomain(any(ClientEntity.class))).thenReturn(client);
  }

  @Test
  void saveClient() {
    var clientEntity = mock(ClientEntity.class);

    when(jpaClientRepository.save(any(ClientEntity.class))).thenReturn(clientEntity);

    var savedClient = clientCommandRepositoryDomainAdapter.saveClient(client);

    verify(clientDataAccessMapper).toEntity(client);
    verify(jpaClientRepository).save(any(ClientEntity.class));
    verify(clientDataAccessMapper).toDomain(clientEntity);

    assertEquals(client, savedClient);
  }

  @Test
  void regenerateClientSecretByTenantIdAndClientId() {
    var secretCaptor = ArgumentCaptor.forClass(String.class);
    var newSecret =
        clientCommandRepositoryDomainAdapter.regenerateClientSecretByTenantIdAndClientId(
            tenantId, clientId);

    verify(jpaClientRepository)
        .regenerateClientSecretByClientId(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            secretCaptor.capture(),
            any(ZonedDateTime.class));

    assertEquals(newSecret, secretCaptor.getValue());
  }

  @Test
  void changeVisibilityByTenantIdAndClientId() {
    clientCommandRepositoryDomainAdapter.changeVisibilityByTenantIdAndClientId(
        tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeVisibility(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
  }

  @Test
  void changeActivationByTenantIdAndClientId() {
    clientCommandRepositoryDomainAdapter.changeActivationByTenantIdAndClientId(
        tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeActivation(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
  }

  @Test
  void deleteByTenantIdAndClientId() {
    when(jpaClientRepository.deleteByClientIdAndTenantId(anyString(), anyInt())).thenReturn(1);

    var result =
        clientCommandRepositoryDomainAdapter.deleteByTenantIdAndClientId(tenantId, clientId);

    verify(jpaClientRepository)
        .deleteByClientIdAndTenantId(eq(clientId.getValue().toString()), eq(tenantId.getValue()));

    assertEquals(1, result);
  }

  @Test
  void regenerateClientSecretCorrectly() {
    var secret =
        clientCommandRepositoryDomainAdapter.regenerateClientSecretByTenantIdAndClientId(
            tenantId, clientId);

    verify(jpaClientRepository)
        .regenerateClientSecretByClientId(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(secret),
            any(ZonedDateTime.class));
  }

  @Test
  void changeVisibilityCorrectly() {
    clientCommandRepositoryDomainAdapter.changeVisibilityByTenantIdAndClientId(
        tenantId, clientId, true);

    verify(jpaClientRepository)
        .changeVisibility(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(true),
            any(ZonedDateTime.class));
  }

  @Test
  void changeActivationCorrectly() {
    clientCommandRepositoryDomainAdapter.changeActivationByTenantIdAndClientId(
        tenantId, clientId, false);

    verify(jpaClientRepository)
        .changeActivation(
            eq(tenantId.getValue()),
            eq(clientId.getValue().toString()),
            eq(false),
            any(ZonedDateTime.class));
  }

  @Test
  void deleteClient() {
    when(jpaClientRepository.deleteByClientIdAndTenantId(anyString(), anyInt())).thenReturn(0);

    var result =
        clientCommandRepositoryDomainAdapter.deleteByTenantIdAndClientId(tenantId, clientId);

    verify(jpaClientRepository)
        .deleteByClientIdAndTenantId(eq(clientId.getValue().toString()), eq(tenantId.getValue()));

    assertEquals(0, result);
  }

  @Test
  void verifySaveClientInteractions() {
    var clientEntity = mock(ClientEntity.class);

    when(jpaClientRepository.save(any(ClientEntity.class))).thenReturn(clientEntity);

    clientCommandRepositoryDomainAdapter.saveClient(client);

    verify(clientDataAccessMapper, times(1)).toEntity(any(Client.class));
    verify(jpaClientRepository, times(1)).save(any(ClientEntity.class));
    verify(clientDataAccessMapper, times(1)).toDomain(any(ClientEntity.class));
  }
}
