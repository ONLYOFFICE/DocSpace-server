package com.asc.registration.service.mapper;

import com.asc.common.core.domain.value.TenantId;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.core.domain.value.enums.ClientStatus;
import com.asc.common.core.domain.value.enums.ClientVisibility;
import com.asc.common.service.transfer.response.ClientResponse;
import com.asc.registration.core.domain.entity.Client;
import com.asc.registration.core.domain.value.ClientInfo;
import com.asc.registration.core.domain.value.ClientRedirectInfo;
import com.asc.registration.core.domain.value.ClientTenantInfo;
import com.asc.registration.core.domain.value.ClientWebsiteInfo;
import com.asc.registration.service.transfer.request.create.CreateTenantClientCommand;
import com.asc.registration.service.transfer.response.ClientInfoResponse;
import com.asc.registration.service.transfer.response.ClientSecretResponse;
import java.util.Set;
import java.util.stream.Collectors;
import org.springframework.stereotype.Component;

/**
 * ClientDataMapper is responsible for mapping data transfer objects (DTOs) to domain objects and
 * vice versa. This component handles the transformation logic for creating, updating, and
 * retrieving client-related information.
 */
@Component
public class ClientDataMapper {

  /**
   * Maps a CreateTenantClientCommand to a Client domain object.
   *
   * @param command The command containing the client creation details.
   * @return The mapped Client domain object.
   */
  public Client toDomain(CreateTenantClientCommand command) {
    if (command == null)
      throw new IllegalArgumentException("CreateTenantClientCommand cannot be null");

    return Client.Builder.builder()
        .authenticationMethods(
            command.isAllowPkce()
                ? Set.of(
                    AuthenticationMethod.PKCE_AUTHENTICATION,
                    AuthenticationMethod.DEFAULT_AUTHENTICATION)
                : Set.of(AuthenticationMethod.DEFAULT_AUTHENTICATION))
        .clientInfo(new ClientInfo(command.getName(), command.getDescription(), command.getLogo()))
        .clientWebsiteInfo(
            ClientWebsiteInfo.Builder.builder()
                .websiteUrl(command.getWebsiteUrl())
                .policyUrl(command.getPolicyUrl())
                .termsUrl(command.getTermsUrl())
                .build())
        .clientRedirectInfo(
            new ClientRedirectInfo(
                command.getRedirectUris(),
                command.getAllowedOrigins(),
                Set.of(command.getLogoutRedirectUri())))
        .clientTenantInfo(new ClientTenantInfo(new TenantId(command.getTenantId())))
        .scopes(command.getScopes())
        .clientVisibility(command.isPublic() ? ClientVisibility.PUBLIC : ClientVisibility.PRIVATE)
        .build();
  }

  /**
   * Maps a Client domain object to a ClientResponse DTO.
   *
   * @param client The client domain object.
   * @return The mapped ClientResponse DTO.
   */
  public ClientResponse toClientResponse(Client client) {
    if (client == null) throw new IllegalArgumentException("Client cannot be null");

    var modified = client.getClientModificationInfo();
    var websiteInfo = client.getClientWebsiteInfo();
    return ClientResponse.builder()
        .name(client.getClientInfo().name())
        .clientId(client.getId().getValue().toString())
        .clientSecret(client.getSecret().value())
        .description(client.getClientInfo().description())
        .websiteUrl(websiteInfo == null ? null : websiteInfo.getWebsiteUrl())
        .termsUrl(websiteInfo == null ? null : websiteInfo.getTermsUrl())
        .policyUrl(websiteInfo == null ? null : websiteInfo.getPolicyUrl())
        .logo(client.getClientInfo().logo())
        .authenticationMethods(
            client.getAuthenticationMethods().stream()
                .map(AuthenticationMethod::getMethod)
                .collect(Collectors.toSet()))
        .tenant(client.getClientTenantInfo().tenantId().getValue())
        .redirectUris(client.getClientRedirectInfo().redirectUris())
        .allowedOrigins(client.getClientRedirectInfo().allowedOrigins())
        .logoutRedirectUri(client.getClientRedirectInfo().logoutRedirectUris())
        .scopes(client.getScopes())
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
        .isPublic(client.getVisibility().equals(ClientVisibility.PUBLIC))
        .enabled(client.getStatus().equals(ClientStatus.ENABLED))
        .invalidated(client.getStatus().equals(ClientStatus.INVALIDATED))
        .build();
  }

  /**
   * Maps a Client domain object to a ClientSecretResponse DTO.
   *
   * @param client The client domain object.
   * @return The mapped ClientSecretResponse DTO.
   */
  public ClientSecretResponse toClientSecret(Client client) {
    if (client == null) throw new IllegalArgumentException("Client cannot be null");

    return ClientSecretResponse.builder().clientSecret(client.getSecret().value()).build();
  }

  /**
   * Maps a Client domain object to a ClientInfoResponse DTO.
   *
   * @param client The client domain object.
   * @return The mapped ClientInfoResponse DTO.
   */
  public ClientInfoResponse toClientInfoResponse(Client client) {
    if (client == null) throw new IllegalArgumentException("Client cannot be null");

    var modified = client.getClientModificationInfo();
    var websiteInfo = client.getClientWebsiteInfo();
    return ClientInfoResponse.builder()
        .name(client.getClientInfo().name())
        .clientId(client.getId().getValue().toString())
        .description(client.getClientInfo().description())
        .websiteUrl(websiteInfo == null ? null : websiteInfo.getWebsiteUrl())
        .termsUrl(websiteInfo == null ? null : websiteInfo.getTermsUrl())
        .policyUrl(websiteInfo == null ? null : websiteInfo.getPolicyUrl())
        .logo(client.getClientInfo().logo())
        .authenticationMethods(
            client.getAuthenticationMethods().stream()
                .map(AuthenticationMethod::getMethod)
                .collect(Collectors.toSet()))
        .isPublic(client.getVisibility().equals(ClientVisibility.PUBLIC))
        .scopes(client.getScopes())
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
}
