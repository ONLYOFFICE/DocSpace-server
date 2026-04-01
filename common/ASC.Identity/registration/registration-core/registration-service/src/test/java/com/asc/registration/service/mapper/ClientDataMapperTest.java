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

package com.asc.registration.service.mapper;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.UserId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.value.*;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.UUID;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.CsvSource;

class ClientDataMapperTest {
  private ClientDataMapper clientDataMapper;

  @BeforeEach
  void setUp() {
    clientDataMapper = new ClientDataMapper();
  }

  private static CreateTenantClientCommand createFullCommand() {
    return CreateTenantClientCommand.builder()
        .name("Test Client")
        .description("Test Description")
        .logo("Test Logo")
        .websiteUrl("http://test.com")
        .termsUrl("http://test.com/terms")
        .policyUrl("http://test.com/policy")
        .redirectUris(Set.of("http://test.com/redirect"))
        .allowedOrigins(Set.of("http://test.com"))
        .logoutRedirectUri("http://test.com/logout")
        .tenantId(1)
        .scopes(Set.of("read", "write"))
        .isPublic(true)
        .allowPkce(true)
        .build();
  }

  private Client createClient() {
    return Client.Builder.builder()
        .id(new ClientId(UUID.randomUUID()))
        .secret(new ClientSecret(UUID.randomUUID().toString()))
        .authenticationMethods(Set.of(AuthenticationMethod.DEFAULT_AUTHENTICATION))
        .scopes(Set.of("read", "write"))
        .clientInfo(new ClientInfo("Test Client", "Test Description", "Test Logo"))
        .clientTenantInfo(new ClientTenantInfo(new TenantId(1L)))
        .clientWebsiteInfo(
            ClientWebsiteInfo.Builder.builder()
                .websiteUrl("http://test.com")
                .termsUrl("http://test.com/terms")
                .policyUrl("http://test.com/policy")
                .build())
        .clientRedirectInfo(
            new ClientRedirectInfo(
                Set.of("http://test.com/redirect"),
                Set.of("http://test.com"),
                Set.of("http://test.com/logout")))
        .clientCreationInfo(
            ClientCreationInfo.Builder.builder()
                .createdOn(ZonedDateTime.now())
                .createdBy(new UserId("creator"))
                .build())
        .clientModificationInfo(
            ClientModificationInfo.Builder.builder()
                .modifiedOn(ZonedDateTime.now())
                .modifiedBy(new UserId("modifier"))
                .build())
        .clientStatus(ClientStatus.ENABLED)
        .clientVisibility(ClientVisibility.PUBLIC)
        .build();
  }

  @ParameterizedTest
  @CsvSource({
    "name",
    "description",
    "logo",
    "websiteUrl",
    "termsUrl",
    "policyUrl",
    "redirectUris",
    "allowedOrigins",
    "logoutRedirectUris",
    "tenantId",
    "scopes",
    "visibility"
  })
  void whenCommandIsMappedToDomain_thenFieldMatches(String field) {
    var command = createFullCommand();
    var client = clientDataMapper.toDomain(command);

    assertNotNull(client);
    switch (field) {
      case "name" -> assertEquals(command.getName(), client.getClientInfo().name());
      case "description" ->
          assertEquals(command.getDescription(), client.getClientInfo().description());
      case "logo" -> assertEquals(command.getLogo(), client.getClientInfo().logo());
      case "websiteUrl" ->
          assertEquals(command.getWebsiteUrl(), client.getClientWebsiteInfo().getWebsiteUrl());
      case "termsUrl" ->
          assertEquals(command.getTermsUrl(), client.getClientWebsiteInfo().getTermsUrl());
      case "policyUrl" ->
          assertEquals(command.getPolicyUrl(), client.getClientWebsiteInfo().getPolicyUrl());
      case "redirectUris" ->
          assertEquals(command.getRedirectUris(), client.getClientRedirectInfo().redirectUris());
      case "allowedOrigins" ->
          assertEquals(
              command.getAllowedOrigins(), client.getClientRedirectInfo().allowedOrigins());
      case "logoutRedirectUris" ->
          assertEquals(
              Set.of(command.getLogoutRedirectUri()),
              client.getClientRedirectInfo().logoutRedirectUris());
      case "tenantId" ->
          assertEquals(
              new TenantId(command.getTenantId()), client.getClientTenantInfo().tenantId());
      case "scopes" -> assertEquals(command.getScopes(), client.getScopes());
      case "visibility" -> assertEquals(ClientVisibility.PUBLIC, client.getVisibility());
      default -> throw new IllegalArgumentException("Unknown field: " + field);
    }
  }

  @ParameterizedTest
  @CsvSource({"client_secret_post", "none"})
  void whenCommandIsMappedToDomain_thenAuthenticationMethodIsPresent(String method) {
    var command = createFullCommand();
    var client = clientDataMapper.toDomain(command);

    assertTrue(client.getAuthenticationMethods().contains(AuthenticationMethod.fromMethod(method)));
  }

