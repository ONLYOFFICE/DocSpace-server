package com.asc.authorization.application.mapper;

import com.asc.authorization.application.configuration.security.AscOAuth2RegisteredClientConfiguration;
import com.asc.common.core.domain.value.enums.AuthenticationMethod;
import com.asc.common.data.client.entity.ClientEntity;
import com.asc.common.data.scope.entity.ScopeEntity;
import com.asc.common.service.transfer.response.ClientResponse;
import java.time.Duration;
import java.util.HashSet;
import java.util.Set;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.security.oauth2.server.authorization.settings.TokenSettings;
import org.springframework.stereotype.Component;

/** Mapper class for converting between {@link ClientEntity} and {@link RegisteredClient}. */
@Component
@RequiredArgsConstructor
public class ClientMapper {

  private final AscOAuth2RegisteredClientConfiguration configuration;

  /**
   * Converts a {@link ClientEntity} to a {@link RegisteredClient}.
   *
   * @param client the ClientEntity to convert.
   * @return the RegisteredClient.
   */
  public RegisteredClient toRegisteredClient(ClientEntity client) {
    return RegisteredClient.withId(client.getClientId())
        .clientId(client.getClientId())
        .clientIdIssuedAt(client.getCreatedOn().toInstant())
        .clientSecret(client.getClientSecret())
        .clientName(client.getName())
        .clientAuthenticationMethods(
            methods -> {
              var clientAuthenticationMethod =
                  client.getAuthenticationMethods().stream()
                      .map(AuthenticationMethod::getMethod)
                      .collect(Collectors.toSet());
              for (String method : clientAuthenticationMethod) {
                methods.add(new ClientAuthenticationMethod(method));
              }
            })
        .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
        .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
        .redirectUris(uris -> uris.addAll(client.getRedirectUris()))
        .scopes(
            scopes ->
                scopes.addAll(
                    client.getScopes().stream()
                        .map(ScopeEntity::getName)
                        .collect(Collectors.toSet())))
        .clientSettings(
            ClientSettings.builder()
                .requireProofKey(false)
                .requireAuthorizationConsent(true)
                .build())
        .tokenSettings(
            TokenSettings.builder()
                .accessTokenTimeToLive(Duration.ofMinutes(configuration.getAccessTokenMinutesTTL()))
                .refreshTokenTimeToLive(Duration.ofDays(configuration.getRefreshTokenDaysTTL()))
                .authorizationCodeTimeToLive(
                    Duration.ofMinutes(configuration.getAuthorizationCodeMinutesTTL()))
                .reuseRefreshTokens(false)
                .build())
        .build();
  }

  /**
   * Converts a {@link ClientEntity} to a {@link ClientResponse}.
   *
   * @param client the ClientEntity to convert.
   * @return the ClientResponse.
   */
  public ClientResponse toClientResponse(ClientEntity client) {
    if (client == null) throw new IllegalArgumentException("Client cannot be null");

    return ClientResponse.builder()
        .name(client.getName())
        .clientId(client.getClientId())
        .clientSecret(client.getClientSecret())
        .description(client.getDescription())
        .websiteUrl(client.getWebsiteUrl())
        .termsUrl(client.getTermsUrl())
        .policyUrl(client.getPolicyUrl())
        .logo(client.getLogo())
        .authenticationMethods(
            client.getAuthenticationMethods().stream()
                .map(AuthenticationMethod::getMethod)
                .collect(Collectors.toSet()))
        .tenant(client.getTenantId())
        .redirectUris(client.getRedirectUris())
        .allowedOrigins(client.getAllowedOrigins())
        .logoutRedirectUri(Set.of(client.getLogoutRedirectUri()))
        .scopes(client.getScopes().stream().map(ScopeEntity::getName).collect(Collectors.toSet()))
        .createdOn(client.getCreatedOn())
        .createdBy(client.getCreatedBy())
        .modifiedOn(client.getModifiedOn())
        .modifiedBy(client.getModifiedBy())
        .isPublic(client.isAccessible())
        .enabled(client.isEnabled())
        .invalidated(client.isInvalidated())
        .build();
  }

  /**
   * Converts a {@link ClientResponse} to a {@link RegisteredClient}.
   *
   * @param clientResponse the ClientResponse to convert.
   * @return the RegisteredClient.
   */
  public RegisteredClient toRegisteredClient(ClientResponse clientResponse) {
    return RegisteredClient.withId(clientResponse.getClientId())
        .clientId(clientResponse.getClientId())
        .clientIdIssuedAt(clientResponse.getCreatedOn().toInstant())
        .clientSecret(clientResponse.getClientSecret())
        .clientName(clientResponse.getName())
        .clientAuthenticationMethods(
            methods -> {
              var clientAuthenticationMethod =
                  new HashSet<>(clientResponse.getAuthenticationMethods());
              for (String method : clientAuthenticationMethod) {
                methods.add(new ClientAuthenticationMethod(method));
              }
            })
        .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
        .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
        .redirectUris(uris -> uris.addAll(clientResponse.getRedirectUris()))
        .scopes(scopes -> scopes.addAll(clientResponse.getScopes()))
        .clientSettings(
            ClientSettings.builder()
                .requireProofKey(false)
                .requireAuthorizationConsent(true)
                .build())
        .tokenSettings(
            TokenSettings.builder()
                .accessTokenTimeToLive(Duration.ofMinutes(configuration.getAccessTokenMinutesTTL()))
                .refreshTokenTimeToLive(Duration.ofDays(configuration.getRefreshTokenDaysTTL()))
                .authorizationCodeTimeToLive(
                    Duration.ofMinutes(configuration.getAuthorizationCodeMinutesTTL()))
                .reuseRefreshTokens(false)
                .build())
        .build();
  }
}
