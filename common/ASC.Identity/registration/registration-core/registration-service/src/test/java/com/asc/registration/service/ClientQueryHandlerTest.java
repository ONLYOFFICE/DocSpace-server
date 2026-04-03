// (c) Copyright Ascensio System SIA 2009-2026
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
import com.asc.common.core.domain.value.Role;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
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
import com.asc.registration.service.ports.output.resilience.ClientCacheService;
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
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Nested;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.EnumSource;
import org.junit.jupiter.params.provider.MethodSource;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.MockitoAnnotations;

public class ClientQueryHandlerTest {
  @InjectMocks private ClientQueryHandler clientQueryHandler;
  @Mock private EncryptionService encryptionService;
  @Mock private ClientQueryRepository clientQueryRepository;
  @Mock private ClientCacheService clientCacheService;
  @Mock private ClientDataMapper clientDataMapper;

  private Client client;
  private ClientResponse clientResponse;
  private ClientInfoResponse clientInfoResponse;

  @BeforeEach
  public void setUp() {
    MockitoAnnotations.openMocks(this);

    when(clientCacheService.get(any(ClientId.class), any(TenantId.class)))
        .thenReturn(Optional.empty());
    when(clientCacheService.getAnyTenant(any(ClientId.class))).thenReturn(Optional.empty());

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
                    .createdBy(new UserId("creator"))
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

  @Nested
  @DisplayName("Admin role tests")
  class AdminRoleTests {
    enum AdminSingleFetchKind {
      CLIENT,
      CLIENT_INFO
    }

    enum AdminPaginationKind {
      CLIENT_INFO,
      CLIENT_FULL
    }

    static Stream<Arguments> adminPaginationCases() {
      return Stream.of(
          Arguments.of(AdminPaginationKind.CLIENT_INFO, 5, "last-client-id-5"),
          Arguments.of(AdminPaginationKind.CLIENT_INFO, 10, "last-client-id"),
          Arguments.of(AdminPaginationKind.CLIENT_FULL, 5, "last-client-id-5"),
          Arguments.of(AdminPaginationKind.CLIENT_FULL, 10, "last-client-id"));
    }

    @ParameterizedTest
    @EnumSource(AdminSingleFetchKind.class)
    public void whenClientIsFoundByIdAndTenantId_thenReturnExpectedResponse(
        AdminSingleFetchKind kind) {
      when(clientQueryRepository.findByClientIdAndTenantId(
              any(ClientId.class), any(TenantId.class)))
          .thenReturn(Optional.of(client));

      if (kind == AdminSingleFetchKind.CLIENT) {
        var query = new TenantClientQuery();
        query.setClientId(clientResponse.getClientId());
        query.setTenantId(1);

        when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);
        when(encryptionService.decrypt(anyString())).thenReturn("decryptedSecret");

        var response = clientQueryHandler.getClient(Role.ROLE_ADMIN, query);

        verify(clientQueryRepository, times(1))
            .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
        verify(clientDataMapper, times(1)).toClientResponse(any(Client.class));
        verify(encryptionService, times(1)).decrypt(anyString());

        assertEquals("decryptedSecret", response.getClientSecret());
        return;
      }

      var query = new ClientInfoQuery();
      query.setClientId(clientResponse.getClientId());
      query.setTenantId(1);

      when(clientDataMapper.toClientInfoResponse(any(Client.class))).thenReturn(clientInfoResponse);

      var response = clientQueryHandler.getClientInfo(Role.ROLE_ADMIN, query);

      verify(clientQueryRepository, times(1))
          .findByClientIdAndTenantId(any(ClientId.class), any(TenantId.class));
      verify(clientDataMapper, times(1)).toClientInfoResponse(any(Client.class));

      assertEquals(clientInfoResponse.getClientId(), response.getClientId());
    }

    @ParameterizedTest
    @MethodSource("adminPaginationCases")
    public void whenPaginationQueryIsExecuted_thenReturnPageableResponse(
        AdminPaginationKind kind, int limit, String lastClientId) {
      var lastCreatedOn = ZonedDateTime.now(ZoneId.of("UTC"));
      var pageableResponse = new PageableResponse<Client>();
      pageableResponse.setLimit(limit);
      pageableResponse.setData(Set.of(client));
      pageableResponse.setLastClientId("next-client-id");
      pageableResponse.setLastCreatedOn(lastCreatedOn.plusMinutes(10));

      when(clientQueryRepository.findAllByTenantId(
              any(TenantId.class), eq(limit), eq(lastClientId), any(ZonedDateTime.class)))
          .thenReturn(pageableResponse);

      if (kind == AdminPaginationKind.CLIENT_INFO) {
        var query = new ClientInfoPaginationQuery();
        query.setTenantId(1);
        query.setLimit(limit);
        query.setLastClientId(lastClientId);
        query.setLastCreatedOn(lastCreatedOn);

        when(clientDataMapper.toClientInfoResponse(any(Client.class)))
            .thenReturn(clientInfoResponse);

        var response = clientQueryHandler.getClientsInfo(Role.ROLE_ADMIN, query);

        verify(clientQueryRepository, times(1))
            .findAllByTenantId(
                any(TenantId.class), eq(limit), eq(lastClientId), any(ZonedDateTime.class));
        verify(clientDataMapper, times(1)).toClientInfoResponse(any(Client.class));

        assertTrue(response.getData().iterator().hasNext());
        assertEquals(
            clientInfoResponse.getClientId(), response.getData().iterator().next().getClientId());
        assertEquals("next-client-id", response.getLastClientId());
        return;
      }

      var query = new TenantClientsPaginationQuery();
      query.setTenantId(1);
      query.setLimit(limit);
      query.setLastClientId(lastClientId);
      query.setLastCreatedOn(lastCreatedOn);

      when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);

      var response = clientQueryHandler.getClients(Role.ROLE_ADMIN, query);

      verify(clientQueryRepository, times(1))
          .findAllByTenantId(
              any(TenantId.class), eq(limit), eq(lastClientId), any(ZonedDateTime.class));
      verify(clientDataMapper, times(1)).toClientResponse(any(Client.class));

      assertTrue(response.getData().iterator().hasNext());
      assertEquals("next-client-id", response.getLastClientId());
    }

