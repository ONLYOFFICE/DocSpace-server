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

package com.asc.registration.data.client.mapper;

import com.asc.common.core.domain.value.ClientId;
import com.asc.common.core.domain.value.ClientSecret;
import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.value.*;
import com.asc.registration.data.client.entity.ClientDynamoEntity;
import com.asc.registration.data.client.entity.ClientEntity;
import com.asc.registration.data.scope.entity.ScopeEntity;
import java.time.ZonedDateTime;
import java.util.Arrays;
import java.util.HashSet;
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
        .authenticationMethods(client.getAuthenticationMethods())
        .tenantId(client.getClientTenantInfo().tenantId().getValue())
        .websiteUrl(websiteInfo == null ? null : websiteInfo.getWebsiteUrl())
        .termsUrl(websiteInfo == null ? null : websiteInfo.getTermsUrl())
        .policyUrl(websiteInfo == null ? null : websiteInfo.getPolicyUrl())
        .redirectUris(client.getClientRedirectInfo().redirectUris())
        .allowedOrigins(client.getClientRedirectInfo().allowedOrigins())
        .logoutRedirectUri(String.join(",", client.getClientRedirectInfo().logoutRedirectUris()))
        .accessible(client.getVisibility().equals(ClientVisibility.PUBLIC))
        .enabled(client.getStatus().equals(ClientStatus.ENABLED))
        .scopes(
            client.getScopes().stream()
                .map(s -> ScopeEntity.builder().name(s).build())
                .collect(Collectors.toSet()))
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
        .version(client.getVersion())
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
        .authenticationMethods(entity.getAuthenticationMethods())
        .scopes(entity.getScopes().stream().map(ScopeEntity::getName).collect(Collectors.toSet()))
        .clientInfo(new ClientInfo(entity.getName(), entity.getDescription(), entity.getLogo()))
        .clientTenantInfo(new ClientTenantInfo(new TenantId(entity.getTenantId())))
        .clientWebsiteInfo(
            ClientWebsiteInfo.Builder.builder()
                .websiteUrl(entity.getWebsiteUrl())
                .termsUrl(entity.getTermsUrl())
                .policyUrl(entity.getPolicyUrl())
                .build())
        .clientRedirectInfo(
            new ClientRedirectInfo(
                new HashSet<>(entity.getRedirectUris()),
                new HashSet<>(entity.getAllowedOrigins()),
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
        .clientStatus(entity.isEnabled() ? ClientStatus.ENABLED : ClientStatus.DISABLED)
        .clientVisibility(
            entity.isAccessible() ? ClientVisibility.PUBLIC : ClientVisibility.PRIVATE)
        .clientVersion(entity.getVersion() != null ? entity.getVersion() : 0)
        .build();
  }

  /**
   * Merges changes from an origin {@link ClientEntity} into a destination {@link ClientEntity}.
   *
   * @param origin the source entity with new values
   * @param destination the existing entity to be updated
   * @return the updated destination entity with merged values
   */
  public ClientEntity merge(ClientEntity origin, ClientEntity destination) {
    destination.setName(origin.getName() != null ? origin.getName() : destination.getName());
    destination.setDescription(
        origin.getDescription() != null ? origin.getDescription() : destination.getDescription());
    destination.setClientSecret(
        origin.getClientSecret() != null
            ? origin.getClientSecret()
            : destination.getClientSecret());
    destination.setLogo(origin.getLogo() != null ? origin.getLogo() : destination.getLogo());
    destination.setAuthenticationMethods(
        origin.getAuthenticationMethods() != null
            ? origin.getAuthenticationMethods()
            : destination.getAuthenticationMethods());
    destination.setTenantId(origin.getTenantId());
    destination.setWebsiteUrl(
        origin.getWebsiteUrl() != null ? origin.getWebsiteUrl() : destination.getWebsiteUrl());
    destination.setTermsUrl(
        origin.getTermsUrl() != null ? origin.getTermsUrl() : destination.getTermsUrl());
    destination.setPolicyUrl(
        origin.getPolicyUrl() != null ? origin.getPolicyUrl() : destination.getPolicyUrl());
    destination.setRedirectUris(
        origin.getRedirectUris() != null
            ? origin.getRedirectUris()
            : destination.getRedirectUris());
    destination.setAllowedOrigins(
        origin.getAllowedOrigins() != null
            ? origin.getAllowedOrigins()
            : destination.getAllowedOrigins());
    destination.setLogoutRedirectUri(
        origin.getLogoutRedirectUri() != null
            ? origin.getLogoutRedirectUri()
            : destination.getLogoutRedirectUri());
    destination.setAccessible(origin.isAccessible());
    destination.setEnabled(origin.isEnabled());
    destination.setInvalidated(origin.isInvalidated());
    if (origin.getScopes() != null && !origin.getScopes().isEmpty()) {
      destination.setScopes(
          origin.getScopes().stream()
              .map(s -> ScopeEntity.builder().name(s.getName()).build())
              .collect(Collectors.toSet()));
    }
    destination.setModifiedOn(
        origin.getModifiedOn() != null ? origin.getModifiedOn() : destination.getModifiedOn());
    destination.setModifiedBy(
        origin.getModifiedBy() != null ? origin.getModifiedBy() : destination.getModifiedBy());
    destination.setVersion(
        origin.getVersion() != null ? origin.getVersion() : destination.getVersion());
    return destination;
  }

  /**
   * Converts a {@link Client} domain object to a {@link ClientDynamoEntity}.
   *
   * @param client the domain object to convert
   * @return the converted DynamoDB entity
   */
  public ClientDynamoEntity toDynamoEntity(Client client) {
    var modified = client.getClientModificationInfo();
    var websiteInfo = client.getClientWebsiteInfo();
    ClientDynamoEntity dynamoEntity = new ClientDynamoEntity();
    dynamoEntity.setClientId(client.getId().getValue().toString());
    dynamoEntity.setName(client.getClientInfo().name());
    dynamoEntity.setDescription(client.getClientInfo().description());
    dynamoEntity.setClientSecret(client.getSecret().value());
    dynamoEntity.setLogo(client.getClientInfo().logo());
    dynamoEntity.setAuthenticationMethods(
        client.getAuthenticationMethods().stream().map(Enum::name).collect(Collectors.toSet()));
    dynamoEntity.setTenantId(client.getClientTenantInfo().tenantId().getValue());
    dynamoEntity.setWebsiteUrl(websiteInfo != null ? websiteInfo.getWebsiteUrl() : null);
    dynamoEntity.setTermsUrl(websiteInfo != null ? websiteInfo.getTermsUrl() : null);
    dynamoEntity.setPolicyUrl(websiteInfo != null ? websiteInfo.getPolicyUrl() : null);
    dynamoEntity.setRedirectUris(client.getClientRedirectInfo().redirectUris());
    dynamoEntity.setAllowedOrigins(client.getClientRedirectInfo().allowedOrigins());
    dynamoEntity.setLogoutRedirectUri(
        String.join(",", client.getClientRedirectInfo().logoutRedirectUris()));
    dynamoEntity.setAccessible(client.getVisibility().equals(ClientVisibility.PUBLIC));
    dynamoEntity.setEnabled(client.getStatus().equals(ClientStatus.ENABLED));
    dynamoEntity.setScopes(client.getScopes());
    dynamoEntity.setCreatedOn(client.getClientCreationInfo().getCreatedOn().toString());
    dynamoEntity.setCreatedBy(client.getClientCreationInfo().getCreatedBy());
    dynamoEntity.setModifiedOn(
        modified != null
            ? modified.getModifiedOn().toString()
            : client.getClientCreationInfo().getCreatedOn().toString());
    dynamoEntity.setModifiedBy(
        modified != null
            ? modified.getModifiedBy()
            : client.getClientCreationInfo().getCreatedBy());
    return dynamoEntity;
  }

  /**
   * Converts a {@link ClientDynamoEntity} DynamoDB entity to a {@link Client} domain object.
   *
   * @param dynamoEntity the DynamoDB entity to convert
   * @return the converted domain object
   */
  public Client toDomain(ClientDynamoEntity dynamoEntity) {
    return Client.Builder.builder()
        .id(new ClientId(UUID.fromString(dynamoEntity.getClientId())))
        .secret(new ClientSecret(dynamoEntity.getClientSecret()))
        .authenticationMethods(
            dynamoEntity.getAuthenticationMethods().stream()
                .map(authMethod -> Enum.valueOf(AuthenticationMethod.class, authMethod))
                .collect(Collectors.toSet()))
        .scopes(dynamoEntity.getScopes())
        .clientInfo(
            new ClientInfo(
                dynamoEntity.getName(), dynamoEntity.getDescription(), dynamoEntity.getLogo()))
        .clientTenantInfo(new ClientTenantInfo(new TenantId(dynamoEntity.getTenantId())))
        .clientWebsiteInfo(
            ClientWebsiteInfo.Builder.builder()
                .websiteUrl(dynamoEntity.getWebsiteUrl())
                .termsUrl(dynamoEntity.getTermsUrl())
                .policyUrl(dynamoEntity.getPolicyUrl())
                .build())
        .clientRedirectInfo(
            new ClientRedirectInfo(
                dynamoEntity.getRedirectUris(),
                dynamoEntity.getAllowedOrigins(),
                Arrays.stream(dynamoEntity.getLogoutRedirectUri().split(","))
                    .collect(Collectors.toSet())))
        .clientCreationInfo(
            ClientCreationInfo.Builder.builder()
                .createdBy(dynamoEntity.getCreatedBy())
                .createdOn(ZonedDateTime.parse(dynamoEntity.getCreatedOn()))
                .build())
        .clientModificationInfo(
            ClientModificationInfo.Builder.builder()
                .modifiedBy(dynamoEntity.getModifiedBy())
                .modifiedOn(ZonedDateTime.parse(dynamoEntity.getModifiedOn()))
                .build())
        .clientStatus(dynamoEntity.isEnabled() ? ClientStatus.ENABLED : ClientStatus.DISABLED)
        .clientVisibility(
            dynamoEntity.isAccessible() ? ClientVisibility.PUBLIC : ClientVisibility.PRIVATE)
        .build();
  }
}
