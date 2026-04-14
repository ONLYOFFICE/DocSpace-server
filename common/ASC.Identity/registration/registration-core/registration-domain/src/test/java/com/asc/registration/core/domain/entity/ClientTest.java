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

package com.asc.registration.core.domain.entity;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.core.domain.value.*;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;
import java.util.function.Consumer;
import java.util.stream.Stream;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.Arguments;
import org.junit.jupiter.params.provider.EnumSource;
import org.junit.jupiter.params.provider.MethodSource;

class ClientTest {
  private final UserId CREATOR_ID = new UserId("creator");
  private final UserId MODIFIER_ID = new UserId("modifier");
  private Client client;

  @BeforeEach
  void setUp() {
    client =
        Client.Builder.builder()
            .id(new ClientId(UUID.randomUUID()))
            .secret(new ClientSecret(UUID.randomUUID().toString()))
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
  }

  static Stream<Arguments> scopeMutationCases() {
    return Stream.of(
        Arguments.of(
            (Consumer<Client>) client -> client.addScope("delete", new UserId("modifier")),
            "delete",
            "missing-scope"),
        Arguments.of(
            (Consumer<Client>) client -> client.addScope("admin", new UserId("modifier")),
            "admin",
            "missing-scope"),
        Arguments.of(
            (Consumer<Client>) client -> client.removeScope("read", new UserId("modifier")),
            "write",
            "read"),
        Arguments.of(
            (Consumer<Client>) client -> client.removeScope("write", new UserId("modifier")),
            "read",
            "write"));
  }

  static Stream<Arguments> authenticationMutationCases() {
    return Stream.of(
        Arguments.of(
            (Consumer<Client>)
                client ->
                    client.addAuthenticationMethod(
                        AuthenticationMethod.PKCE_AUTHENTICATION, new UserId("modifier")),
            AuthenticationMethod.PKCE_AUTHENTICATION,
            2),
        Arguments.of(
            (Consumer<Client>)
                client -> {
                  client.addAuthenticationMethod(
                      AuthenticationMethod.PKCE_AUTHENTICATION, new UserId("modifier"));
                  client.removeAuthenticationMethod(
                      AuthenticationMethod.DEFAULT_AUTHENTICATION, new UserId("modifier"));
                },
            AuthenticationMethod.PKCE_AUTHENTICATION,
            1),
        Arguments.of(
            (Consumer<Client>)
                client -> {
                  client.addAuthenticationMethod(
                      AuthenticationMethod.PKCE_AUTHENTICATION, new UserId("modifier"));
                  client.removeAuthenticationMethod(
                      AuthenticationMethod.PKCE_AUTHENTICATION, new UserId("modifier"));
                },
            AuthenticationMethod.DEFAULT_AUTHENTICATION,
            1));
  }

  @Test
  void whenInitialized_thenClientIsEnabledAndFieldsAreSet() {
    client.initialize(CREATOR_ID);

    assertNotNull(client.getId());
    assertNotNull(client.getSecret());
    assertEquals(ClientStatus.ENABLED, client.getStatus());
    assertNotNull(client.getClientCreationInfo());
    assertEquals(CREATOR_ID, client.getClientCreationInfo().getCreatedBy());
    assertNotNull(client.getClientCreationInfo().getCreatedOn());
  }

  @Test
  void whenEnabledAfterBeingDisabled_thenClientStatusIsEnabled() {
    client.initialize(CREATOR_ID);
    client.disable(MODIFIER_ID);
    client.enable(MODIFIER_ID);

    assertEquals(ClientStatus.ENABLED, client.getStatus());
  }

  @Test
  void whenDisabled_thenClientStatusIsDisabled() {
    client.initialize(CREATOR_ID);
    client.disable(MODIFIER_ID);

    assertEquals(ClientStatus.DISABLED, client.getStatus());
  }

