// (c) Copyright Ascensio System SIA 2009-2025
//
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
//
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
//
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
//
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
//
// All the Product's GUI elements, including illustrations and icon sets, as well as technical
// writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

package com.asc.authorization.application.security.oauth.service;

import com.asc.authorization.application.configuration.properties.SecurityConfigurationProperties;
import com.asc.authorization.application.exception.authorization.AuthorizationCleanupException;
import com.asc.authorization.application.exception.authorization.AuthorizationPersistenceException;
import com.asc.authorization.application.mapper.AuthorizationMapper;
import com.asc.authorization.application.security.authentication.BasicSignature;
import com.asc.authorization.data.authorization.entity.AuthorizationEntity;
import com.asc.authorization.data.authorization.repository.JpaAuthorizationRepository;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
import com.asc.common.utilities.crypto.EncryptionService;
import com.asc.common.utilities.crypto.HashingService;
import jakarta.servlet.http.Cookie;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.core.OAuth2AccessToken;
import org.springframework.security.oauth2.core.OAuth2RefreshToken;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.security.oauth2.server.authorization.client.RegisteredClientRepository;
import org.springframework.stereotype.Service;
import org.springframework.transaction.PlatformTransactionManager;
import org.springframework.transaction.TransactionDefinition;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.transaction.support.TransactionTemplate;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

/**
 * Service implementation for managing OAuth2 authorizations.
 *
 * <p>This service handles saving, retrieving, and deleting OAuth2 authorizations and their
 * associated tokens. It also manages encryption, hashing, and tenant-specific data during these
 * operations.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AuthorizationService
    implements OAuth2AuthorizationService, AuthorizationCleanupService {
  private static final String CLIENT_STATE_COOKIE = "client_state";

  private final SecurityConfigurationProperties securityConfigurationProperties;
  private final PlatformTransactionManager transactionManager;

  private final AuthorizationMapper authorizationMapper;
  private final EncryptionService encryptionService;
  private final HashingService hashingService;
  private final JpaConsentRepository jpaConsentRepository;
  private final JpaAuthorizationRepository jpaAuthorizationRepository;
  private final RegisteredClientAccessibilityService registeredClientAccessibilityRepository;
  private final RegisteredClientRepository registeredClientRepository;

  /**
   * Saves an OAuth2 authorization to the database.
   *
   * <p>The authorization is encrypted, hashed, and stored. If the authorization already exists, it
   * is merged with the new data.
   *
   * @param authorization the OAuth2 authorization to save.
   * @throws AuthorizationPersistenceException if an error occurs while saving.
   */
  public void save(OAuth2Authorization authorization) {
    try {
      MDC.put("id", authorization.getId());
      log.info("Saving authorization");

      setClientStateCookie(authorization);

      var signature = getRequestSignature();

      var accessToken = authorization.getToken(OAuth2AccessToken.class);
      var refreshToken = authorization.getToken(OAuth2RefreshToken.class);
      var atoken =
          accessToken != null
              ? encryptionService.encrypt(accessToken.getToken().getTokenValue())
              : null;
      var rtoken =
          refreshToken != null
              ? encryptionService.encrypt(refreshToken.getToken().getTokenValue())
              : null;

      var template = new TransactionTemplate(transactionManager);
      template.setIsolationLevel(TransactionDefinition.ISOLATION_READ_COMMITTED);
      template.setTimeout(2);
      template.execute(
          status -> {
            try {
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

              if (accessToken != null && accessToken.getToken() != null)
                entity.setAccessTokenHash(
                    hashingService.hash(accessToken.getToken().getTokenValue()));

              if (refreshToken != null && refreshToken.getToken() != null)
                entity.setRefreshTokenHash(
                    hashingService.hash(refreshToken.getToken().getTokenValue()));

              if (signature != null && signature.getTenantId() > 0)
                entity.setTenantId(signature.getTenantId());
              entity.setAccessTokenValue(atoken);
              entity.setRefreshTokenValue(rtoken);

              jpaAuthorizationRepository.save(entity);
              log.info("Authorization saved successfully");
              return null;
            } catch (Exception ex) {
              status.setRollbackOnly();
              throw ex;
            }
          });
    } catch (Exception e) {
      log.error("Could not save authorization");
      throw new AuthorizationPersistenceException(e);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Removes an OAuth2 authorization from the database.
   *
   * @param authorization the OAuth2 authorization to remove.
   * @throws AuthorizationCleanupException if an error occurs while removing.
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

  @Transactional(
      timeout = 2,
      rollbackFor = {Exception.class})
  public void remove(String principalId, String clientId) {
    jpaAuthorizationRepository.deleteAllAuthorizationsByPrincipalIdAndClientId(
        principalId, clientId);
    jpaConsentRepository.deleteAllConsentsByPrincipalIdAndClientId(principalId, clientId);
  }

  /**
   * Retrieves an OAuth2 authorization by its ID.
   *
   * @param id the ID of the authorization.
   * @return the OAuth2 authorization, or {@code null} if not found.
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
                      e.getRegisteredClientId()))
          .map(
              entity -> {
                var accessToken = entity.getAccessTokenValue();
                var refreshToken = entity.getRefreshTokenValue();
                if (accessToken != null && !accessToken.isBlank())
                  entity.setAccessTokenValue(encryptionService.decrypt(accessToken));
                if (refreshToken != null && !refreshToken.isBlank())
                  entity.setRefreshTokenValue(encryptionService.decrypt(refreshToken));
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
   * Retrieves an OAuth2 authorization by its token.
   *
   * @param token the token associated with the authorization.
   * @param tokenType the type of the token (e.g., access token, refresh token).
   * @return the OAuth2 authorization, or {@code null} if not found.
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
                      e.getRegisteredClientId()))
          .map(
              entity -> {
                var accessToken = entity.getAccessTokenValue();
                var refreshToken = entity.getRefreshTokenValue();
                if (accessToken != null && !accessToken.isBlank())
                  entity.setAccessTokenValue(encryptionService.decrypt(accessToken));
                if (refreshToken != null && !refreshToken.isBlank())
                  entity.setRefreshTokenValue(encryptionService.decrypt(refreshToken));
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
   * Sets the client state cookie for the authorization.
   *
   * @param authorization the OAuth2 authorization containing the state.
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
   * Retrieves the tenant signature information from the current request.
   *
   * @return the {@link BasicSignature}, or {@code null} if not found.
   */
  private BasicSignature getRequestSignature() {
    var ctx = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
    if (ctx == null) return null;

    var signature =
        ctx.getRequest().getAttribute(securityConfigurationProperties.getSignatureHeader());
    if (signature == null) return null;

    return (BasicSignature) signature;
  }
}
