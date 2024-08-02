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
import java.util.Collections;
import java.util.Comparator;
import java.util.List;
import java.util.Set;
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

/**
 * A component responsible for managing JWK (JSON Web Key) sources and customizing OAuth2 tokens. It
 * handles key rotation, invalidation, and JWK retrieval for security purposes.
 */
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

  /**
   * Initializes the key rotation and deprecation periods based on the configuration. Also
   * invalidates and rotates keys if needed.
   */
  @SneakyThrows
  @PostConstruct
  public void init() {
    rotationPeriod =
        Duration.ofMinutes(registeredClientConfiguration.getAccessTokenMinutesTTL() * 4L);
    deprecationPeriod =
        Duration.ofMinutes(registeredClientConfiguration.getAccessTokenMinutesTTL());
    rotateKeysIfNeeded();
    invalidateKeys();
  }

  /** Scheduled task to periodically invalidate and rotate keys. Runs every 60 seconds. */
  @Scheduled(fixedRate = 60000)
  public void scheduledKeyRotationAndCleanup() {
    try {
      rotateKeysIfNeeded();
      invalidateKeys();
    } catch (Exception e) {
      log.error("Error during scheduled key rotation and cleanup", e);
    }
  }

  /**
   * Retrieves a list of JWKs that match the specified selector and security context.
   *
   * @param jwkSelector The JWK selector used to filter keys.
   * @param securityContext The security context.
   * @return A list of matching JWKs.
   * @throws KeySourceException if an error occurs while fetching JWKs.
   */
  @SneakyThrows
  public List<JWK> get(JWKSelector jwkSelector, SecurityContext securityContext)
      throws KeySourceException {
    log.debug("Trying to get JWK");
    var result =
        getActiveKeyPairs().stream()
            .filter(
                keyPair -> {
                  MDC.put("id", keyPair.getId());
                  MDC.put("type", keyPair.getPairType().name());
                  var matches =
                      keyPair.getPairType().equals(keyPairGenerator.type())
                          && jwkSelector.getMatcher().matches(buildJwk(keyPair));
                  MDC.clear();
                  return matches;
                })
            .map(this::buildJwk)
            .collect(Collectors.toList());

    if (result.isEmpty()) log.warn("No matching JWKs found");

    return result;
  }

  /**
   * Customizes the JWT encoding context with specific claims and headers.
   *
   * @param context The JWT encoding context.
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
          .audience(Collections.singletonList(tenantAuthority.getAuthority()));

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
   * @throws NoSuchAlgorithmException if an error occurs while generating a new key pair.
   */
  protected void rotateKeysIfNeeded() throws NoSuchAlgorithmException {
    var latestKeyPair = getLatestActiveKeyPair();
    if (latestKeyPair == null
        || Duration.between(latestKeyPair.getCreatedAt(), ZonedDateTime.now(ZoneOffset.UTC))
                .compareTo(rotationPeriod)
            > 0) {
      generateAndStoreNewKeyPair();
    }
  }

  /** Invalidates keys that have surpassed the rotation and deprecation periods. */
  protected void invalidateKeys() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    keyPairService.invalidateKeyPairs(cutoffTime);
  }

  /**
   * Retrieves the latest active key pair.
   *
   * @return The latest active key pair or null if none found.
   */
  private KeyPair getLatestActiveKeyPair() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    return keyPairService.findActiveKeyPairs(cutoffTime).stream()
        .max(Comparator.comparing(KeyPair::getCreatedAt))
        .orElse(null);
  }

  /**
   * Retrieves the set of active key pairs.
   *
   * @return A set of active key pairs.
   */
  private Set<KeyPair> getActiveKeyPairs() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    return keyPairService.findActiveKeyPairs(cutoffTime);
  }

  /**
   * Generates and stores a new key pair.
   *
   * @throws NoSuchAlgorithmException if an error occurs while generating the key pair.
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
    keyPairService.saveKeyPair(keyPair);
  }

  /**
   * Builds a JWK from the given key pair.
   *
   * @param keyPair The key pair to build the JWK from.
   * @return The resulting JWK or null if an error occurs.
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
}
