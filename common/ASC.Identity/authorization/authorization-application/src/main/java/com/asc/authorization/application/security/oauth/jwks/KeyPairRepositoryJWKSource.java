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

package com.asc.authorization.application.security.oauth.jwks;

import com.asc.authorization.application.configuration.properties.RegisteredClientConfigurationProperties;
import com.asc.authorization.application.mapper.KeyPairMapper;
import com.asc.authorization.application.security.RegionUtils;
import com.asc.authorization.application.security.authentication.TenantAuthority;
import com.asc.authorization.application.security.oauth.service.KeyPairService;
import com.asc.authorization.data.key.entity.KeyPair;
import com.asc.common.core.domain.value.KeyPairType;
import com.asc.common.messaging.configuration.AuthorizationMessagingConfiguration;
import com.asc.common.service.transfer.message.KeyPairRetrievedEvent;
import com.asc.common.service.transfer.message.RetrieveKeyPairMessage;
import com.asc.common.utilities.crypto.EncryptionService;
import com.nimbusds.jose.KeySourceException;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.JWKSelector;
import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
import jakarta.annotation.PostConstruct;
import jakarta.annotation.Resource;
import java.security.NoSuchAlgorithmException;
import java.security.spec.InvalidKeySpecException;
import java.time.Duration;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.util.*;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import net.javacrumbs.shedlock.spring.annotation.SchedulerLock;
import org.slf4j.MDC;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.cache.caffeine.CaffeineCacheManager;
import org.springframework.context.event.EventListener;
import org.springframework.core.env.Environment;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.security.oauth2.jose.jws.SignatureAlgorithm;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationCode;
import org.springframework.security.oauth2.server.authorization.token.JwtEncodingContext;
import org.springframework.security.oauth2.server.authorization.token.OAuth2TokenCustomizer;
import org.springframework.stereotype.Component;

