package com.asc.authorization.application.mapper;

import com.asc.authorization.application.configuration.security.AscOAuth2RegisteredClientConfiguration;
import com.asc.common.data.client.entity.ClientEntity;
import java.time.Duration;
import java.util.Arrays;
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
        .clientIdIssuedAt(client.getClientIssuedAt().toInstant())
        .clientSecret(client.getClientSecret())
        .clientName(client.getName())
        .clientAuthenticationMethods(
            methods -> {
              for (String method : client.getAuthenticationMethod().split(",")) {
                methods.add(new ClientAuthenticationMethod(method));
              }
            })
        .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
        .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
        .redirectUris(
            uris ->
                uris.addAll(
                    Arrays.stream(client.getRedirectUris().split(",")).collect(Collectors.toSet())))
        .scopes(
            scopes ->
                scopes.addAll(
                    Arrays.stream(client.getScopes().split(",")).collect(Collectors.toSet())))
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
