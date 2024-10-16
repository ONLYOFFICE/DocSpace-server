package com.asc.authorization.application.security.service;

import com.asc.authorization.application.exception.authorization.AuthorizationCleanupException;
import com.asc.authorization.application.exception.authorization.AuthorizationPersistenceException;
import com.asc.authorization.application.mapper.AuthorizationMapper;
import com.asc.authorization.data.authorization.entity.AuthorizationEntity;
import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.common.utilities.crypto.HashingService;
import jakarta.servlet.http.Cookie;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.TimeUnit;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.core.OAuth2AccessToken;
import org.springframework.security.oauth2.core.OAuth2RefreshToken;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Isolation;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

@Slf4j
@Service
@RequiredArgsConstructor
public class AscTransactionalAuthorizationService {
  private static final String CLIENT_STATE_COOKIE = "client_state";

  private final ExecutorService executor = Executors.newVirtualThreadPerTaskExecutor();

  private final AuthorizationMapper authorizationMapper;
  private final EncryptionService encryptionService;
  private final HashingService hashingService;
  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final RegisteredClientAccessibilityService registeredClientAccessibilityRepository;
  private final RegisteredClientRepository registeredClientRepository;

  /**
   * Saves an OAuth2 authorization.
   *
   * @param authorization the authorization to save
   * @throws AuthorizationPersistenceException if an error occurs during saving
   */
  @Transactional(
      isolation = Isolation.SERIALIZABLE,
      timeout = 3,
      rollbackFor = {Exception.class})
  public void save(OAuth2Authorization authorization) {
    try {
      MDC.put("id", authorization.getId());
      log.info("Saving authorization");

      setClientStateCookie(authorization);
      var tenant = getTenantFromRequest();
      var atokenFuture = encryptTokenAsync(authorization.getToken(OAuth2AccessToken.class));
      var rtokenFuture = encryptTokenAsync(authorization.getToken(OAuth2RefreshToken.class));

      var existingAuthorizationOpt =
          jpaAuthorizationRepository
              .findByRegisteredClientIdAndPrincipalIdAndAuthorizationGrantType(
                  authorization.getRegisteredClientId(),
                  authorization.getPrincipalName(),
                  authorization.getAuthorizationGrantType().getValue());

      var mappedAuthorization = authorizationMapper.toEntity(authorization);
      var entity =
          authorizationMapper.merge(
              existingAuthorizationOpt.orElseGet(() -> mappedAuthorization),
              authorizationMapper.toEntity(authorization));

      entity.setAccessTokenHash(hashingService.hash(entity.getAccessTokenValue()));
      entity.setRefreshTokenHash(hashingService.hash(entity.getRefreshTokenValue()));

      if (tenant != null && tenant.getTenantId() > 0) entity.setTenantId(tenant.getTenantId());

      entity.setAccessTokenValue(atokenFuture.get(2, TimeUnit.SECONDS));
      entity.setRefreshTokenValue(rtokenFuture.get(2, TimeUnit.SECONDS));

      jpaAuthorizationRepository.save(entity);

      log.info("Authorization saved successfully");
    } catch (Exception e) {
      log.error("Could not save authorization");
      throw new AuthorizationPersistenceException(e);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Removes an OAuth2 authorization.
   *
   * @param authorization the authorization to remove
   * @throws AuthorizationCleanupException if an error occurs during removal
   */
  @Transactional(
      timeout = 2,
      rollbackFor = {Exception.class})
  public void remove(OAuth2Authorization authorization) {
    try {
      MDC.put("id", authorization.getId());
      log.info("Removing authorization by id");

      jpaAuthorizationRepository.deleteById(
          new AuthorizationEntity.AuthorizationId(
              authorization.getRegisteredClientId(),
              authorization.getPrincipalName(),
              authorization.getAuthorizationGrantType().getValue()));

      log.info("Authorization removed successfully");
    } catch (Exception e) {
      log.error("Could not remove authorization");
      throw new AuthorizationCleanupException(e);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds an OAuth2 authorization by its ID.
   *
   * @param id the ID of the authorization
   * @return the found authorization, or null if not found
   */
  @Transactional(readOnly = true, timeout = 2)
  public OAuth2Authorization findById(String id) {
    MDC.put("id", id);
    log.info("Retrieving authorization by id");

    try {
      return jpaAuthorizationRepository
          .findByAuthorizationId(id)
          .filter(
              e ->
                  registeredClientAccessibilityRepository.validateClientAccessibility(
                      e.getRegisteredClientId(), e.getTenantId()))
          .map(
              entity -> {
                decryptAuthorizationTokens(entity);
                return authorizationMapper.fromEntity(
                    entity,
                    registeredClientRepository.findByClientId(entity.getRegisteredClientId()));
              })
          .orElse(null);
    } catch (Exception e) {
      log.error("Could not find authorization by id", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds an OAuth2 authorization by its token.
   *
   * @param token the token of the authorization
   * @param tokenType the type of the token
   * @return the found authorization, or null if not found
   */
  @Transactional(readOnly = true, timeout = 2)
  public OAuth2Authorization findByToken(String token, OAuth2TokenType tokenType) {
    MDC.put("token", token);
    log.info("Retrieving authorization by token");

    try {
      var hashedToken =
          tokenType == null
                  || tokenType.equals(OAuth2TokenType.ACCESS_TOKEN)
                  || tokenType.equals(OAuth2TokenType.REFRESH_TOKEN)
              ? hashingService.hash(token)
              : token;

      return jpaAuthorizationRepository
          .findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(hashedToken)
          .filter(
              e ->
                  registeredClientAccessibilityRepository.validateClientAccessibility(
                      e.getRegisteredClientId(), e.getTenantId()))
          .map(
              entity -> {
                decryptAuthorizationTokens(entity);
                return authorizationMapper.fromEntity(
                    entity,
                    registeredClientRepository.findByClientId(entity.getRegisteredClientId()));
              })
          .orElse(null);
    } catch (Exception e) {
      log.error("Could not find authorization by token", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Sets the client state cookie for the given authorization.
   *
   * @param authorization the authorization
   */
  private void setClientStateCookie(OAuth2Authorization authorization) {
    var ctx = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
    var state = authorization.<String>getAttribute(OAuth2ParameterNames.STATE);
    if (state != null && !state.isBlank()) {
      if (ctx == null || ctx.getResponse() == null)
        throw new IllegalStateException("Request context holder or response is null");

      log.debug("Setting authorization state cookie: {}", state);

      var cookie = new Cookie(CLIENT_STATE_COOKIE, state);
      cookie.setPath("/");
      cookie.setMaxAge(60 * 60 * 24 * 365 * 10); // 10 years
      ctx.getResponse().addCookie(cookie);
    }
  }

  /**
   * Retrieves the tenant information from the current request.
   *
   * @return the tenant response, or null if not found
   */
  private AscTenantResponse getTenantFromRequest() {
    var ctx = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
    if (ctx == null) return null;

    return (AscTenantResponse) ctx.getRequest().getAttribute("tenant");
  }

  /**
   * Asynchronously encrypts a token.
   *
   * @param token the token to encrypt
   * @return a CompletableFuture containing the encrypted token value
   */
  private CompletableFuture<String> encryptTokenAsync(OAuth2Authorization.Token<?> token) {
    return CompletableFuture.supplyAsync(
        () -> {
          try {
            return token != null
                ? encryptionService.encrypt(token.getToken().getTokenValue())
                : null;
          } catch (Exception e) {
            throw new IllegalStateException("Failed to encrypt token", e);
          }
        },
        executor);
  }

  /**
   * Asynchronously decrypts a token.
   *
   * @param tokenValue the token value to decrypt
   * @return a CompletableFuture containing the decrypted token value
   */
  private CompletableFuture<String> decryptTokenAsync(String tokenValue) {
    return CompletableFuture.supplyAsync(
        () -> {
          try {
            return tokenValue != null ? encryptionService.decrypt(tokenValue) : null;
          } catch (Exception e) {
            throw new IllegalStateException("Failed to decrypt token", e);
          }
        },
        executor);
  }

  /**
   * Decrypts the tokens of the given authorization entity.
   *
   * @param entity the authorization entity
   */
  private void decryptAuthorizationTokens(AuthorizationEntity entity) {
    try {
      var accessTokenFuture = decryptTokenAsync(entity.getAccessTokenValue());
      var refreshTokenFuture = decryptTokenAsync(entity.getRefreshTokenValue());

      entity.setAccessTokenValue(accessTokenFuture.get(2, TimeUnit.SECONDS));
      entity.setRefreshTokenValue(refreshTokenFuture.get(2, TimeUnit.SECONDS));
    } catch (Exception e) {
      throw new IllegalStateException("Failed to decrypt tokens", e);
    }
  }
}
