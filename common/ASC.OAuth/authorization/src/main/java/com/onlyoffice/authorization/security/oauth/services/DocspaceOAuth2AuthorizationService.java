/**
 *
 */
package com.onlyoffice.authorization.security.oauth.services;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.Module;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.onlyoffice.authorization.core.entities.Authorization;
import com.onlyoffice.authorization.core.transfer.messaging.AuthorizationMessage;
import com.onlyoffice.authorization.core.usecases.repositories.AuthorizationPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.service.authorization.AuthorizationCleanupUsecases;
import com.onlyoffice.authorization.core.usecases.service.authorization.AuthorizationCreationUsecases;
import com.onlyoffice.authorization.core.usecases.service.authorization.AuthorizationRetrieveUsecases;
import com.onlyoffice.authorization.external.caching.hazelcast.AuthorizationCache;
import com.onlyoffice.authorization.external.messaging.configuration.RabbitMQConfiguration;
import com.onlyoffice.authorization.security.crypto.aes.Cipher;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import jakarta.annotation.PostConstruct;
import jakarta.servlet.http.Cookie;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.amqp.core.AmqpTemplate;
import org.springframework.dao.DataRetrievalFailureException;
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
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.security.oauth2.server.authorization.jackson2.OAuth2AuthorizationServerJackson2Module;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.util.Assert;
import org.springframework.util.StringUtils;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.util.Date;
import java.util.List;
import java.util.Map;
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
    private final String CLIENT_STATE_COOKIE = "client_state";

    private final RabbitMQConfiguration configuration;

    private final AuthorizationPersistenceQueryUsecases queryUsecases;
    private final RegisteredClientRepository registeredClientRepository;

    private final AmqpTemplate amqpTemplate;
    private final AuthorizationCache cache;
    private final Cipher cipher;

    private final ObjectMapper objectMapper = new ObjectMapper();

    @PostConstruct
    public void init() {
        ClassLoader classLoader = DocspaceOAuth2AuthorizationService.class.getClassLoader();
        List<Module> securityModules = SecurityJackson2Modules.getModules(classLoader);
        this.objectMapper.registerModules(securityModules);
        this.objectMapper.registerModule(new OAuth2AuthorizationServerJackson2Module());
        this.objectMapper.registerModule(new JavaTimeModule());
    }

    @RateLimiter(name = "mutateRateLimiter")
    @SneakyThrows
    public void save(OAuth2Authorization authorization) {
        Assert.notNull(authorization, "authorization cannot be null");
        MDC.put("id", authorization.getId());
        log.info("trying to save authorization with id");

        String state = authorization.getAttribute(OAuth2ParameterNames.STATE);
        OAuth2Authorization.Token<OAuth2AuthorizationCode> authorizationCode =
                authorization.getToken(OAuth2AuthorizationCode.class);
        OAuth2Authorization.Token<OAuth2AccessToken> accessToken =
                authorization.getToken(OAuth2AccessToken.class);
        OAuth2Authorization.Token<OAuth2RefreshToken> refreshToken =
                authorization.getToken(OAuth2RefreshToken.class);

        cache.put(authorization.getId(), authorization);
        log.info("adding authorization with id to the cache");
        MDC.clear();
        if (state != null && !state.isBlank()) {
            MDC.put("state", state);
            log.info("adding authorization with state to the cache");
            MDC.clear();
            cache.put(state, authorization);
        } else if (authorizationCode != null && authorizationCode.getToken() != null) {
            MDC.put("code", authorizationCode.getToken().getTokenValue());
            log.info("adding authorization with code to the cache");
            MDC.clear();
            cache.put(authorizationCode.getToken().getTokenValue(), authorization);
        } else if (accessToken != null && accessToken.getToken() != null) {
            MDC.put("access_token", accessToken.getToken().getTokenValue());
            log.info("adding authorization with access token to the cache");
            MDC.clear();
            cache.put(accessToken.getToken().getTokenValue(), authorization);
        } else if (refreshToken != null && refreshToken.getToken() != null) {
            MDC.put("refresh_token", refreshToken.getToken().getTokenValue());
            log.debug("adding authorization with refresh token to the cache");
            MDC.clear();
            cache.put(refreshToken.getToken().getTokenValue(), authorization);
        }

        var msg = toMessage(authorization);
        if (msg.getState() != null && !msg.getState().isBlank()) {
            Cookie cookie = new Cookie(CLIENT_STATE_COOKIE, msg.getState());
            cookie.setPath("/");
            cookie.setMaxAge(60 * 60 * 24 * 365 * 10);
            cookie.setHttpOnly(true);
            ((ServletRequestAttributes) RequestContextHolder
                    .getRequestAttributes()).getResponse()
                    .addCookie(cookie);
        }

        if (msg.getAuthorizationCodeValue() != null && !msg.getAuthorizationCodeValue().isBlank())
            msg.setAuthorizationCodeValue(cipher.encrypt(msg.getAuthorizationCodeValue()));

        if (msg.getAccessTokenValue() != null && !msg.getAccessTokenValue().isBlank())
            msg.setAccessTokenValue(cipher.encrypt(msg.getAccessTokenValue()));

        if (msg.getRefreshTokenValue() != null && !msg.getRefreshTokenValue().isBlank())
            msg.setRefreshTokenValue(cipher.encrypt(msg.getRefreshTokenValue()));

        this.amqpTemplate.convertAndSend(
                configuration.getAuthorization().getExchange(),
                configuration.getAuthorization().getRouting(),
                msg);
    }

    @RateLimiter(name = "mutateRateLimiter")
    public void remove(OAuth2Authorization authorization) {
        Assert.notNull(authorization, "authorization cannot be null");
        MDC.put("id", authorization.getId());
        log.info("trying to remove authorization by id");

        String state = authorization.getAttribute(OAuth2ParameterNames.STATE);
        OAuth2Authorization.Token<OAuth2AuthorizationCode> authorizationCode =
                authorization.getToken(OAuth2AuthorizationCode.class);
        OAuth2Authorization.Token<OAuth2AccessToken> accessToken =
                authorization.getToken(OAuth2AccessToken.class);
        OAuth2Authorization.Token<OAuth2RefreshToken> refreshToken =
                authorization.getToken(OAuth2RefreshToken.class);

        cache.delete(authorization.getId());
        log.info("deleting authorization by id from the cache");
        MDC.clear();
        if (state != null && !state.isBlank()) {
            MDC.put("state", state);
            log.info("deleting authorization with state from the cache");
            MDC.clear();
            cache.delete(state);
        } else if (authorizationCode != null && authorizationCode.getToken() != null) {
            MDC.put("code", authorizationCode.getToken().getTokenValue());
            log.info("deleting authorization with code from the cache");
            MDC.clear();
            cache.delete(authorizationCode.getToken().getTokenValue());
        } else if (accessToken != null && accessToken.getToken() != null) {
            MDC.put("access_token", accessToken.getToken().getTokenValue());
            log.info("deleting authorization with access token from the cache");
            MDC.clear();
            cache.delete(accessToken.getToken().getTokenValue());
        } else if (refreshToken != null && refreshToken.getToken() != null) {
            MDC.put("refresh_token", refreshToken.getToken().getTokenValue());
            log.info("deleting authorization with refresh token from the cache");
            MDC.clear();
            cache.delete(refreshToken.getToken().getTokenValue());
        }

        var msg = toMessage(authorization);
        msg.setAccessTokenValue("***");
        msg.setRefreshTokenValue("***");
        msg.setInvalidated(true);

        this.amqpTemplate.convertSendAndReceive(
                configuration.getAuthorization().getExchange(),
                configuration.getAuthorization().getRouting(),
                msg
        );
    }

    @RateLimiter(name = "getRateLimiter", fallbackMethod = "findAuthorizationFallback")
    @SneakyThrows
    public OAuth2Authorization findById(String id) {
        Assert.hasText(id, "id cannot be empty");
        MDC.put("id", id);
        log.info("trying to find authorization by id");

        var authorization = this.cache.get(id);
        if (authorization != null) {
            log.info("found authorization with id in the cache");
            this.cache.delete(authorization.getId());
            log.info("authorization has been removed from the cache by id");
            MDC.clear();
            return authorization;
        }

        var msg = this.queryUsecases.getById(id);
        if (msg.getAuthorizationCodeValue() != null && !msg.getAuthorizationCodeValue().isBlank())
            msg.setAuthorizationCodeValue(cipher.decrypt(msg.getAuthorizationCodeValue()));

        if (msg.getAccessTokenValue() != null && !msg.getAccessTokenValue().isBlank())
            msg.setAccessTokenValue(cipher.decrypt(msg.getAccessTokenValue()));

        if (msg.getRefreshTokenValue() != null && !msg.getRefreshTokenValue().isBlank())
            msg.setRefreshTokenValue(cipher.decrypt(msg.getRefreshTokenValue()));

        MDC.clear();
        return toObject(this.queryUsecases.getById(id));
    }

    private OAuth2Authorization findAuthorizationFallback(String id, Throwable e) {
        MDC.put("id", id);
        MDC.put("reason", e.getMessage());
        log.warn("authorization request is blocked due to rate-limiting");
        MDC.clear();
        return null;
    }

    @RateLimiter(name = "getRateLimiter", fallbackMethod = "findAuthorizationByTokenFallback")
    @SneakyThrows
    public OAuth2Authorization findByToken(String token, OAuth2TokenType tokenType) {
        Assert.hasText(token, "token cannot be empty");
        MDC.put("token", token);
        log.info("trying to find authorization by token");

        var authorization = this.cache.get(token);
        if (authorization != null) {
            log.info("found authorization by token in the cache");
            this.cache.delete(authorization.getId());
            log.info("authorization with token has been removed from the cache");
            return authorization;
        }

        MDC.clear();
        Authorization result;
        if (tokenType == null) {
            log.info("trying to find authorization by any value");
            result = this.queryUsecases.getByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(token);
        } else if (OAuth2ParameterNames.STATE.equals(tokenType.getValue())) {
            MDC.put("state", token);
            log.info("trying to find authorization by state");
            MDC.clear();
            result = this.queryUsecases.getByState(token);
        } else if (OAuth2ParameterNames.CODE.equals(tokenType.getValue())) {
            MDC.put("authorization_code", token);
            log.info("trying to find authorization by authorization code");
            MDC.clear();
            result = this.queryUsecases.getByAuthorizationCodeValue(token);
        } else if (OAuth2ParameterNames.ACCESS_TOKEN.equals(tokenType.getValue())) {
            MDC.put("access_token", token);
            log.info("trying to find authorization by access token");
            MDC.clear();
            result = this.queryUsecases.getByAccessTokenValue(token);
        } else if (OAuth2ParameterNames.REFRESH_TOKEN.equals(tokenType.getValue())) {
            MDC.put("refresh_token", token);
            log.debug("trying to find authorization by refresh token");
            MDC.clear();
            result = this.queryUsecases.getByRefreshTokenValue(token);
        } else {
            log.info("empty authorization");
            return null;
        }

        if (result.getAuthorizationCodeValue() != null && !result.getAuthorizationCodeValue().isBlank())
            result.setAuthorizationCodeValue(cipher.decrypt(result.getAuthorizationCodeValue()));

        if (result.getAccessTokenValue() != null && !result.getAccessTokenValue().isBlank())
            result.setAccessTokenValue(cipher.decrypt(result.getAccessTokenValue()));

        if (result.getRefreshTokenValue() != null && !result.getRefreshTokenValue().isBlank())
            result.setRefreshTokenValue(cipher.decrypt(result.getRefreshTokenValue()));

        return toObject(result);
    }

    private OAuth2Authorization findAuthorizationByTokenFallback(String token, OAuth2TokenType tokenType, Throwable e) {
        MDC.put("token", token);
        MDC.put("reason", e.getMessage());
        log.warn("authorization token request is blocked due to rate-limiting");
        MDC.clear();
        return null;
    }

    private OAuth2Authorization toObject(Authorization entity) {
        RegisteredClient registeredClient = this.registeredClientRepository.findById(entity.getRegisteredClientId());
        if (registeredClient == null) {
            throw new DataRetrievalFailureException(
                    "the registered client with id '" + entity.getRegisteredClientId() + "' was not found in the registered client repository.");
        }

        OAuth2Authorization.Builder builder = OAuth2Authorization.withRegisteredClient(registeredClient)
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
            OAuth2AccessToken accessToken = new OAuth2AccessToken(
                    OAuth2AccessToken.TokenType.BEARER,
                    entity.getAccessTokenValue(),
                    entity.getAccessTokenIssuedAt().toInstant(),
                    entity.getAccessTokenExpiresAt().toInstant(),
                    StringUtils.commaDelimitedListToSet(entity.getAccessTokenScopes()));
            builder.token(accessToken, metadata -> metadata.putAll(parseMap(entity.getAccessTokenMetadata())));
        }

        if (entity.getRefreshTokenValue() != null) {
            OAuth2RefreshToken refreshToken = new OAuth2RefreshToken(
                    entity.getRefreshTokenValue(),
                    entity.getRefreshTokenIssuedAt().toInstant(),
                    entity.getRefreshTokenExpiresAt().toInstant());
            builder.token(refreshToken, metadata -> metadata.putAll(parseMap(entity.getRefreshTokenMetadata())));
        }

        return builder.build();
    }

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

    private void setTokenValues(
            OAuth2Authorization.Token<?> token,
            Consumer<String> tokenValueConsumer,
            Consumer<Date> issuedAtConsumer,
            Consumer<Date> expiresAtConsumer,
            Consumer<String> metadataConsumer) {
        if (token != null) {
            OAuth2Token oAuth2Token = token.getToken();
            tokenValueConsumer.accept(oAuth2Token.getTokenValue());
            issuedAtConsumer.accept(Date.from(oAuth2Token.getIssuedAt()));
            expiresAtConsumer.accept(Date.from(oAuth2Token.getExpiresAt()));
            metadataConsumer.accept(writeMap(token.getMetadata()));
        }
    }

    private Map<String, Object> parseMap(String data) {
        try {
            return this.objectMapper.readValue(data, new TypeReference<Map<String, Object>>() {
            });
        } catch (Exception ex) {
            throw new IllegalArgumentException(ex.getMessage(), ex);
        }
    }

    private String writeMap(Map<String, Object> metadata) {
        try {
            return this.objectMapper.writeValueAsString(metadata);
        } catch (Exception ex) {
            throw new IllegalArgumentException(ex.getMessage(), ex);
        }
    }

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
