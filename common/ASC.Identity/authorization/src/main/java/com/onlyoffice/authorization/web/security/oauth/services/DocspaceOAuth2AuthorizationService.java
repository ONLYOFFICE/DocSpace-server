/**
 *
 */
package com.onlyoffice.authorization.web.security.oauth.services;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.Module;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.onlyoffice.authorization.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.core.entities.Authorization;
import com.onlyoffice.authorization.core.usecases.repositories.AuthorizationPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.onlyoffice.authorization.core.usecases.service.authorization.AuthorizationCreationUsecases;
import com.onlyoffice.authorization.core.usecases.service.authorization.AuthorizationRetrieveUsecases;
import com.onlyoffice.authorization.extensions.runnables.FunctionalRunnable;
import com.onlyoffice.authorization.web.security.crypto.aes.Cipher;
import com.onlyoffice.authorization.web.server.caching.DistributedCacheMap;
import com.onlyoffice.authorization.web.server.messaging.AuthorizationMessage;
import jakarta.annotation.PostConstruct;
import jakarta.servlet.http.Cookie;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.security.jackson2.SecurityJackson2Modules;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.OAuth2AccessToken;
import org.springframework.security.oauth2.core.OAuth2RefreshToken;
import org.springframework.security.oauth2.core.OAuth2Token;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationCode;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.jackson2.OAuth2AuthorizationServerJackson2Module;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.StringUtils;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.time.Instant;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Map;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.TimeoutException;
import java.util.function.Consumer;

/**
 *
 */