  @Test
  void whenSecretIsRegenerated_thenOldSecretIsReplaced() {
    client.initialize(CREATOR_ID);
    client.disable(MODIFIER_ID);
    String oldSecret = client.getSecret().value();
    client.regenerateSecret(MODIFIER_ID);

    assertNotEquals(oldSecret, client.getSecret().value());
  }

  @ParameterizedTest
  @MethodSource("scopeMutationCases")
  void whenScopeIsMutated_thenExpectedScopesArePresent(
      Consumer<Client> mutation, String presentScope, String absentScope) {
    client.initialize(CREATOR_ID);
    mutation.accept(client);

    assertTrue(client.getScopes().contains(presentScope));
    assertFalse(client.getScopes().contains(absentScope));
  }

  @Test
  void whenLastScopeIsRemoved_thenExceptionIsThrown() {
    client.initialize(CREATOR_ID);
    client.removeScope("read", MODIFIER_ID);

    ClientDomainException exception =
        assertThrows(
            ClientDomainException.class,
            () -> {
              client.removeScope("write", MODIFIER_ID);
            });

    assertEquals("Client must have at least one scope", exception.getMessage());
  }

  @ParameterizedTest
  @EnumSource(
      value = ClientVisibility.class,
      names = {"PUBLIC", "PRIVATE"})
  void whenVisibilityIsChanged_thenVisibilityIsUpdated(ClientVisibility visibility) {
    client.initialize(CREATOR_ID);
    client.changeVisibility(visibility, MODIFIER_ID);

    assertEquals(visibility, client.getVisibility());
  }

  @Test
  void whenClientInfoIsUpdated_thenClientInfoIsReplaced() {
    client.initialize(CREATOR_ID);
    var newClientInfo = new ClientInfo("Updated Client", "Updated Description", "Updated Logo URL");
    client.updateClientInfo(newClientInfo, MODIFIER_ID);

    assertEquals(newClientInfo, client.getClientInfo());
  }

  @Test
  void whenClientWebsiteInfoIsUpdated_thenWebsiteInfoIsReplaced() {
    client.initialize(CREATOR_ID);
    var newClientWebsiteInfo =
        ClientWebsiteInfo.Builder.builder()
            .websiteUrl("http://updated.url")
            .termsUrl("http://updated.url/terms")
            .policyUrl("http://updated.url/policy")
            .build();
    client.updateClientWebsiteInfo(newClientWebsiteInfo, MODIFIER_ID);

    assertEquals(newClientWebsiteInfo, client.getClientWebsiteInfo());
  }

  @Test
  void whenClientRedirectInfoIsUpdated_thenRedirectInfoIsReplaced() {
    client.initialize(CREATOR_ID);
    var newClientRedirectInfo =
        new ClientRedirectInfo(
            Set.of("http://updated.redirect.url"),
            Set.of("http://updated.allowed.origin"),
            Set.of("http://updated.logout.url"));
    client.updateClientRedirectInfo(newClientRedirectInfo, MODIFIER_ID);

    assertEquals(newClientRedirectInfo, client.getClientRedirectInfo());
  }

  @ParameterizedTest
  @MethodSource("authenticationMutationCases")
  void whenAuthenticationMethodIsMutated_thenExpectedMethodsArePresent(
      Consumer<Client> mutation, AuthenticationMethod presentMethod, int expectedMethodsCount) {
    client.initialize(CREATOR_ID);
    mutation.accept(client);

    assertTrue(client.getAuthenticationMethods().contains(presentMethod));
    assertEquals(expectedMethodsCount, client.getAuthenticationMethods().size());
  }

  @Test
  void whenLastAuthenticationMethodIsRemoved_thenExceptionIsThrown() {
    client.initialize(CREATOR_ID);

    var exception =
        assertThrows(
            ClientDomainException.class,
            () -> {
              client.removeAuthenticationMethod(
                  AuthenticationMethod.DEFAULT_AUTHENTICATION, MODIFIER_ID);
            });

    assertEquals(
        "Client must have at least one authentication method. Cannot remove the last one",
        exception.getMessage());
  }
}