    @Test
    public void whenAdminClientTenantMismatch_thenThrowClientNotFoundException() {
      var query = new TenantClientQuery();
      query.setClientId(clientResponse.getClientId());
      query.setTenantId(2);

      when(clientQueryRepository.findByClientIdAndTenantId(
              any(ClientId.class), eq(new TenantId(1L))))
          .thenReturn(Optional.of(client));
      when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);

      assertThrows(
          ClientNotFoundException.class,
          () -> clientQueryHandler.getClient(Role.ROLE_ADMIN, query));
    }
  }

  @Nested
  @DisplayName("User role tests")
  class UserRoleTests {
    enum UserSingleFetchKind {
      CLIENT,
      CLIENT_INFO
    }

    static Stream<Arguments> userNotFoundCases() {
      return Stream.of(
          Arguments.of(UserSingleFetchKind.CLIENT), Arguments.of(UserSingleFetchKind.CLIENT_INFO));
    }

    static Stream<Arguments> userPaginationCases() {
      return Stream.of(Arguments.of(5, "last-client-id"), Arguments.of(3, "last-client-id-3"));
    }

    @ParameterizedTest
    @EnumSource(UserSingleFetchKind.class)
    public void whenUserClientIsFound_thenReturnExpectedResponse(UserSingleFetchKind kind) {
      when(clientQueryRepository.findByClientIdAndTenantIdAndCreatorId(
              any(ClientId.class), any(TenantId.class), any(UserId.class)))
          .thenReturn(Optional.of(client));

      if (kind == UserSingleFetchKind.CLIENT) {
        var query = new TenantClientQuery();
        query.setClientId(clientResponse.getClientId());
        query.setTenantId(1);
        query.setUserId("creator");

        when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);
        when(encryptionService.decrypt(anyString())).thenReturn("decryptedSecret");

        var response = clientQueryHandler.getClient(Role.ROLE_USER, query);

        verify(clientQueryRepository, times(1))
            .findByClientIdAndTenantIdAndCreatorId(
                any(ClientId.class), any(TenantId.class), any(UserId.class));
        assertEquals("decryptedSecret", response.getClientSecret());
        return;
      }

      var query = new ClientInfoQuery();
      query.setClientId(clientResponse.getClientId());
      query.setTenantId(1);
      query.setUserId("creator");

      when(clientDataMapper.toClientInfoResponse(any(Client.class))).thenReturn(clientInfoResponse);

      var response = clientQueryHandler.getClientInfo(Role.ROLE_USER, query);

      verify(clientQueryRepository, times(1))
          .findByClientIdAndTenantIdAndCreatorId(
              any(ClientId.class), any(TenantId.class), any(UserId.class));
      assertEquals(clientInfoResponse.getClientId(), response.getClientId());
    }

    @ParameterizedTest
    @MethodSource("userNotFoundCases")
    public void whenUserClientLookupReturnsEmpty_thenThrowClientNotFoundException(
        UserSingleFetchKind kind) {
      when(clientQueryRepository.findByClientIdAndTenantIdAndCreatorId(
              any(ClientId.class), any(TenantId.class), any(UserId.class)))
          .thenReturn(Optional.empty());

      if (kind == UserSingleFetchKind.CLIENT) {
        var query = new TenantClientQuery();
        query.setClientId(clientResponse.getClientId());
        query.setTenantId(1);
        query.setUserId("nonCreator");

        assertThrows(
            ClientNotFoundException.class,
            () -> clientQueryHandler.getClient(Role.ROLE_USER, query));
        return;
      }

      var query = new ClientInfoQuery();
      query.setClientId(clientResponse.getClientId());
      query.setTenantId(1);
      query.setUserId("nonCreator");

      assertThrows(
          ClientNotFoundException.class,
          () -> clientQueryHandler.getClientInfo(Role.ROLE_USER, query));
    }

    @Test
    public void whenUserClientInfoAccessRestricted_thenThrowClientNotFoundException() {
      var query = new ClientInfoQuery();
      query.setClientId(clientResponse.getClientId());
      query.setTenantId(2);
      query.setUserId("newCreator");

      var restrictedClient =
          Client.Builder.builder()
              .id(client.getId())
              .secret(client.getSecret())
              .authenticationMethods(client.getAuthenticationMethods())
              .scopes(client.getScopes())
              .clientInfo(client.getClientInfo())
              .clientTenantInfo(new ClientTenantInfo(new TenantId(1L)))
              .clientRedirectInfo(client.getClientRedirectInfo())
              .clientCreationInfo(client.getClientCreationInfo())
              .clientVisibility(ClientVisibility.PUBLIC)
              .build();

      when(clientQueryRepository.findByClientIdAndTenantIdAndCreatorId(
              any(ClientId.class), any(TenantId.class), eq(new UserId("creator"))))
          .thenReturn(Optional.of(restrictedClient));

      assertThrows(
          ClientNotFoundException.class,
          () -> clientQueryHandler.getClientInfo(Role.ROLE_USER, query));
    }

    @ParameterizedTest
    @MethodSource("userPaginationCases")
    public void whenUserClientsPaginationQuery_thenUseCreatorId(int limit, String lastClientId) {
      var query = new ClientInfoPaginationQuery();
      query.setTenantId(1);
      query.setUserId("creator");
      query.setLimit(limit);
      query.setLastClientId(lastClientId);
      query.setLastCreatedOn(ZonedDateTime.now(ZoneId.of("UTC")));

      var pageableResponse = new PageableResponse<Client>();
      pageableResponse.setLimit(limit);
      pageableResponse.setData(Set.of(client));
      pageableResponse.setLastClientId("next-client-id");
      pageableResponse.setLastCreatedOn(ZonedDateTime.now(ZoneId.of("UTC")).plusMinutes(10));

      when(clientQueryRepository.findAllByTenantIdAndCreatorId(
              any(TenantId.class),
              any(UserId.class),
              eq(limit),
              eq(lastClientId),
              any(ZonedDateTime.class)))
          .thenReturn(pageableResponse);
      when(clientDataMapper.toClientInfoResponse(any(Client.class))).thenReturn(clientInfoResponse);

      var response = clientQueryHandler.getClientsInfo(Role.ROLE_USER, query);

      verify(clientQueryRepository, times(1))
          .findAllByTenantIdAndCreatorId(
              any(TenantId.class),
              any(UserId.class),
              eq(limit),
              eq(lastClientId),
              any(ZonedDateTime.class));
      assertEquals("next-client-id", response.getLastClientId());
    }

    @Test
    public void whenUserClientTenantMismatch_thenThrowClientNotFoundException() {
      var query = new TenantClientQuery();
      query.setClientId(clientResponse.getClientId());
      query.setTenantId(2);
      query.setUserId("creator");

      when(clientQueryRepository.findByClientIdAndTenantIdAndCreatorId(
              any(ClientId.class), eq(new TenantId(1L)), any(UserId.class)))
          .thenReturn(Optional.of(client));
      when(clientDataMapper.toClientResponse(any(Client.class))).thenReturn(clientResponse);

      assertThrows(
          ClientNotFoundException.class, () -> clientQueryHandler.getClient(Role.ROLE_USER, query));
    }
  }
}
