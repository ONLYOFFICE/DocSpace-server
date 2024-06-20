package com.asc.registration.service.mapper;

import static org.junit.jupiter.api.Assertions.*;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
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

class ClientDataMapperTest {
  private ClientDataMapper clientDataMapper;

  @BeforeEach
  void setUp() {
    clientDataMapper = new ClientDataMapper();
  }

  @Test
  void testToDomain() {
    var command =
        CreateTenantClientCommand.builder()
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
    var client = clientDataMapper.toDomain(command);

    assertNotNull(client);
    assertEquals(command.getName(), client.getClientInfo().name());
    assertEquals(command.getDescription(), client.getClientInfo().description());
    assertEquals(command.getLogo(), client.getClientInfo().logo());
    assertEquals(command.getWebsiteUrl(), client.getClientWebsiteInfo().getWebsiteUrl());
    assertEquals(command.getTermsUrl(), client.getClientWebsiteInfo().getTermsUrl());
    assertEquals(command.getPolicyUrl(), client.getClientWebsiteInfo().getPolicyUrl());
    assertEquals(command.getRedirectUris(), client.getClientRedirectInfo().redirectUris());
    assertEquals(command.getAllowedOrigins(), client.getClientRedirectInfo().allowedOrigins());
    assertEquals(
        Set.of(command.getLogoutRedirectUri()),
        client.getClientRedirectInfo().logoutRedirectUris());
    assertEquals(new TenantId(command.getTenantId()), client.getClientTenantInfo().tenantId());
    assertEquals(command.getScopes(), client.getScopes());
    assertEquals(ClientVisibility.PUBLIC, client.getVisibility());
    assertTrue(
        client.getAuthenticationMethods().contains(AuthenticationMethod.DEFAULT_AUTHENTICATION));
    assertTrue(
        client.getAuthenticationMethods().contains(AuthenticationMethod.PKCE_AUTHENTICATION));
  }

  @Test
  void testToClientResponse() {
    var client = createClient();
    var response = clientDataMapper.toClientResponse(client);

    assertNotNull(response);
    assertEquals(client.getClientInfo().name(), response.getName());
    assertEquals(client.getId().getValue().toString(), response.getClientId());
    assertEquals(client.getSecret().value(), response.getClientSecret());
    assertEquals(client.getClientInfo().description(), response.getDescription());
    assertEquals(client.getClientWebsiteInfo().getWebsiteUrl(), response.getWebsiteUrl());
    assertEquals(client.getClientWebsiteInfo().getTermsUrl(), response.getTermsUrl());
    assertEquals(client.getClientWebsiteInfo().getPolicyUrl(), response.getPolicyUrl());
    assertEquals(client.getClientInfo().logo(), response.getLogo());
    assertEquals(
        client.getAuthenticationMethods().size(), response.getAuthenticationMethods().size());
    assertEquals(client.getClientTenantInfo().tenantId().getValue(), response.getTenant());
    assertEquals(client.getClientRedirectInfo().redirectUris(), response.getRedirectUris());
    assertEquals(client.getClientRedirectInfo().allowedOrigins(), response.getAllowedOrigins());
    assertEquals(
        client.getClientRedirectInfo().logoutRedirectUris(), response.getLogoutRedirectUri());
    assertEquals(client.getScopes(), response.getScopes());
    assertEquals(client.getClientCreationInfo().getCreatedOn(), response.getCreatedOn());
    assertEquals(client.getClientCreationInfo().getCreatedBy(), response.getCreatedBy());
    assertEquals(client.getClientModificationInfo().getModifiedOn(), response.getModifiedOn());
    assertEquals(client.getClientModificationInfo().getModifiedBy(), response.getModifiedBy());
    assertEquals(client.getStatus().equals(ClientStatus.ENABLED), response.isEnabled());
    assertEquals(client.getVisibility().equals(ClientVisibility.PUBLIC), response.isPublic());
    assertEquals(client.getStatus().equals(ClientStatus.INVALIDATED), response.isInvalidated());
  }

  @Test
  void testToClientSecret() {
    var client = createClient();
    var response = clientDataMapper.toClientSecret(client);

    assertNotNull(response);
    assertEquals(client.getSecret().value(), response.getClientSecret());
  }

  @Test
  void testToClientInfoResponse() {
    var client = createClient();
    var response = clientDataMapper.toClientInfoResponse(client);

    assertNotNull(response);
    assertEquals(client.getClientInfo().name(), response.getName());
    assertEquals(client.getId().getValue().toString(), response.getClientId());
    assertEquals(client.getClientInfo().description(), response.getDescription());
    assertEquals(client.getClientWebsiteInfo().getWebsiteUrl(), response.getWebsiteUrl());
    assertEquals(client.getClientWebsiteInfo().getTermsUrl(), response.getTermsUrl());
    assertEquals(client.getClientWebsiteInfo().getPolicyUrl(), response.getPolicyUrl());
    assertEquals(client.getClientInfo().logo(), response.getLogo());
    assertEquals(
        client.getAuthenticationMethods().size(), response.getAuthenticationMethods().size());
    assertEquals(client.getClientRedirectInfo().redirectUris(), response.getRedirectUris());
    assertEquals(client.getClientRedirectInfo().allowedOrigins(), response.getAllowedOrigins());
    assertEquals(
        client.getClientRedirectInfo().logoutRedirectUris(), response.getLogoutRedirectUri());
    assertEquals(client.getScopes(), response.getScopes());
    assertEquals(client.getClientCreationInfo().getCreatedOn(), response.getCreatedOn());
    assertEquals(client.getClientCreationInfo().getCreatedBy(), response.getCreatedBy());
    assertEquals(client.getClientModificationInfo().getModifiedOn(), response.getModifiedOn());
    assertEquals(client.getClientModificationInfo().getModifiedBy(), response.getModifiedBy());
  }

  private Client createClient() {
    return Client.Builder.builder()
        .id(new ClientId(UUID.randomUUID()))
        .secret(new ClientSecret(UUID.randomUUID().toString()))
        .authenticationMethods(Set.of(AuthenticationMethod.DEFAULT_AUTHENTICATION))
        .scopes(Set.of("read", "write"))
        .clientInfo(new ClientInfo("Test Client", "Test Description", "Test Logo"))
        .clientTenantInfo(new ClientTenantInfo(new TenantId(1)))
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
                .createdBy("creator")
                .build())
        .clientModificationInfo(
            ClientModificationInfo.Builder.builder()
                .modifiedOn(ZonedDateTime.now())
                .modifiedBy("modifier")
                .build())
        .clientStatus(ClientStatus.ENABLED)
        .clientVisibility(ClientVisibility.PUBLIC)
        .build();
  }
}
