package com.asc.authorization.application.mapper;

import com.asc.authorization.application.security.oauth.services.AscAuthorizationService;
import com.asc.authorization.data.authorization.entity.AuthorizationEntity;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.databind.SerializationFeature;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import jakarta.annotation.PostConstruct;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.util.Map;
import org.springframework.security.jackson2.SecurityJackson2Modules;
import org.springframework.security.oauth2.core.AuthorizationGrantType;
import org.springframework.security.oauth2.core.OAuth2AccessToken;
import org.springframework.security.oauth2.core.OAuth2RefreshToken;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationCode;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClient;
import org.springframework.security.oauth2.server.authorization.jackson2.OAuth2AuthorizationServerJackson2Module;
import org.springframework.stereotype.Component;
import org.springframework.util.StringUtils;

/**
 * Mapper class for converting between {@link AuthorizationEntity} and {@link OAuth2Authorization}.
 */
@Component
public class AuthorizationMapper {
  private final String UTC = "UTC";
  private final ObjectMapper objectMapper = new ObjectMapper();

  /** Initializes the {@link ObjectMapper} with required modules. */
  @PostConstruct
  public void init() {
    var classLoader = AscAuthorizationService.class.getClassLoader();
    var securityModules = SecurityJackson2Modules.getModules(classLoader);
    objectMapper.registerModules(securityModules);
    objectMapper.registerModule(new OAuth2AuthorizationServerJackson2Module());
    objectMapper.registerModule(new JavaTimeModule());
    objectMapper.disable(SerializationFeature.WRITE_DATES_AS_TIMESTAMPS);
  }

  /**
   * Converts an {@link AuthorizationEntity} to an {@link OAuth2Authorization}.
   *
   * @param entity the AuthorizationEntity to convert.
   * @param client the RegisteredClient associated with the authorization.
   * @return the OAuth2Authorization.
   */
  public OAuth2Authorization fromEntity(AuthorizationEntity entity, RegisteredClient client) {
    if (client == null) {
      return null;
    }
    var builder =
        OAuth2Authorization.withRegisteredClient(client)
            .id(entity.getId())
            .principalName(entity.getPrincipalName())
            .authorizationGrantType(
                resolveAuthorizationGrantType(entity.getAuthorizationGrantType()))
            .authorizedScopes(StringUtils.commaDelimitedListToSet(entity.getAuthorizedScopes()))
            .attributes(attributes -> attributes.putAll(parseMap(entity.getAttributes())));

    if (entity.getState() != null) {
      builder.attribute(OAuth2ParameterNames.STATE, entity.getState());
    }

    if (entity.getAuthorizationCodeValue() != null) {
      var authorizationCode =
          new OAuth2AuthorizationCode(
              entity.getAuthorizationCodeValue(),
              entity.getAuthorizationCodeIssuedAt().toInstant(),
              entity.getAuthorizationCodeExpiresAt().toInstant());
      builder.token(
          authorizationCode,
          metadata -> metadata.putAll(parseMap(entity.getAuthorizationCodeMetadata())));
    }

    if (entity.getAccessTokenValue() != null) {
      var issuedAt =
          entity.getAccessTokenIssuedAt() != null
              ? entity.getAccessTokenIssuedAt().toInstant()
              : null;
      var expiresAt =
          entity.getAccessTokenExpiresAt() != null
              ? entity.getAccessTokenExpiresAt().toInstant()
              : null;
      var accessToken =
          new OAuth2AccessToken(
              OAuth2AccessToken.TokenType.BEARER,
              entity.getAccessTokenValue(),
              issuedAt,
              expiresAt,
              StringUtils.commaDelimitedListToSet(entity.getAccessTokenScopes()));
      builder.token(
          accessToken, metadata -> metadata.putAll(parseMap(entity.getAccessTokenMetadata())));
    }

    if (entity.getRefreshTokenValue() != null) {
      var issuedAt =
          entity.getRefreshTokenIssuedAt() != null
              ? entity.getRefreshTokenIssuedAt().toInstant()
              : null;
      var expiresAt =
          entity.getRefreshTokenExpiresAt() != null
              ? entity.getRefreshTokenExpiresAt().toInstant()
              : null;
      var refreshToken = new OAuth2RefreshToken(entity.getRefreshTokenValue(), issuedAt, expiresAt);
      builder.token(
          refreshToken, metadata -> metadata.putAll(parseMap(entity.getRefreshTokenMetadata())));
    }

    return builder.build();
  }

