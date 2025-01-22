// (c) Copyright Ascensio System SIA 2009-2025
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

import static org.junit.jupiter.api.Assertions.*;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.exception.ClientNotFoundException;
import com.asc.registration.core.domain.value.ClientCreationInfo;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientTenantInfo;
import com.asc.registration.service.mapper.ClientDataMapper;
import com.asc.registration.service.ports.output.repository.ClientQueryRepository;
import com.asc.registration.service.transfer.request.fetch.ClientInfoPaginationQuery;
import com.asc.registration.service.transfer.request.fetch.ClientInfoQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientQuery;
import com.asc.registration.service.transfer.request.fetch.TenantClientsPaginationQuery;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.PageableResponse;
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

public class ClientQueryHandlerTest {
  @InjectMocks private ClientQueryHandler clientQueryHandler;
  @Mock private EncryptionService encryptionService;
  @Mock private ClientQueryRepository clientQueryRepository;
  @Mock private ClientDataMapper clientDataMapper;

  private Client client;
  private ClientResponse clientResponse;
  private ClientInfoResponse clientInfoResponse;

  @BeforeEach
  public void setUp() {
    MockitoAnnotations.openMocks(this);

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
                    .createdBy("creator")
                    .createdOn(ZonedDateTime.now(ZoneId.of("UTC")))
                    .build())
            .clientVisibility(ClientVisibility.PUBLIC)
            .build();
    clientResponse =
        ClientResponse.builder()
            .clientId(client.getId().getValue().toString())
            .clientSecret(client.getSecret().value())
            .build();
    clientInfoResponse =
        ClientInfoResponse.builder().clientId(clientResponse.getClientId()).build();
  }

  @Test
  public void whenClientIsFoundByIdAndTenantId_thenReturnClientResponse() {
    var query = new TenantClientQuery();
    query.setClientId(clientResponse.getClientId());
    query.setTenantId(1);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.of(client));
    when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);
    when(encryptionService.decrypt(anyString())).thenReturn("decryptedSecret");

    var response = clientQueryHandler.getClient(query);

    verify(clientQueryRepository, times(1))
        .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
    verify(clientDataMapper, times(1)).toClientResponse(any(Client.class));
    verify(encryptionService, times(1)).decrypt(anyString());

    assertEquals("decryptedSecret", response.getClientSecret());
  }

  @Test
  public void whenClientIsNotFoundByIdAndTenantId_thenThrowClientNotFoundException() {
    var query = new TenantClientQuery();
    query.setClientId(clientResponse.getClientId());
    query.setTenantId(1);

    when(clientQueryRepository.findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());

    assertThrows(ClientNotFoundException.class, () -> clientQueryHandler.getClient(query));
  }

  @Test
  public void whenClientInfoIsFoundById_thenReturnClientInfoResponse() {
    var query = new ClientInfoQuery();
    query.setClientId(clientResponse.getClientId());

    when(clientQueryRepository.findById(any(ClientId.class))).thenReturn(Optional.of(client));
    when(clientDataMapper.toClientInfoResponse(any(Client.class))).thenReturn(clientInfoResponse);

    var response = clientQueryHandler.getClientInfo(query);

    verify(clientQueryRepository, times(1)).findById(any(ClientId.class));
    verify(clientDataMapper, times(1)).toClientInfoResponse(any(Client.class));

    assertEquals(clientInfoResponse.getClientId(), response.getClientId());
  }

  @Test
  public void whenClientInfoIsNotFoundById_thenThrowClientNotFoundException() {
    var query = new ClientInfoQuery();
    query.setClientId(clientResponse.getClientId());

    when(clientQueryRepository.findById(any(ClientId.class))).thenReturn(Optional.empty());

    assertThrows(ClientNotFoundException.class, () -> clientQueryHandler.getClientInfo(query));
  }

  @Test
  public void whenClientInfoPaginationQueryIsExecuted_thenReturnPageableResponse() {
    var query = new ClientInfoPaginationQuery();
    query.setTenantId(1);
    query.setLimit(10);
    query.setLastClientId("last-client-id");
    query.setLastCreatedOn(ZonedDateTime.now(ZoneId.of("UTC")));

    var pageableResponse = new PageableResponse<Client>();
    pageableResponse.setLimit(10);
    pageableResponse.setData(Set.of(client));
    pageableResponse.setLastClientId("next-client-id");
    pageableResponse.setLastCreatedOn(ZonedDateTime.now(ZoneId.of("UTC")).plusMinutes(10));

    when(clientQueryRepository.findAllPublicAndPrivateByTenantId(
            any(TenantId.class), eq(10), eq("last-client-id"), any(ZonedDateTime.class)))
        .thenReturn(pageableResponse);

    when(clientDataMapper.toClientInfoResponse(any(Client.class))).thenReturn(clientInfoResponse);

    var response = clientQueryHandler.getClientsInfo(query);

    verify(clientQueryRepository, times(1))
        .findAllPublicAndPrivateByTenantId(
            any(TenantId.class), eq(10), eq("last-client-id"), any(ZonedDateTime.class));
    verify(clientDataMapper, times(1)).toClientInfoResponse(any(Client.class));

    assertTrue(response.getData().iterator().hasNext());
    assertEquals(
        clientInfoResponse.getClientId(), response.getData().iterator().next().getClientId());
    assertEquals("next-client-id", response.getLastClientId());
  }

  @Test
  public void whenTenantClientsPaginationQueryIsExecuted_thenReturnPageableResponse() {
    var query = new TenantClientsPaginationQuery();
    query.setTenantId(1);
    query.setLimit(10);
    query.setLastClientId("last-client-id");
    query.setLastCreatedOn(ZonedDateTime.now(ZoneId.of("UTC")));

    var pageableResponse = new PageableResponse<Client>();
    pageableResponse.setLimit(10);
    pageableResponse.setData(Set.of(client));
    pageableResponse.setLastClientId("next-client-id");
    pageableResponse.setLastCreatedOn(ZonedDateTime.now(ZoneId.of("UTC")).plusMinutes(10));

    when(clientQueryRepository.findAllByTenantId(
            any(TenantId.class), eq(10), eq("last-client-id"), any(ZonedDateTime.class)))
        .thenReturn(pageableResponse);

    when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);

    var response = clientQueryHandler.getClients(query);

    verify(clientQueryRepository, times(1))
        .findAllByTenantId(
            any(TenantId.class), eq(10), eq("last-client-id"), any(ZonedDateTime.class));
    verify(clientDataMapper, times(1)).toClientResponse(any(Client.class));

    assertTrue(response.getData().iterator().hasNext());
    assertEquals("next-client-id", response.getLastClientId());
  }
}