@Slf4j
@Service
@RequiredArgsConstructor
@Transactional(readOnly = true, timeout = 2000)
public class DocspaceOAuth2AuthorizationService implements OAuth2AuthorizationService, AuthorizationRetrieveUsecases,
        AuthorizationCreationUsecases, AuthorizationCleanupUsecases {
    private final String AUTHORIZATION_QUEUE = "authorization";
    private final String CLIENT_STATE_COOKIE = "client_state";
    private final ObjectMapper objectMapper = new ObjectMapper();

    private final RabbitMQConfiguration configuration;

    private final AuthorizationPersistenceQueryUsecases queryUsecases;

    private final DistributedCacheMap<String, AuthorizationMessage> cache;
    private final AmqpTemplate amqpTemplate;
    private final Cipher cipher;

    @PostConstruct
    public void init() {
        ClassLoader classLoader = DocspaceOAuth2AuthorizationService.class.getClassLoader();
        List<Module> securityModules = SecurityJackson2Modules.getModules(classLoader);
        objectMapper.registerModules(securityModules);
        objectMapper.registerModule(new OAuth2AuthorizationServerJackson2Module());
        objectMapper.registerModule(new JavaTimeModule());
        objectMapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);
    }

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

        var msg = toMessage(authorization);
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

        var msg = toMessage(authorization);
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

            return fromMessage(authorization);
        }

        MDC.clear();

        var msg = queryUsecases.getById(id);
        if (msg == null)
            return null;

        MDC.put("id", id);
        log.info("Found authorization in the database");

        try {
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
                            .build()))
                .get(2, TimeUnit.SECONDS);
        } catch (ExecutionException | InterruptedException | TimeoutException e) {
            log.warn("Could not execute decryption tasks", e);
            return null;
        } finally {
            MDC.clear();
        }

        return toObject(msg);
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

            return fromMessage(authorization);
        }

        Authorization result;
        if (tokenType == null) {
            log.debug("Trying to find authorization by any value");

            result = queryUsecases.getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(token);
        } else if (OAuth2ParameterNames.STATE.equals(tokenType.getValue())) {
            MDC.put("state", token);
            log.debug("Trying to find authorization by state");
            MDC.clear();

            result = queryUsecases.getByState(token);
        } else if (OAuth2ParameterNames.CODE.equals(tokenType.getValue())) {
            MDC.put("authorizationCode", token);
            log.debug("Trying to find authorization by authorization code");
            MDC.clear();

            result = queryUsecases.getByAuthorizationCodeValue(token);
        } else if (OAuth2ParameterNames.ACCESS_TOKEN.equals(tokenType.getValue())) {
            MDC.put("accessToken", token);
            log.debug("Trying to find authorization by access token");
            MDC.clear();

            result = queryUsecases.getByAccessTokenValue(token);
        } else if (OAuth2ParameterNames.REFRESH_TOKEN.equals(tokenType.getValue())) {
            MDC.put("refreshToken", token);
            log.debug("Trying to find authorization by refresh token");
            MDC.clear();

            result = queryUsecases.getByRefreshTokenValue(token);
        } else {
            log.debug("Empty authorization");
            return null;
        }

        if (result == null)
            return null;

        try {
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
                                    .build()))
                    .get(2, TimeUnit.SECONDS);
        } catch (ExecutionException | InterruptedException | TimeoutException e) {
            log.warn("Could not execute decryption tasks", e);
            return null;
        } finally {
            MDC.clear();
        }

        return toObject(result);
    }

    /**
     *
     * @param entity
     * @return
     */
    private OAuth2Authorization toObject(Authorization entity) {
        OAuth2Authorization.Builder builder = OAuth2Authorization
                .withRegisteredClient(RegisteredClient
                        .withId(entity.getRegisteredClientId())
                        .build()
                )
                .id(entity.getId())
                .principalName(entity.getPrincipalName())
                .authorizationGrantType(resolveAuthorizationGrantType(entity.getAuthorizationGrantType()))
                .authorizedScopes(StringUtils.commaDelimitedListToSet(entity.getAuthorizedScopes()))
                .attributes(attributes -> attributes.putAll(parseMap(entity.getAttributes())));
        if (entity.getState() != null) {
            builder.attribute(OAuth2ParameterNames.STATE, entity.getState());
        }

        if (entity.getAuthorizationCodeValue() != null) {
            OAuth2AuthorizationCode authorizationCode = new OAuth2AuthorizationCode(
                    entity.getAuthorizationCodeValue(),
                    entity.getAuthorizationCodeIssuedAt().toInstant(),
                    entity.getAuthorizationCodeExpiresAt().toInstant());
            builder.token(authorizationCode, metadata -> metadata.putAll(parseMap(entity.getAuthorizationCodeMetadata())));
        }

        if (entity.getAccessTokenValue() != null) {
            Instant issuedAt = null;
            Instant expiresAt = null;
            if (entity.getAccessTokenIssuedAt() != null)
                issuedAt = entity.getAccessTokenIssuedAt().toInstant();
            if (entity.getAccessTokenExpiresAt() != null)
                expiresAt = entity.getAccessTokenExpiresAt().toInstant();
            OAuth2AccessToken accessToken = new OAuth2AccessToken(
                    OAuth2AccessToken.TokenType.BEARER,
                    entity.getAccessTokenValue(),
                    issuedAt,
                    expiresAt,
                    StringUtils.commaDelimitedListToSet(entity.getAccessTokenScopes()));
            builder.token(accessToken, metadata -> metadata.putAll(parseMap(entity.getAccessTokenMetadata())));
        }

        if (entity.getRefreshTokenValue() != null) {
            Instant issuedAt = null;
            Instant expiresAt = null;
            if (entity.getRefreshTokenIssuedAt() != null)
                issuedAt = entity.getRefreshTokenIssuedAt().toInstant();
            if (entity.getRefreshTokenExpiresAt() != null)
                expiresAt = entity.getRefreshTokenExpiresAt().toInstant();
            OAuth2RefreshToken refreshToken = new OAuth2RefreshToken(
                    entity.getRefreshTokenValue(),
                    issuedAt,
                    expiresAt);
            builder.token(refreshToken, metadata -> metadata.putAll(parseMap(entity.getRefreshTokenMetadata())));
        }

        return builder.build();
    }

    private OAuth2Authorization fromMessage(AuthorizationMessage message) {
        OAuth2Authorization.Builder builder = OAuth2Authorization
                .withRegisteredClient(RegisteredClient
                        .withId(message.getRegisteredClientId())
                        .build()
                )
                .id(message.getId())
                .principalName(message.getPrincipalName())
                .authorizationGrantType(resolveAuthorizationGrantType(message.getAuthorizationGrantType()))
                .authorizedScopes(StringUtils.commaDelimitedListToSet(message.getAuthorizedScopes()))
                .attributes(attributes -> attributes.putAll(parseMap(message.getAttributes())));
        if (message.getState() != null) {
            builder.attribute(OAuth2ParameterNames.STATE, message.getState());
        }

        if (message.getAuthorizationCodeValue() != null) {
            OAuth2AuthorizationCode authorizationCode = new OAuth2AuthorizationCode(
                    message.getAuthorizationCodeValue(),
                    message.getAuthorizationCodeIssuedAt().toInstant(),
                    message.getAuthorizationCodeExpiresAt().toInstant());
            builder.token(authorizationCode, metadata -> metadata.putAll(parseMap(message.getAuthorizationCodeMetadata())));
        }

        if (message.getAccessTokenValue() != null) {
            Instant issuedAt = null;
            Instant expiresAt = null;
            if (message.getAccessTokenIssuedAt() != null)
                issuedAt = message.getAccessTokenIssuedAt().toInstant();
            if (message.getAccessTokenExpiresAt() != null)
                expiresAt = message.getAccessTokenExpiresAt().toInstant();
            OAuth2AccessToken accessToken = new OAuth2AccessToken(
                    OAuth2AccessToken.TokenType.BEARER,
                    message.getAccessTokenValue(),
                    issuedAt,
                    expiresAt,
                    StringUtils.commaDelimitedListToSet(message.getAccessTokenScopes()));
            builder.token(accessToken, metadata -> metadata.putAll(parseMap(message.getAccessTokenMetadata())));
        }

        if (message.getRefreshTokenValue() != null) {
            Instant issuedAt = null;
            Instant expiresAt = null;
            if (message.getRefreshTokenIssuedAt() != null)
                issuedAt = message.getRefreshTokenIssuedAt().toInstant();
            if (message.getRefreshTokenExpiresAt() != null)
                expiresAt = message.getRefreshTokenExpiresAt().toInstant();
            OAuth2RefreshToken refreshToken = new OAuth2RefreshToken(
                    message.getRefreshTokenValue(),
                    issuedAt,
                    expiresAt);
            builder.token(refreshToken, metadata -> metadata.putAll(parseMap(message.getRefreshTokenMetadata())));
        }

        return builder.build();
    }

    /**
     *
     * @param authorization
     * @return
     */
    private AuthorizationMessage toMessage(OAuth2Authorization authorization) {
        AuthorizationMessage message = AuthorizationMessage
                .builder()
                .id(authorization.getId())
                .registeredClientId(authorization.getRegisteredClientId())
                .principalName(authorization.getPrincipalName())
                .authorizationGrantType(authorization.getAuthorizationGrantType().getValue())
                .authorizedScopes(StringUtils.collectionToDelimitedString(authorization.getAuthorizedScopes(), ","))
                .attributes(writeMap(authorization.getAttributes()))
                .state(authorization.getAttribute(OAuth2ParameterNames.STATE))
                .build();

        OAuth2Authorization.Token<OAuth2AuthorizationCode> authorizationCode =
                authorization.getToken(OAuth2AuthorizationCode.class);
        setTokenValues(
                authorizationCode,
                message::setAuthorizationCodeValue,
                message::setAuthorizationCodeIssuedAt,
                message::setAuthorizationCodeExpiresAt,
                message::setAuthorizationCodeMetadata
        );

        OAuth2Authorization.Token<OAuth2AccessToken> accessToken =
                authorization.getToken(OAuth2AccessToken.class);
        setTokenValues(
                accessToken,
                message::setAccessTokenValue,
                message::setAccessTokenIssuedAt,
                message::setAccessTokenExpiresAt,
                message::setAccessTokenMetadata
        );

        if (accessToken != null && accessToken.getToken().getScopes() != null) {
            message.setAccessTokenScopes(StringUtils.collectionToDelimitedString(accessToken.getToken().getScopes(), ","));
        }

        OAuth2Authorization.Token<OAuth2RefreshToken> refreshToken =
                authorization.getToken(OAuth2RefreshToken.class);

        setTokenValues(
                refreshToken,
                message::setRefreshTokenValue,
                message::setRefreshTokenIssuedAt,
                message::setRefreshTokenExpiresAt,
                message::setRefreshTokenMetadata
        );

        return message;
    }

    /**
     *
     * @param token
     * @param tokenValueConsumer
     * @param issuedAtConsumer
     * @param expiresAtConsumer
     * @param metadataConsumer
     */
    private void setTokenValues(
            OAuth2Authorization.Token<?> token,
            Consumer<String> tokenValueConsumer,
            Consumer<ZonedDateTime> issuedAtConsumer,
            Consumer<ZonedDateTime> expiresAtConsumer,
            Consumer<String> metadataConsumer) {
        if (token != null) {
            OAuth2Token oAuth2Token = token.getToken();
            tokenValueConsumer.accept(oAuth2Token.getTokenValue());
            issuedAtConsumer.accept(ZonedDateTime.ofInstant(oAuth2Token.getIssuedAt(), ZoneId.systemDefault()));
            expiresAtConsumer.accept(ZonedDateTime.ofInstant(oAuth2Token.getExpiresAt(), ZoneId.systemDefault()));
            metadataConsumer.accept(writeMap(token.getMetadata()));
        }
    }

    /**
     *
     * @param data
     * @return
     */
    private Map<String, Object> parseMap(String data) {
        if (data == null || data.isBlank())
            return Map.of();
        try {
            return objectMapper.readValue(data, new TypeReference<Map<String, Object>>() {});
        } catch (Exception ex) {
            throw new IllegalArgumentException(ex.getMessage(), ex);
        }
    }

    /**
     *
     * @param metadata
     * @return
     */
    private String writeMap(Map<String, Object> metadata) {
        try {
            return this.objectMapper.writeValueAsString(metadata);
        } catch (Exception ex) {
            throw new IllegalArgumentException(ex.getMessage(), ex);
        }
    }

    /**
     *
     * @param authorizationGrantType
     * @return
     */
    private static AuthorizationGrantType resolveAuthorizationGrantType(String authorizationGrantType) {
        if (AuthorizationGrantType.AUTHORIZATION_CODE.getValue().equals(authorizationGrantType)) {
            return AuthorizationGrantType.AUTHORIZATION_CODE;
        } else if (AuthorizationGrantType.CLIENT_CREDENTIALS.getValue().equals(authorizationGrantType)) {
            return AuthorizationGrantType.CLIENT_CREDENTIALS;
        } else if (AuthorizationGrantType.REFRESH_TOKEN.getValue().equals(authorizationGrantType)) {
            return AuthorizationGrantType.REFRESH_TOKEN;
        }
        return new AuthorizationGrantType(authorizationGrantType);
    }
}
