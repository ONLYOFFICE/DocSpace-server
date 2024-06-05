package com.asc.registration.data.client.mapper;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.value.*;
import java.util.Arrays;
import java.util.UUID;
import java.util.stream.Collectors;
import org.springframework.stereotype.Component;

/**
 * Mapper class to convert between {@link Client} domain objects and {@link ClientEntity} data
 * access objects.
 */
@Component
public class ClientDataAccessMapper {

  /**
   * Converts a {@link Client} domain object to a {@link ClientEntity}.
   *
   * @param client the domain object to convert
   * @return the converted data access object
   */
  public ClientEntity toEntity(Client client) {
    var modified = client.getClientModificationInfo();
    var websiteInfo = client.getClientWebsiteInfo();
    return ClientEntity.builder()
        .clientId(client.getId().getValue().toString())
        .name(client.getClientInfo().name())
        .description(client.getClientInfo().description())
        .clientSecret(client.getSecret().value())
        .logo(client.getClientInfo().logo())
        .clientIssuedAt(client.getClientCreationInfo().getCreatedOn())
        .authenticationMethod(
            String.join(
                ",",
                client.getAuthenticationMethods().stream()
                    .map(AuthenticationMethod::getMethod)
                    .collect(Collectors.toSet())))
        .tenant(client.getClientTenantInfo().tenantId().getValue())
        .tenantUrl(client.getClientTenantInfo().tenantUrl())
        .websiteUrl(websiteInfo == null ? null : websiteInfo.getWebsiteUrl())
        .termsUrl(websiteInfo == null ? null : websiteInfo.getTermsUrl())
        .policyUrl(websiteInfo == null ? null : websiteInfo.getPolicyUrl())
        .redirectUris(String.join(",", client.getClientRedirectInfo().redirectUris()))
        .allowedOrigins(String.join(",", client.getClientRedirectInfo().allowedOrigins()))
        .logoutRedirectUri(String.join(",", client.getClientRedirectInfo().logoutRedirectUris()))
        .enabled(client.getStatus().equals(ClientStatus.ENABLED))
        .invalidated(client.getStatus().equals(ClientStatus.INVALIDATED))
        .scopes(String.join(",", client.getScopes()))
        .createdOn(client.getClientCreationInfo().getCreatedOn())
        .createdBy(client.getClientCreationInfo().getCreatedBy())
        .modifiedOn(
            modified == null
                ? client.getClientCreationInfo().getCreatedOn()
                : modified.getModifiedOn())
        .modifiedBy(
            modified == null
                ? client.getClientCreationInfo().getCreatedBy()
                : modified.getModifiedBy())
        .build();
  }

  /**
   * Converts a {@link ClientEntity} data access object to a {@link Client} domain object.
   *
   * @param entity the data access object to convert
   * @return the converted domain object
   */
  public Client toDomain(ClientEntity entity) {
    return Client.Builder.builder()
        .id(new ClientId(UUID.fromString(entity.getClientId())))
        .secret(new ClientSecret(entity.getClientSecret()))
        .authenticationMethods(
            Arrays.stream(entity.getAuthenticationMethod().split(","))
                .map(AuthenticationMethod::fromMethod)
                .collect(Collectors.toSet()))
        .scopes(Arrays.stream(entity.getScopes().split(",")).collect(Collectors.toSet()))
        .clientInfo(new ClientInfo(entity.getName(), entity.getDescription(), entity.getLogo()))
        .clientTenantInfo(
            new ClientTenantInfo(new TenantId(entity.getTenant()), entity.getTenantUrl()))
        .clientWebsiteInfo(
            ClientWebsiteInfo.Builder.builder()
                .websiteUrl(entity.getWebsiteUrl())
                .termsUrl(entity.getTermsUrl())
                .policyUrl(entity.getPolicyUrl())
                .build())
        .clientRedirectInfo(
            new ClientRedirectInfo(
                Arrays.stream(entity.getRedirectUris().split(",")).collect(Collectors.toSet()),
                Arrays.stream(entity.getAllowedOrigins().split(",")).collect(Collectors.toSet()),
                Arrays.stream(entity.getLogoutRedirectUri().split(","))
                    .collect(Collectors.toSet())))
        .clientCreationInfo(
            ClientCreationInfo.Builder.builder()
                .createdBy(entity.getCreatedBy())
                .createdOn(entity.getCreatedOn())
                .build())
        .clientModificationInfo(
            ClientModificationInfo.Builder.builder()
                .modifiedBy(entity.getModifiedBy())
                .modifiedOn(entity.getModifiedOn())
                .build())
        .clientStatus(
            entity.isInvalidated()
                ? ClientStatus.INVALIDATED
                : entity.isEnabled() ? ClientStatus.ENABLED : ClientStatus.DISABLED)
        .build();
  }
}