  /**
   * Converts an {@link OAuth2Authorization} to an {@link AuthorizationEntity}.
   *
   * @param authorization the OAuth2Authorization to convert.
   * @return the AuthorizationEntity.
   */
  public AuthorizationEntity toEntity(OAuth2Authorization authorization) {
    var builder =
        AuthorizationEntity.builder()
            .id(authorization.getId())
            .registeredClientId(authorization.getRegisteredClientId())
            .principalName(authorization.getPrincipalName())
            .authorizationGrantType(authorization.getAuthorizationGrantType().getValue())
            .authorizedScopes(
                StringUtils.collectionToCommaDelimitedString(authorization.getAuthorizedScopes()))
            .attributes(writeMap(authorization.getAttributes()));

    if (authorization.getAttribute(OAuth2ParameterNames.STATE) != null) {
      builder.state(authorization.getAttribute(OAuth2ParameterNames.STATE));
    }

    var authorizationCode = authorization.getToken(OAuth2AuthorizationCode.class);
    if (authorizationCode != null) {
      builder
          .authorizationCodeValue(authorizationCode.getToken().getTokenValue())
          .authorizationCodeIssuedAt(
              ZonedDateTime.ofInstant(authorizationCode.getToken().getIssuedAt(), ZoneId.of(UTC)))
          .authorizationCodeExpiresAt(
              ZonedDateTime.ofInstant(authorizationCode.getToken().getExpiresAt(), ZoneId.of(UTC)))
          .authorizationCodeMetadata(writeMap(authorizationCode.getMetadata()));
    }

    var accessToken = authorization.getToken(OAuth2AccessToken.class);
    if (accessToken != null) {
      builder
          .accessTokenValue(accessToken.getToken().getTokenValue())
          .accessTokenIssuedAt(
              ZonedDateTime.ofInstant(accessToken.getToken().getIssuedAt(), ZoneId.of(UTC)))
          .accessTokenExpiresAt(
              ZonedDateTime.ofInstant(accessToken.getToken().getExpiresAt(), ZoneId.of(UTC)))
          .accessTokenMetadata(writeMap(accessToken.getMetadata()))
          .accessTokenType(accessToken.getToken().getTokenType().getValue())
          .accessTokenScopes(
              StringUtils.collectionToCommaDelimitedString(accessToken.getToken().getScopes()));
    }

    var refreshToken = authorization.getToken(OAuth2RefreshToken.class);
    if (refreshToken != null) {
      builder
          .refreshTokenValue(refreshToken.getToken().getTokenValue())
          .refreshTokenIssuedAt(
              ZonedDateTime.ofInstant(refreshToken.getToken().getIssuedAt(), ZoneId.of(UTC)))
          .refreshTokenExpiresAt(
              ZonedDateTime.ofInstant(refreshToken.getToken().getExpiresAt(), ZoneId.of(UTC)))
          .refreshTokenMetadata(writeMap(refreshToken.getMetadata()));
    }

    return builder.build();
  }

  /**
   * Parses a JSON string to a Map.
   *
   * @param data the JSON string.
   * @return the parsed Map.
   */
  private Map<String, Object> parseMap(String data) {
    if (data == null || data.isBlank()) {
      return Map.of();
    }
    try {
      return objectMapper.readValue(data, new TypeReference<Map<String, Object>>() {});
    } catch (Exception ex) {
      throw new IllegalArgumentException(ex.getMessage(), ex);
    }
  }

  /**
   * Converts a Map to a JSON string.
   *
   * @param metadata the Map.
   * @return the JSON string.
   */
  private String writeMap(Map<String, Object> metadata) {
    try {
      return objectMapper.writeValueAsString(metadata);
    } catch (Exception ex) {
      throw new IllegalArgumentException(ex.getMessage(), ex);
    }
  }

  /**
   * Resolves an {@link AuthorizationGrantType} from a string.
   *
   * @param authorizationGrantType the string representation.
   * @return the AuthorizationGrantType.
   */
  private static AuthorizationGrantType resolveAuthorizationGrantType(
      String authorizationGrantType) {
    return switch (authorizationGrantType) {
      case "authorization_code" -> AuthorizationGrantType.AUTHORIZATION_CODE;
      case "client_credentials" -> AuthorizationGrantType.CLIENT_CREDENTIALS;
      case "refresh_token" -> AuthorizationGrantType.REFRESH_TOKEN;
      default -> new AuthorizationGrantType(authorizationGrantType);
    };
  }
}