/**
 * Manages JSON Web Key (JWK) sources and customizes OAuth2 tokens.
 *
 * <p>This component handles key rotation, invalidation, and retrieval of JWKs for use in securing
 * OAuth2 tokens. It also customizes JWT claims and headers during token generation.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class KeyPairRepositoryJWKSource
    implements JWKSource<SecurityContext>, OAuth2TokenCustomizer<JwtEncodingContext> {
  private static final ThreadLocal<KeyPair> remoteKeyPairHolder = new ThreadLocal<>();

  @Value("${spring.application.region}")
  private String region;

  private final CaffeineCacheManager cacheManager;

  private final Environment environment;
  private final RegisteredClientConfigurationProperties registeredClientConfiguration;

  private final RabbitTemplate rpcRabbitTemplate;
  private final MessageConverter messageConverter;

  private final KeyPairMapper keyPairMapper;

  private final KeyPairService keyPairService;
  private final EncryptionService encryptionService;

  @Resource(name = "${spring.application.signature.jwks}")
  private JwksKeyPairGenerator keyPairGenerator;

  private static Duration rotationPeriod;
  private static Duration deprecationPeriod;

  /**
   * Initializes key rotation and deprecation periods based on the registered client configuration.
   */
  @PostConstruct
  public void init() {
    rotationPeriod =
        Duration.ofMinutes(registeredClientConfiguration.getAccessTokenMinutesTTL() * 4L);
    deprecationPeriod =
        Duration.ofMinutes(registeredClientConfiguration.getAccessTokenMinutesTTL());
  }

  /**
   * Extracts the region from the authorization code in the JWT encoding context.
   *
   * @param context the JWT encoding context
   * @return the region from the authorization code, or the default configured region if not found
   */
  private String getRegionFromContext(JwtEncodingContext context) {
    if (context.getAuthorization() != null) {
      var authCodeToken = context.getAuthorization().getToken(OAuth2AuthorizationCode.class);
      if (authCodeToken != null && authCodeToken.getToken() != null) {
        var extractedRegion =
            RegionUtils.extractFromPrefix(authCodeToken.getToken().getTokenValue());
        if (extractedRegion.isPresent()) return extractedRegion.get();
      }
    }

    return region != null ? region.toLowerCase() : "";
  }

  /**
   * Scheduled task for rotating and invalidating keys, running every 30 minutes.
   *
   * <p>This method ensures old keys are invalidated and new keys are generated if needed.
   */
  @EventListener(ApplicationReadyEvent.class)
  @Scheduled(fixedDelayString = "PT30M")
  @SchedulerLock(name = "key_rotation_task")
  public void scheduledKeyRotationAndCleanup() {
    try {
      rotateKeysIfNeeded();
      invalidateKeys();
    } catch (Exception e) {
      log.error("Critical error during key rotation and cleanup", e);
    }
  }

  /**
   * Retrieves JWKs matching the provided selector and security context.
   *
   * @param jwkSelector the {@link JWKSelector} for selecting keys.
   * @param securityContext the {@link SecurityContext}.
   * @return a list of matching {@link JWK}s.
   * @throws KeySourceException if an error occurs during key retrieval.
   */
  public List<JWK> get(JWKSelector jwkSelector, SecurityContext securityContext)
      throws KeySourceException {
    log.debug("Trying to get JWK");

    var remoteKeyPair = remoteKeyPairHolder.get();
    if (remoteKeyPair != null) {
      try {
        var remoteJwk = buildJwk(remoteKeyPair);
        if (remoteJwk != null && jwkSelector.getMatcher().matches(remoteJwk)) {
          log.debug("Using remote key pair for cross-region signing: {}", remoteKeyPair.getId());
          return List.of(remoteJwk);
        }
      } finally {
        remoteKeyPairHolder.remove();
      }
    }

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
   * Fetches the latest active key pair from a remote region via RPC.
   *
   * @param region the region to fetch the key from
   * @return the key pair from the remote region, or null if not available
   */
  private KeyPair fetchFromRemoteRegion(String region) {
    try {
      log.debug("Fetching key pair from remote region: {}", region);

      var routingKey =
          AuthorizationMessagingConfiguration.AUTHORIZATION_RPC_ROUTING_KEY_PREFIX + region;

      var request = RetrieveKeyPairMessage.builder().build();
      var response =
          rpcRabbitTemplate.sendAndReceive(
              AuthorizationMessagingConfiguration.AUTHORIZATION_RPC_EXCHANGE,
              routingKey,
              messageConverter.toMessage(request, new MessageProperties()));

      if (response == null) {
        log.warn("No response received from remote region: {}", region);
        return null;
      }

      var responseData = messageConverter.fromMessage(response);
      if (!(responseData instanceof KeyPairRetrievedEvent keyPairRetrievedEvent)) {
        log.error("Unexpected response type from remote region: {}", responseData.getClass());
        return null;
      }

      if (!keyPairRetrievedEvent.isSuccess()) return null;

      log.debug(
          "Successfully fetched key pair {} from region {}", keyPairRetrievedEvent.getId(), region);

      return KeyPair.builder()
          .id(keyPairRetrievedEvent.getId())
          .publicKey(keyPairRetrievedEvent.getPublicKey())
          .privateKey(encryptionService.decrypt(keyPairRetrievedEvent.getPrivateKey()))
          .pairType(KeyPairType.valueOf(keyPairRetrievedEvent.getPairType()))
          .build();
    } catch (Exception e) {
      log.error("Error fetching key pair from remote region: {}", region, e);
      return null;
    }
  }

  /**
   * Customizes the JWT encoding context with additional claims and header information.
   *
   * <p>Includes client ID, tenant details, region, issuer, and audience claims.
   *
   * @param context the {@link JwtEncodingContext}.
   */
  public void customize(JwtEncodingContext context) {
    var tokenRegion = getRegionFromContext(context);
    var isSaaS =
        Arrays.stream(environment.getActiveProfiles())
            .anyMatch(profile -> profile.equalsIgnoreCase("saas"));

    KeyPair activeKeyPair;
    if (isSaaS
        && tokenRegion != null
        && !tokenRegion.isBlank()
        && !tokenRegion.equalsIgnoreCase(region)) {
      log.info(
          "Cross-region token generation detected. Current region: {}, Token region: {}",
          region,
          tokenRegion);

      var cache = cacheManager.getCache("key_pair");
      var cached = cache.get(tokenRegion, KeyPair.class);

      if (cached != null) {
        log.debug("Found a key pair in cache");
        activeKeyPair = cached;
      } else {
        activeKeyPair = fetchFromRemoteRegion(tokenRegion);
        if (activeKeyPair != null) cache.put(tokenRegion, activeKeyPair);
        else throw new UnsupportedOperationException("Could not find any suitable keypair");
      }

      remoteKeyPairHolder.set(activeKeyPair);
    } else {
      activeKeyPair = getLatestActiveKeyPair();
    }

    if (activeKeyPair == null)
      throw new UnsupportedOperationException("Could not find any suitable keypair");

    log.debug("Using key pair with ID: {}", activeKeyPair.getId());

    var principal = context.getPrincipal();
    var authority = principal.getAuthorities().stream().findFirst().orElse(null);

    if (context.getRegisteredClient() != null
        && context.getRegisteredClient().getClientId() != null)
      context.getClaims().claim("cid", context.getRegisteredClient().getClientId());
    if (context.getAuthorization() != null
        && context.getAuthorization().getRegisteredClientId() != null)
      context.getClaims().claim("cid", context.getAuthorization().getRegisteredClientId());
    if (principal.getPrincipal() != null)
      context.getClaims().subject(principal.getPrincipal().toString());
    if (authority instanceof TenantAuthority tenantAuthority)
      context
          .getClaims()
          .issuer(String.format("%s/oauth2", tenantAuthority.getAuthority()))
          .claim("tid", tenantAuthority.getTenantId())
          .audience(Collections.singletonList(tenantAuthority.getAuthority()));

    if (tokenRegion != null && !tokenRegion.isBlank() && isSaaS)
      context.getClaims().claim("region", tokenRegion);

    context
        .getJwsHeader()
        .keyId(activeKeyPair.getId())
        .algorithm(
            activeKeyPair.getPairType().equals(KeyPairType.EC)
                ? SignatureAlgorithm.ES256
                : SignatureAlgorithm.RS256);
  }

  /**
   * Rotates keys if the latest key is older than the rotation period.
   *
   * @throws NoSuchAlgorithmException if key generation fails.
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

  /** Invalidates keys that are older than the combined rotation and deprecation periods. */
  protected void invalidateKeys() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    keyPairService.invalidateKeyPairs(cutoffTime);
  }

  /**
   * Retrieves the latest active key pair.
   *
   * @return the latest active {@link KeyPair}, or {@code null} if none are found.
   */
  private KeyPair getLatestActiveKeyPair() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    return keyPairService.findActiveKeyPairs(cutoffTime).stream()
        .max(Comparator.comparing(KeyPair::getCreatedAt))
        .orElse(null);
  }

  /**
   * Retrieves a set of active key pairs.
   *
   * @return a set of active {@link KeyPair}s.
   */
  private Set<KeyPair> getActiveKeyPairs() {
    var cutoffTime =
        ZonedDateTime.now(ZoneOffset.UTC).minus(rotationPeriod).minus(deprecationPeriod);
    return keyPairService.findActiveKeyPairs(cutoffTime);
  }

  /**
   * Generates a new key pair and stores it in the key pair service.
   *
   * @throws NoSuchAlgorithmException if key generation fails.
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
   * Builds a JWK from the provided key pair.
   *
   * @param keyPair the {@link KeyPair}.
   * @return the constructed {@link JWK}, or {@code null} if an error occurs.
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
