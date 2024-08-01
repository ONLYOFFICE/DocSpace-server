// (c) Copyright Ascensio System SIA 2009-2024
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

package com.asc.authorization.application.security.jwks;

import com.asc.authorization.application.configuration.security.AscOAuth2RegisteredClientConfiguration;
import com.asc.authorization.application.mapper.KeyPairMapper;
import com.asc.authorization.application.security.oauth.authorities.TenantAuthority;
import com.asc.authorization.application.security.oauth.services.AscKeyPairService;
import com.asc.authorization.data.key.entity.KeyPair;
import com.asc.common.core.domain.value.KeyPairType;
import com.github.benmanes.caffeine.cache.Cache;
import com.github.benmanes.caffeine.cache.Caffeine;
import com.github.benmanes.caffeine.cache.Expiry;
import com.nimbusds.jose.KeySourceException;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.JWKSelector;
import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
import jakarta.annotation.PostConstruct;
import java.security.NoSuchAlgorithmException;
import java.security.spec.InvalidKeySpecException;
import java.time.Duration;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.util.Arrays;
import java.util.Comparator;
import java.util.List;
import java.util.stream.Collectors;
import lombok.AllArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.security.oauth2.jose.jws.SignatureAlgorithm;
import org.springframework.security.oauth2.server.authorization.token.JwtEncodingContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenCustomizer;
import org.springframework.stereotype.Component;