  @ParameterizedTest
  @CsvSource({
    "name",
    "clientId",
    "description",
    "websiteUrl",
    "termsUrl",
    "policyUrl",
    "logo",
    "authenticationMethodsSize",
    "scopes",
    "createdOn",
    "createdBy",
    "modifiedOn",
    "modifiedBy",
    "isPublic"
  })
  void whenDomainClientIsMapped_thenSharedResponseFieldsMatch(String field) {
    var client = createClient();
    var fullResponse = clientDataMapper.toClientResponse(client);
    var infoResponse = clientDataMapper.toClientInfoResponse(client);

    assertNotNull(fullResponse);
    assertNotNull(infoResponse);

    switch (field) {
      case "name" -> {
        assertEquals(client.getClientInfo().name(), fullResponse.getName());
        assertEquals(client.getClientInfo().name(), infoResponse.getName());
      }
      case "clientId" -> {
        assertEquals(client.getId().getValue().toString(), fullResponse.getClientId());
        assertEquals(client.getId().getValue().toString(), infoResponse.getClientId());
      }
      case "description" -> {
        assertEquals(client.getClientInfo().description(), fullResponse.getDescription());
        assertEquals(client.getClientInfo().description(), infoResponse.getDescription());
      }
      case "websiteUrl" -> {
        assertEquals(client.getClientWebsiteInfo().getWebsiteUrl(), fullResponse.getWebsiteUrl());
        assertEquals(client.getClientWebsiteInfo().getWebsiteUrl(), infoResponse.getWebsiteUrl());
      }
      case "termsUrl" -> {
        assertEquals(client.getClientWebsiteInfo().getTermsUrl(), fullResponse.getTermsUrl());
        assertEquals(client.getClientWebsiteInfo().getTermsUrl(), infoResponse.getTermsUrl());
      }
      case "policyUrl" -> {
        assertEquals(client.getClientWebsiteInfo().getPolicyUrl(), fullResponse.getPolicyUrl());
        assertEquals(client.getClientWebsiteInfo().getPolicyUrl(), infoResponse.getPolicyUrl());
      }
      case "logo" -> {
        assertEquals(client.getClientInfo().logo(), fullResponse.getLogo());
        assertEquals(client.getClientInfo().logo(), infoResponse.getLogo());
      }
      case "authenticationMethodsSize" -> {
        assertEquals(
            client.getAuthenticationMethods().size(),
            fullResponse.getAuthenticationMethods().size());
        assertEquals(
            client.getAuthenticationMethods().size(),
            infoResponse.getAuthenticationMethods().size());
      }
      case "scopes" -> {
        assertEquals(client.getScopes(), fullResponse.getScopes());
        assertEquals(client.getScopes(), infoResponse.getScopes());
      }
      case "createdOn" -> {
        assertEquals(client.getClientCreationInfo().getCreatedOn(), fullResponse.getCreatedOn());
        assertEquals(client.getClientCreationInfo().getCreatedOn(), infoResponse.getCreatedOn());
      }
      case "createdBy" -> {
        assertEquals(
            client.getClientCreationInfo().getCreatedBy().getValue(), fullResponse.getCreatedBy());
        assertEquals(
            client.getClientCreationInfo().getCreatedBy().getValue(), infoResponse.getCreatedBy());
      }
      case "modifiedOn" -> {
        assertEquals(
            client.getClientModificationInfo().getModifiedOn(), fullResponse.getModifiedOn());
        assertEquals(
            client.getClientModificationInfo().getModifiedOn(), infoResponse.getModifiedOn());
      }
      case "modifiedBy" -> {
        assertEquals(
            client.getClientModificationInfo().getModifiedBy().getValue(),
            fullResponse.getModifiedBy());
        assertEquals(
            client.getClientModificationInfo().getModifiedBy().getValue(),
            infoResponse.getModifiedBy());
      }
      case "isPublic" -> {
        assertEquals(
            client.getVisibility().equals(ClientVisibility.PUBLIC), fullResponse.isPublic());
        assertEquals(
            client.getVisibility().equals(ClientVisibility.PUBLIC), infoResponse.isPublic());
      }
      default -> throw new IllegalArgumentException("Unknown field: " + field);
    }
  }

  @ParameterizedTest
  @CsvSource({
    "clientSecret",
    "tenant",
    "redirectUris",
    "allowedOrigins",
    "logoutRedirectUri",
    "enabled"
  })
  void whenDomainClientIsMappedToClientResponse_thenExclusiveFieldsMatch(String field) {
    var client = createClient();
    var response = clientDataMapper.toClientResponse(client);

    assertNotNull(response);
    switch (field) {
      case "clientSecret" -> assertEquals(client.getSecret().value(), response.getClientSecret());
      case "tenant" ->
          assertEquals(client.getClientTenantInfo().tenantId().getValue(), response.getTenant());
      case "redirectUris" ->
          assertEquals(client.getClientRedirectInfo().redirectUris(), response.getRedirectUris());
      case "allowedOrigins" ->
          assertEquals(
              client.getClientRedirectInfo().allowedOrigins(), response.getAllowedOrigins());
      case "logoutRedirectUri" ->
          assertEquals(
              client.getClientRedirectInfo().logoutRedirectUris(), response.getLogoutRedirectUri());
      case "enabled" ->
          assertEquals(client.getStatus().equals(ClientStatus.ENABLED), response.isEnabled());
      default -> throw new IllegalArgumentException("Unknown field: " + field);
    }
  }

  @Test
  void whenDomainClientIsMappedToClientSecret_thenSecretResponseIsCreated() {
    var client = createClient();
    var response = clientDataMapper.toClientSecret(client);

    assertNotNull(response);
    assertEquals(client.getSecret().value(), response.getClientSecret());
  }
}
