package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.application.mapper.AuthorizationMapper;
import com.asc.authorization.application.mapper.ClientMapper;
import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.common.data.client.repository.JpaClientRepository;
import com.asc.common.utilities.cipher.EncryptionService;
import jakarta.servlet.http.Cookie;
import java.util.concurrent.*;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
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

/**
 * Service to handle OAuth2 Authorization operations including saving, removing, and finding
 * authorizations.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AscAuthorizationService implements OAuth2AuthorizationService {
  private static final String CLIENT_STATE_COOKIE = "client_state";

  private final ExecutorService executor = Executors.newVirtualThreadPerTaskExecutor();
  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final JpaClientRepository jpaClientRepository;
  private final EncryptionService encryptionService;
  private final AuthorizationMapper authorizationMapper;
  private final ClientMapper clientMapper;

  /**
   * Saves the given OAuth2Authorization object, encrypting sensitive data and setting state
   * cookies.
   *
   * @param authorization the OAuth2Authorization object to save.
   */
  @Transactional(
      timeout = 3,
      rollbackFor = {Exception.class})
  public void save(OAuth2Authorization authorization) {
    MDC.put("id", authorization.getId());
    log.info("Trying to save authorization");

    try {
      String state = authorization.getAttribute(OAuth2ParameterNames.STATE);
      var authorizationCode = authorization.getToken(OAuth2AuthorizationCode.class);
      var accessToken = authorization.getToken(OAuth2AccessToken.class);
      var refreshToken = authorization.getToken(OAuth2RefreshToken.class);

      if (state != null && !state.isBlank()) {
        log.info("Setting authorization state cookie");
        Cookie cookie = new Cookie(CLIENT_STATE_COOKIE, state);
        cookie.setPath("/");
        cookie.setMaxAge(60 * 60 * 24 * 365 * 10); // 10 years
        ((ServletRequestAttributes) RequestContextHolder.getRequestAttributes())
            .getResponse()
            .addCookie(cookie);
      }

      var atokenFuture =
          CompletableFuture.supplyAsync(
              () -> {
                try {
                  return accessToken != null
                      ? encryptionService.encrypt(accessToken.getToken().getTokenValue())
                      : null;
                } catch (Exception e) {
                  throw new IllegalStateException("Failed to encrypt access token", e);
                }
              },
              executor);

      var rtokenFuture =
          CompletableFuture.supplyAsync(
              () -> {
                try {
                  return refreshToken != null
                      ? encryptionService.encrypt(refreshToken.getToken().getTokenValue())
                      : null;
                } catch (Exception e) {
                  throw new IllegalStateException("Failed to encrypt refresh token", e);
                }
              },
              executor);

      var entity = authorizationMapper.toEntity(authorization);
      entity.setAccessTokenValue(atokenFuture.get(2, TimeUnit.SECONDS));
      entity.setRefreshTokenValue(rtokenFuture.get(2, TimeUnit.SECONDS));

      jpaAuthorizationRepository.save(entity);
      log.info("Authorization saved successfully");
    } catch (ExecutionException | InterruptedException | TimeoutException e) {
      log.warn("Could not save authorization", e);
      Thread.currentThread().interrupt();
    } finally {
      MDC.clear();
    }
  }

  /**
   * Removes the given OAuth2Authorization object by its ID.
   *
   * @param authorization the OAuth2Authorization object to remove.
   */
  @Transactional(
      timeout = 2,
      rollbackFor = {Exception.class})
  public void remove(OAuth2Authorization authorization) {
    MDC.put("id", authorization.getId());
    log.info("Trying to remove authorization by id");

    try {
      jpaAuthorizationRepository.deleteById(authorization.getId());
      log.info("Authorization removed successfully");
    } catch (Exception e) {
      log.warn("Could not remove authorization", e);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds an OAuth2Authorization object by its ID.
   *
   * @param id the ID of the authorization to find.
   * @return the found OAuth2Authorization object, or null if not found.
   */
  @Transactional(readOnly = true, timeout = 2)
  public OAuth2Authorization findById(String id) {
    MDC.put("id", id);
    log.info("Trying to find authorization by id");

    try {
      var response = jpaAuthorizationRepository.findById(id);
      if (response.isPresent()) {
        var entity = response.get();
        var clientResponse =
            jpaClientRepository
                .findClientByClientId(entity.getRegisteredClientId())
                .map(clientMapper::toRegisteredClient);

        if (clientResponse.isPresent()) {
          return authorizationMapper.fromEntity(entity, clientResponse.get());
        } else {
          log.warn("Registered client not found for client ID: {}", entity.getRegisteredClientId());
          return null;
        }
      } else {
        log.warn("Authorization not found for ID: {}", id);
        return null;
      }
    } catch (Exception e) {
      log.error("Error finding authorization by id", e);
      return null;
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds an OAuth2Authorization object by a token.
   *
   * @param token the token to find the authorization by.
   * @param tokenType the type of token.
   * @return the found OAuth2Authorization object, or null if not found.
   */
  @Transactional(readOnly = true, timeout = 2)
  public OAuth2Authorization findByToken(String token, OAuth2TokenType tokenType) {
    MDC.put("token", token);
    log.info("Trying to find authorization by token");

    try {
      var response =
          jpaAuthorizationRepository
              .findByStateOrAuthorizationCodeValueOrAccessTokenValueOrRefreshTokenValue(token);
      if (response.isPresent()) {
        var entity = response.get();
        var registeredClientOpt =
            jpaClientRepository
                .findClientByClientId(entity.getRegisteredClientId())
                .map(clientMapper::toRegisteredClient);

        if (registeredClientOpt.isPresent()) {
          return authorizationMapper.fromEntity(entity, registeredClientOpt.get());
        } else {
          log.warn("Registered client not found for client ID: {}", entity.getRegisteredClientId());
          return null;
        }
      } else {
        log.warn("Authorization not found for token: {}", token);
        return null;
      }
    } catch (Exception e) {
      log.error("Error finding authorization by token", e);
      return null;
    } finally {
      MDC.clear();
    }
  }
}
