package com.asc.authorization.web.server.utilities;

import com.asc.authorization.web.server.messaging.AuthorizationMessage;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.Module;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import com.asc.authorization.core.entities.Authorization;
import com.asc.authorization.web.security.oauth.services.AscOAuth2AuthorizationService;
import jakarta.annotation.PostConstruct;
import org.springframework.security.jackson2.SecurityJackson2Modules;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.OAuth2AccessToken;
import org.springframework.security.oauth2.core.OAuth2RefreshToken;
import org.springframework.security.oauth2.core.OAuth2Token;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationCode;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.jackson2.OAuth2AuthorizationServerJackson2Module;
import org.springframework.stereotype.Component;
import org.springframework.util.StringUtils;

import java.time.Instant;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.List;
import java.util.Map;
import java.util.function.Consumer;

/**
 *
 */
@Component
public class AuthorizationMapper {
    private final ObjectMapper objectMapper = new ObjectMapper();

    @PostConstruct
    public void init() {
        ClassLoader classLoader = AscOAuth2AuthorizationService.class.getClassLoader();
        List<Module> securityModules = SecurityJackson2Modules.getModules(classLoader);
        objectMapper.registerModules(securityModules);
        objectMapper.registerModule(new OAuth2AuthorizationServerJackson2Module());
        objectMapper.registerModule(new JavaTimeModule());
        objectMapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);
    }

    /**
     *
     * @param entity
     * @return
     */
    public OAuth2Authorization fromEntity(Authorization entity, RegisteredClient client) {
        if (client == null)
            return null;
        OAuth2Authorization.Builder builder = OAuth2Authorization
                .withRegisteredClient(client)
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

    /**
     *
     * @param message
     * @param client
     * @return
     */
    public OAuth2Authorization fromMessage(AuthorizationMessage message, RegisteredClient client) {
        if (client == null)
            return null;
        OAuth2Authorization.Builder builder = OAuth2Authorization
                .withRegisteredClient(client)
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
    public AuthorizationMessage toMessage(OAuth2Authorization authorization) {
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
            return objectMapper.writeValueAsString(metadata);
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
