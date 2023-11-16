/**
 *
 */
package com.onlyoffice.authorization.ports.services;

import com.onlyoffice.authorization.core.entities.Client;
import com.onlyoffice.authorization.core.exceptions.ClientPermissionException;
import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import com.onlyoffice.authorization.security.oauth.configuration.RegisteredClientConfiguration;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.ClientAuthenticationMethod;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.settings.ClientSettings;
import org.springframework.security.oauth2.server.authorization.settings.TokenSettings;
import org.springframework.stereotype.Component;

import java.time.Duration;
import java.util.Arrays;
import java.util.stream.Collectors;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ClientService implements ClientRetrieveUsecases {
    private final String DEFAULT_CLIENT_AUTHENTICATION_METHOD = "client_secret_post";

    private final RegisteredClientConfiguration configuration;
    private final ClientPersistenceQueryUsecases clientUsecases;

    public RegisteredClient getClientById(String id) throws ClientPermissionException {
        MDC.put("client_id", id);
        log.info("Trying to get client by id");
        var client = clientUsecases.getById(id);
        if (!client.isEnabled()) {
            log.info("Client is disabled");
            MDC.clear();
            throw new ClientPermissionException(String
                    .format("client with id %s is disabled", id));
        }
        MDC.clear();
        return toObject(client);
    }

    public RegisteredClient getClientByClientId(String clientId) throws ClientPermissionException {
        MDC.put("client_id", clientId);
        log.info("Trying to get client by client id");
        var client = clientUsecases.getClientByClientId(clientId);
        if (!client.isEnabled()) {
            log.info("Client id disabled");
            MDC.clear();
            throw new ClientPermissionException(String
                    .format("client with client_id %s is disabled", clientId));
        }
        MDC.clear();
        return toObject(client);
    }

    private RegisteredClient toObject(Client client) {
        return RegisteredClient.withId(client.getClientId())
                .clientId(client.getClientId())
                .clientIdIssuedAt(client.getClientIssuedAt().toInstant())
                .clientSecret(client.getClientSecret())
                .clientName(client.getName())
                .clientAuthenticationMethod(client.getAuthenticationMethod()
                        .equals(DEFAULT_CLIENT_AUTHENTICATION_METHOD) ?
                        ClientAuthenticationMethod.CLIENT_SECRET_POST :
                        ClientAuthenticationMethod.CLIENT_SECRET_JWT
                )
                .authorizationGrantType(AuthorizationGrantType.AUTHORIZATION_CODE)
                .authorizationGrantType(AuthorizationGrantType.REFRESH_TOKEN)
                .redirectUris((uris) -> uris.addAll(Arrays.stream(client
                                .getRedirectUris().split(","))
                        .collect(Collectors.toSet())))
                .scopes((scopes) -> scopes.addAll(Arrays.stream(client
                                .getScopes().split(","))
                        .collect(Collectors.toSet()))
                )
                .clientSettings(ClientSettings
                        .builder()
                        .requireProofKey(true)
                        .requireAuthorizationConsent(true)
                        .build()
                )
                .tokenSettings(TokenSettings
                        .builder()
                        .accessTokenTimeToLive(Duration
                                .ofMinutes(configuration.getAccessTokenMinutesTTL())
                        )
                        .refreshTokenTimeToLive(Duration
                                .ofDays(configuration.getRefreshTokenDaysTTL())
                        )
                        .authorizationCodeTimeToLive(Duration
                                .ofMinutes(configuration.getAuthorizationCodeMinutesTTL())
                        )
                        .reuseRefreshTokens(false)
                        .build()
                )
                .build();
    }
}