/** Component for managing JSON Web Keys (JWK) from a repository and customizing JWT encoding. */
@Slf4j
@Component
@AllArgsConstructor
public class KeyPairRepositoryJWKSource
    implements JWKSource<SecurityContext>, OAuth2TokenCustomizer<JwtEncodingContext> {
  private final AscOAuth2RegisteredClientConfiguration registeredClientConfiguration;

  @Autowired
  @Qualifier("rsa")
  private final JwksKeyPairGenerator keyPairGenerator;

  private final KeyPairMapper keyPairMapper;
  private final AscKeyPairService keyPairService;

  private static Duration rotationPeriod;
  private static Duration deprecationPeriod;
  private static final Cache<String, KeyPair> keyPairsCache =
      Caffeine.newBuilder().expireAfter(new CreationDateExpiry()).build();

  /**
   * Initializes the key rotation and deprecation periods and performs initial key rotation and
   * deprecation.
   *
   * @throws NoSuchAlgorithmException if a required algorithm is not available.
   */
  @SneakyThrows
  @PostConstruct
  public void init() {
    rotationPeriod =
        Duration.ofMinutes(registeredClientConfiguration.getAccessTokenMinutesTTL() * 4L);
    deprecationPeriod =
        Duration.ofMinutes(registeredClientConfiguration.getAccessTokenMinutesTTL());
    invalidateKeys();
    rotateKeysIfNeeded();
    refreshKeyPairs();
  }

  /** Scheduled task for key rotation and cleanup, running every minute. */
  @Scheduled(fixedRate = 60000)
  public void scheduledKeyRotationAndCleanup() {
    try {
      invalidateKeys();
      rotateKeysIfNeeded();
      refreshKeyPairs();
    } catch (Exception e) {
      log.error("Error during scheduled key rotation and cleanup", e);
    }
  }

  /**
   * Retrieves JSON Web Keys (JWK) based on the provided JWK selector and security context.
   *
   * @param jwkSelector the JWK selector.
   * @param securityContext the security context.
   * @return a list of JWKs matching the selector.
   * @throws KeySourceException if an error occurs while retrieving the keys.
   */
  @SneakyThrows
  public List<JWK> get(JWKSelector jwkSelector, SecurityContext securityContext)
      throws KeySourceException {
    log.debug("Trying to get JWK");
    List<JWK> result =
        keyPairsCache.asMap().values().stream()
            .filter(
                keyPair -> {
                  MDC.put("id", keyPair.getId());
                  MDC.put("type", keyPair.getPairType().name());
                  boolean matches =
                      keyPair.getPairType().equals(keyPairGenerator.type())
                          && jwkSelector.getMatcher().matches(buildJwk(keyPair));
                  MDC.clear();
                  return matches;
                })
            .map(this::buildJwk)
            .collect(Collectors.toList());

    if (result.isEmpty()) refreshKeyPairs();

    return result;
  }

  /**
   * Customizes the JWT encoding context with additional claims and headers.
   *
   * @param context the JWT encoding context.
   */
  public void customize(JwtEncodingContext context) {
    var activeKeyPair = getLatestActiveKeyPair();
    if (activeKeyPair == null)
      throw new UnsupportedOperationException("Could not find any suitable keypair");

    log.debug("Using key pair with ID: {}", activeKeyPair.getId());

    var principal = context.getPrincipal();
    var authority = principal.getAuthorities().stream().findFirst().orElse(null);

    if (context.getAuthorization().getRegisteredClientId() != null)
      context.getClaims().claim("cid", context.getAuthorization().getRegisteredClientId());
    if (principal.getPrincipal() != null)
      context.getClaims().subject(principal.getPrincipal().toString());
    if (authority instanceof TenantAuthority tenantAuthority)
      context
          .getClaims()
          .issuer(String.format("%s/oauth2", tenantAuthority.getAuthority()))
          .claim("tid", tenantAuthority.getTenantId())
          .audience(Arrays.asList(tenantAuthority.getAuthority()));

    context
        .getJwsHeader()
        .keyId(activeKeyPair.getId())
        .algorithm(
            activeKeyPair.getPairType().equals(KeyPairType.EC)
                ? SignatureAlgorithm.ES256
                : SignatureAlgorithm.RS256);
  }

  /**
   * Rotates keys if needed based on the rotation period.
   *
   * @throws NoSuchAlgorithmException if a required algorithm is not available.
   */
  protected void rotateKeysIfNeeded() throws NoSuchAlgorithmException {
    if (shouldRotateKeys()) {
      generateAndStoreNewKeyPair();
      refreshKeyPairs();
    }
  }

  /** Deprecates old keys that are beyond the deprecation period. */
  protected void invalidateKeys() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    keyPairService.invalidateKeyPairs(cutoffTime);
  }

  /**
   * Determines if keys should be rotated based on the latest active key pair's age.
   *
   * @return true if keys should be rotated, false otherwise.
   */
  private boolean shouldRotateKeys() {
    var latestKeyPair = getLatestActiveKeyPair();
    return latestKeyPair == null
        || Duration.between(latestKeyPair.getCreatedAt(), ZonedDateTime.now(ZoneOffset.UTC))
                .compareTo(rotationPeriod)
            > 0;
  }

  /**
   * Retrieves the latest active key pair.
   *
   * @return the latest active key pair, or null if none exists.
   */
  private KeyPair getLatestActiveKeyPair() {
    return keyPairsCache.asMap().values().stream()
        .max(Comparator.comparing(KeyPair::getCreatedAt))
        .orElse(null);
  }

  /** Refreshes the key pairs cache by retrieving active key pairs from the service. */
  private void refreshKeyPairs() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    var keyPairs = keyPairService.findActiveKeyPairs(cutoffTime);
    keyPairs.forEach(keyPair -> keyPairsCache.put(keyPair.getId(), keyPair));
  }

  /**
   * Generates and stores a new key pair.
   *
   * @throws NoSuchAlgorithmException if a required algorithm is not available.
   */
  private void generateAndStoreNewKeyPair() throws NoSuchAlgorithmException {
    var pair = keyPairGenerator.generateKeyPair();
    var keyPair =
        KeyPair.builder()
            .publicKey(keyPairMapper.toString(pair.getPublic()))
            .privateKey(keyPairMapper.toString(pair.getPrivate()))
            .pairType(keyPairGenerator.type())
            .createdAt(ZonedDateTime.now(ZoneOffset.UTC))
            .build();
    var saved = keyPairService.saveKeyPair(keyPair);
    keyPairsCache.put(saved.getId(), saved);
  }

  /**
   * Builds a JSON Web Key (JWK) from the provided key pair.
   *
   * @param keyPair the key pair.
   * @return the JWK.
   */
  private JWK buildJwk(KeyPair keyPair) {
    try {
      return keyPairGenerator.buildKey(
          keyPair.getId(), keyPair.getPublicKey(), keyPair.getPrivateKey());
    } catch (NoSuchAlgorithmException | InvalidKeySpecException e) {
      log.error("Error building JWK from KeyPair", e);
      return null;
    }
  }

  /** Custom expiry policy for key pairs based on their creation date. */
  private static class CreationDateExpiry implements Expiry<String, KeyPair> {
    public long expireAfterCreate(String key, KeyPair keyPair, long currentTime) {
      long durationSinceCreation =
          Duration.between(keyPair.getCreatedAt(), ZonedDateTime.now(ZoneOffset.UTC)).toNanos();
      return rotationPeriod
          .plus(deprecationPeriod)
          .minus(Duration.ofNanos(durationSinceCreation))
          .toNanos();
    }

    public long expireAfterUpdate(
        String key, KeyPair keyPair, long currentTime, long currentDuration) {
      return currentDuration;
    }

    public long expireAfterRead(
        String key, KeyPair keyPair, long currentTime, long currentDuration) {
      return currentDuration;
    }
  }
}
