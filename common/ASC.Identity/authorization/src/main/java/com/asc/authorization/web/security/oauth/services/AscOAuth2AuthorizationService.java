/**
 *
 */
package com.asc.authorization.web.security.oauth.services;

import com.asc.authorization.web.server.messaging.AuthorizationMessage;
import com.asc.authorization.web.server.utilities.AuthorizationMapper;
import com.asc.authorization.web.server.utilities.ClientMapper;
import com.asc.authorization.configuration.RabbitMQConfiguration;
import com.asc.authorization.core.entities.Authorization;
import com.asc.authorization.core.usecases.repositories.AuthorizationPersistenceQueryUsecases;
import com.asc.authorization.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.asc.authorization.core.usecases.service.authorization.AuthorizationCreationUsecases;
import com.asc.authorization.core.usecases.service.authorization.AuthorizationRetrieveUsecases;
import com.asc.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import com.asc.authorization.extensions.runnables.FunctionalRunnable;
import com.asc.authorization.web.security.crypto.cipher.Cipher;
import com.asc.authorization.web.server.caching.DistributedCacheMap;
import jakarta.servlet.http.Cookie;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.security.oauth2.core.OAuth2AccessToken;
import org.springframework.security.oauth2.core.OAuth2RefreshToken;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationCode;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
@Transactional(readOnly = true, timeout = 2000)
public class AscOAuth2AuthorizationService implements OAuth2AuthorizationService, AuthorizationRetrieveUsecases,
        AuthorizationCreationUsecases, AuthorizationCleanupUsecases {
    private final String AUTHORIZATION_QUEUE = "authorization";
    private final String CLIENT_STATE_COOKIE = "client_state";

    private final RabbitMQConfiguration configuration;
    private final AuthorizationMapper authorizationMapper;
    private final ClientMapper clientMapper;

    private final AuthorizationPersistenceQueryUsecases authorizationPersistenceQueryUsecases;
    private final ClientRetrieveUsecases clientRetrieveUsecases;

    private final Cipher cipher;
    private final AmqpTemplate amqpTemplate;
    private final DistributedCacheMap<String, AuthorizationMessage> cache;


    /**
     *
     * @param authorization the {@link OAuth2Authorization}
     */
    public void save(OAuth2Authorization authorization) {
        MDC.put("id", authorization.getId());
        log.info("Trying to save authorization");

        String state = authorization.getAttribute(OAuth2ParameterNames.STATE);
        OAuth2Authorization.Token<OAuth2AuthorizationCode> authorizationCode =
                authorization.getToken(OAuth2AuthorizationCode.class);
        OAuth2Authorization.Token<OAuth2AccessToken> accessToken =
                authorization.getToken(OAuth2AccessToken.class);
        OAuth2Authorization.Token<OAuth2RefreshToken> refreshToken =
                authorization.getToken(OAuth2RefreshToken.class);

        log.info("Adding authorization to the cache");

        var msg = authorizationMapper.toMessage(authorization);
        cache.put(authorization.getId(), msg);
        if (state != null && !state.isBlank()) {
            MDC.put("state", state);
            log.debug("Adding authorization with state to the cache");
            MDC.clear();

            cache.put(state, msg);
        } else if (authorizationCode != null && authorizationCode.getToken() != null) {
            MDC.put("code", authorizationCode.getToken().getTokenValue());
            log.debug("Adding authorization with code to the cache");
            MDC.clear();

            cache.put(authorizationCode.getToken().getTokenValue(), msg);
        } else if (accessToken != null && accessToken.getToken() != null) {
            MDC.put("accessToken", accessToken.getToken().getTokenValue());
            log.debug("Adding authorization with access token to the cache");
            MDC.clear();

            cache.put(accessToken.getToken().getTokenValue(), msg);
        } else if (refreshToken != null && refreshToken.getToken() != null) {
            MDC.put("refreshToken", refreshToken.getToken().getTokenValue());
            log.debug("Adding authorization with refresh token to the cache");
            MDC.clear();

            cache.put(refreshToken.getToken().getTokenValue(), msg);
        }

        if (msg.getState() != null && !msg.getState().isBlank()) {
            log.info("Setting authorization state cookie");
            Cookie cookie = new Cookie(CLIENT_STATE_COOKIE, msg.getState());
            cookie.setPath("/");
            cookie.setMaxAge(60 * 60 * 24 * 365 * 10);
            ((ServletRequestAttributes) RequestContextHolder
                    .getRequestAttributes()).getResponse()
                    .addCookie(cookie);
        }


        try {
            CompletableFuture.allOf(CompletableFuture.runAsync(FunctionalRunnable
                    .builder().action(cipher::encrypt)
                    .extractor(msg::getAuthorizationCodeValue)
                    .setter(msg::setAuthorizationCodeValue).build()),
                    CompletableFuture.runAsync(FunctionalRunnable.builder()
                            .action(cipher::encrypt).extractor(msg::getAccessTokenValue)
                            .setter(msg::setAccessTokenValue).build()),
                    CompletableFuture.runAsync(FunctionalRunnable.builder()
                            .action(cipher::encrypt).extractor(msg::getRefreshTokenValue)
                            .setter(msg::setRefreshTokenValue).build()))
                            .get(2, TimeUnit.SECONDS);
        } catch (ExecutionException | TimeoutException | InterruptedException e) {
            log.warn("Could not execute encryption tasks", e);
            return;
        }

        amqpTemplate.convertAndSend(
                configuration.getQueues().get(AUTHORIZATION_QUEUE).getExchange(),
                configuration.getQueues().get(AUTHORIZATION_QUEUE).getRouting(),
                msg);
    }

    /**
     *
     * @param authorization the {@link OAuth2Authorization}
     */
    public void remove(OAuth2Authorization authorization) {
        MDC.put("id", authorization.getId());
        log.info("Trying to remove authorization by id");

        String state = authorization.getAttribute(OAuth2ParameterNames.STATE);
        OAuth2Authorization.Token<OAuth2AuthorizationCode> authorizationCode =
                authorization.getToken(OAuth2AuthorizationCode.class);
        OAuth2Authorization.Token<OAuth2AccessToken> accessToken =
                authorization.getToken(OAuth2AccessToken.class);
        OAuth2Authorization.Token<OAuth2RefreshToken> refreshToken =
                authorization.getToken(OAuth2RefreshToken.class);

        log.info("Removing authorization from the cache");

        cache.delete(authorization.getId());
        if (state != null && !state.isBlank()) {
            MDC.put("state", state);
            log.debug("Removing authorization with state from the cache");
            MDC.clear();

            cache.delete(state);
        } else if (authorizationCode != null && authorizationCode.getToken() != null) {
            MDC.put("code", authorizationCode.getToken().getTokenValue());
            log.debug("Removing authorization with code from the cache");
            MDC.clear();

            cache.delete(authorizationCode.getToken().getTokenValue());
        } else if (accessToken != null && accessToken.getToken() != null) {
            MDC.put("accessToken", accessToken.getToken().getTokenValue());
            log.debug("Removing authorization with access token from the cache");
            MDC.clear();

            cache.delete(accessToken.getToken().getTokenValue());
        } else if (refreshToken != null && refreshToken.getToken() != null) {
            MDC.put("refreshToken", refreshToken.getToken().getTokenValue());
            log.debug("Removing authorization with refresh token from the cache");
            MDC.clear();

            cache.delete(refreshToken.getToken().getTokenValue());
        }

        var msg = authorizationMapper.toMessage(authorization);
        msg.setAccessTokenValue("***");
        msg.setRefreshTokenValue("***");
        msg.setInvalidated(true);

        amqpTemplate.convertSendAndReceive(
                configuration.getQueues().get(AUTHORIZATION_QUEUE).getExchange(),
                configuration.getQueues().get(AUTHORIZATION_QUEUE).getRouting(),
                msg);
    }

    /**
     *
     * @param id the authorization identifier
     * @return
     */
    public OAuth2Authorization findById(String id) {
        MDC.put("id", id);
        log.info("Trying to find authorization by id");

        var authorization = cache.get(id);
        if (authorization != null) {
            log.debug("Found authorization in the cache");

            cache.delete(authorization.getId());

            log.debug("Removing authorization from the cache");
            MDC.clear();

            var client = clientRetrieveUsecases.getClientByClientId(authorization
                    .getRegisteredClientId());
            return authorizationMapper.fromMessage(authorization, clientMapper
                    .toRegisteredClient(client));
        }

        MDC.clear();

        var msg = authorizationPersistenceQueryUsecases.getById(id);
        if (msg == null)
            return null;

        MDC.put("id", id);
        log.info("Found authorization in the database");

        try {
            var clientFuture = CompletableFuture.supplyAsync(() -> clientRetrieveUsecases
                    .getClientByClientId(msg.getRegisteredClientId()));
            CompletableFuture.allOf(
                    CompletableFuture.runAsync(FunctionalRunnable
                            .builder()
                            .action(cipher::decrypt)
                            .extractor(msg::getAuthorizationCodeValue)
                            .setter(msg::setAuthorizationCodeValue)
                            .build()),
                    CompletableFuture.runAsync(FunctionalRunnable
                            .builder()
                            .action(cipher::decrypt)
                            .extractor(msg::getAccessTokenValue)
                            .setter(msg::setAccessTokenValue)
                            .build()),
                    CompletableFuture.runAsync(FunctionalRunnable
                            .builder()
                            .action(cipher::decrypt)
                            .extractor(msg::getRefreshTokenValue)
                            .setter(msg::setRefreshTokenValue)
                            .build()), clientFuture).get(2, TimeUnit.SECONDS);
            return authorizationMapper.fromEntity(msg, clientMapper.toRegisteredClient(clientFuture.get()));
        } catch (ExecutionException | InterruptedException | TimeoutException e) {
            log.warn("Could not execute decryption tasks", e);
            return null;
        } finally {
            MDC.clear();
        }
    }

    /**
     *
     * @param token the token credential
     * @param tokenType the {@link OAuth2TokenType token type}
     * @return
     */
    public OAuth2Authorization findByToken(String token, OAuth2TokenType tokenType) {
        MDC.put("token", token);
        log.debug("Trying to find authorization by token");

        var authorization = cache.get(token);
        if (authorization != null) {
            log.debug("Found authorization in the cache");

            cache.delete(authorization.getId());

            log.debug("Removing authorization from the cache");
            MDC.clear();

            var client = clientRetrieveUsecases.getClientByClientId(authorization
                    .getRegisteredClientId());
            return authorizationMapper.fromMessage(authorization, clientMapper.toRegisteredClient(client));
        }

        Authorization result;
        if (tokenType == null) {
            log.debug("Trying to find authorization by any value");

            result = authorizationPersistenceQueryUsecases.getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(token);
        } else if (OAuth2ParameterNames.STATE.equals(tokenType.getValue())) {
            MDC.put("state", token);
            log.debug("Trying to find authorization by state");
            MDC.clear();

            result = authorizationPersistenceQueryUsecases.getByState(token);
        } else if (OAuth2ParameterNames.CODE.equals(tokenType.getValue())) {
            MDC.put("authorizationCode", token);
            log.debug("Trying to find authorization by authorization code");
            MDC.clear();

            result = authorizationPersistenceQueryUsecases.getByAuthorizationCodeValue(token);
        } else if (OAuth2ParameterNames.ACCESS_TOKEN.equals(tokenType.getValue())) {
            MDC.put("accessToken", token);
            log.debug("Trying to find authorization by access token");
            MDC.clear();

            result = authorizationPersistenceQueryUsecases.getByAccessTokenValue(token);
        } else if (OAuth2ParameterNames.REFRESH_TOKEN.equals(tokenType.getValue())) {
            MDC.put("refreshToken", token);
            log.debug("Trying to find authorization by refresh token");
            MDC.clear();

            result = authorizationPersistenceQueryUsecases.getByRefreshTokenValue(token);
        } else {
            log.debug("Empty authorization");
            return null;
        }

        if (result == null)
            return null;

        try {
            var clientFuture = CompletableFuture.supplyAsync(() -> clientRetrieveUsecases
                    .getClientByClientId(result.getRegisteredClientId()));
            CompletableFuture.allOf(
                            CompletableFuture.runAsync(FunctionalRunnable
                                    .builder()
                                    .action(cipher::decrypt)
                                    .extractor(result::getAuthorizationCodeValue)
                                    .setter(result::setAuthorizationCodeValue)
                                    .build()),
                            CompletableFuture.runAsync(FunctionalRunnable
                                    .builder()
                                    .action(cipher::decrypt)
                                    .extractor(result::getAccessTokenValue)
                                    .setter(result::setAccessTokenValue)
                                    .build()),
                            CompletableFuture.runAsync(FunctionalRunnable
                                    .builder()
                                    .action(cipher::decrypt)
                                    .extractor(result::getRefreshTokenValue)
                                    .setter(result::setRefreshTokenValue)
                                    .build()),
                            clientFuture)
                    .get(2, TimeUnit.SECONDS);
            return authorizationMapper.fromEntity(result, clientMapper
                    .toRegisteredClient(clientFuture.get()));
        } catch (ExecutionException | InterruptedException | TimeoutException e) {
            log.warn("Could not execute decryption tasks", e);
            return null;
        } finally {
            MDC.clear();
        }
    }
}
