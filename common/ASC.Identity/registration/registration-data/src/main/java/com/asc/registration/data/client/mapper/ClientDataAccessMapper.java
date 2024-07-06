// (c) Copyright Ascensio System SIA 2009-2024
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
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.scope.entity.ScopeEntity;
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
        .invalidated(client.getStatus().equals(ClientStatus.INVALIDATED))
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
                entity.getRedirectUris(),
                entity.getAllowedOrigins(),
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
        .clientVisibility(
            entity.isAccessible() ? ClientVisibility.PUBLIC : ClientVisibility.PRIVATE)
        .build();
  }
}
