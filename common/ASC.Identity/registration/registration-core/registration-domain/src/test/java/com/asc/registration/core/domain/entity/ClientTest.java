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

package com.asc.registration.core.domain.entity;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.exception.ClientDomainException;
import com.asc.registration.core.domain.value.*;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

class ClientTest {
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
                    .createdBy("creator")
                    .createdOn(ZonedDateTime.now(ZoneId.of("UTC")))
                    .build())
            .clientVisibility(ClientVisibility.PRIVATE)
            .build();
  }

  @Test
  void whenInitialized_thenClientIsEnabledAndFieldsAreSet() {
    client.initialize("creator@example.com");

    assertNotNull(client.getId());
    assertNotNull(client.getSecret());
    assertEquals(ClientStatus.ENABLED, client.getStatus());
    assertNotNull(client.getClientCreationInfo());
    assertEquals("creator@example.com", client.getClientCreationInfo().getCreatedBy());
    assertNotNull(client.getClientCreationInfo().getCreatedOn());
  }

  @Test
  void whenEnabledAfterBeingDisabled_thenClientStatusIsEnabled() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");
    client.enable("modifier@example.com");

    assertEquals(ClientStatus.ENABLED, client.getStatus());
  }

  @Test
  void whenDisabled_thenClientStatusIsDisabled() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");

    assertEquals(ClientStatus.DISABLED, client.getStatus());
  }

  @Test
  void whenSecretIsRegenerated_thenOldSecretIsReplaced() {
    client.initialize("creator@example.com");
    client.disable("modifier@example.com");
    String oldSecret = client.getSecret().value();
    client.regenerateSecret("modifier@example.com");

    assertNotEquals(oldSecret, client.getSecret().value());
  }

  @Test
  void whenScopeIsAdded_thenScopeIsIncluded() {
    client.initialize("creator@example.com");
    String newScope = "delete";
    client.addScope(newScope, "modifier@example.com");

    assertTrue(client.getScopes().contains(newScope));
  }

  @Test
  void whenScopeIsRemoved_thenScopeIsExcluded() {
    client.initialize("creator@example.com");
    String scopeToRemove = "read";
    client.removeScope(scopeToRemove, "modifier@example.com");

    assertFalse(client.getScopes().contains(scopeToRemove));
    assertTrue(client.getScopes().contains("write"));
  }

  @Test
  void whenLastScopeIsRemoved_thenExceptionIsThrown() {
    client.initialize("creator@example.com");
    client.removeScope("read", "modifier@example.com");

    ClientDomainException exception =
        assertThrows(
            ClientDomainException.class,
            () -> {
              client.removeScope("write", "modifier@example.com");
            });

    assertEquals("Client must have at least one scope", exception.getMessage());
  }

  @Test
  void whenVisibilityIsChanged_thenVisibilityIsUpdated() {
    client.initialize("creator@example.com");
    client.changeVisibility(ClientVisibility.PUBLIC, "modifier@example.com");

    assertEquals(ClientVisibility.PUBLIC, client.getVisibility());
  }

  @Test
  void whenClientInfoIsUpdated_thenClientInfoIsReplaced() {
    client.initialize("creator@example.com");
    var newClientInfo = new ClientInfo("Updated Client", "Updated Description", "Updated Logo URL");
    client.updateClientInfo(newClientInfo, "modifier@example.com");

    assertEquals(newClientInfo, client.getClientInfo());
  }

  @Test
  void whenClientWebsiteInfoIsUpdated_thenWebsiteInfoIsReplaced() {
    client.initialize("creator@example.com");
    var newClientWebsiteInfo =
        ClientWebsiteInfo.Builder.builder()
            .websiteUrl("http://updated.url")
            .termsUrl("http://updated.url/terms")
            .policyUrl("http://updated.url/policy")
            .build();
    client.updateClientWebsiteInfo(newClientWebsiteInfo, "modifier@example.com");

    assertEquals(newClientWebsiteInfo, client.getClientWebsiteInfo());
  }

  @Test
  void whenClientRedirectInfoIsUpdated_thenRedirectInfoIsReplaced() {
    client.initialize("creator@example.com");
    var newClientRedirectInfo =
        new ClientRedirectInfo(
            Set.of("http://updated.redirect.url"),
            Set.of("http://updated.allowed.origin"),
            Set.of("http://updated.logout.url"));
    client.updateClientRedirectInfo(newClientRedirectInfo, "modifier@example.com");

    assertEquals(newClientRedirectInfo, client.getClientRedirectInfo());
  }

  @Test
  void whenAuthenticationMethodIsAdded_thenMethodIsIncluded() {
    client.initialize("creator@example.com");
    var newMethod = AuthenticationMethod.PKCE_AUTHENTICATION;
    client.addAuthenticationMethod(newMethod, "modifier@example.com");

    assertTrue(client.getAuthenticationMethods().contains(newMethod));
  }

  @Test
  void whenAuthenticationMethodIsRemoved_thenMethodIsExcluded() {
    client.initialize("creator@example.com");
    var methodToRemove = AuthenticationMethod.DEFAULT_AUTHENTICATION;
    var newMethod = AuthenticationMethod.PKCE_AUTHENTICATION;
    client.addAuthenticationMethod(newMethod, "modifier@example.com");
    client.removeAuthenticationMethod(methodToRemove, "modifier@example.com");

    assertFalse(client.getAuthenticationMethods().contains(methodToRemove));
    assertTrue(client.getAuthenticationMethods().contains(newMethod));
  }

  @Test
  void whenLastAuthenticationMethodIsRemoved_thenExceptionIsThrown() {
    client.initialize("creator@example.com");

    var exception =
        assertThrows(
            ClientDomainException.class,
            () -> {
              client.removeAuthenticationMethod(
                  AuthenticationMethod.DEFAULT_AUTHENTICATION, "modifier@example.com");
            });

    assertEquals(
        "Client must have at least one authentication method. Cannot remove the last one",
        exception.getMessage());
  }
}
