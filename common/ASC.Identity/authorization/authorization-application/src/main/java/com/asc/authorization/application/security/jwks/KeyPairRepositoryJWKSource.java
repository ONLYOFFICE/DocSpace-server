package com.asc.authorization.application.security.jwks;

import com.asc.authorization.application.mapper.KeyPairMapper;
import com.asc.authorization.application.security.oauth.authorities.TenantAuthority;
import com.asc.authorization.data.key.entity.KeyPair;
import com.asc.authorization.data.key.repository.JpaKeyPairRepository;
import com.asc.common.core.domain.value.KeyPairType;
import com.asc.common.utilities.crypto.EncryptionService;
import com.nimbusds.jose.KeySourceException;
import com.nimbusds.jose.jwk.JWK;
import com.nimbusds.jose.jwk.JWKSelector;
import com.nimbusds.jose.jwk.source.JWKSource;
import com.nimbusds.jose.proc.SecurityContext;
import jakarta.annotation.PostConstruct;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.stream.StreamSupport;
import lombok.AllArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Qualifier;
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
  @Autowired
  @Qualifier("rsa")
  private JwksKeyPairGenerator keyPairGenerator;

  private final EncryptionService encryptionService;
  private final JpaKeyPairRepository jpaKeyPairRepository;
  private final KeyPairMapper keyPairMapper;

  private List<KeyPair> keyPairs = new ArrayList<>();

  /**
   * Initializes the key pairs from the repository, generating a new one if none are found.
   *
   * @throws Exception If an error occurs during initialization.
   */
  @PostConstruct
  public void init() throws Exception {
    keyPairs.addAll(
        StreamSupport.stream(jpaKeyPairRepository.findAll().spliterator(), false)
            .filter(p -> p.getPairType().equals(keyPairGenerator.type()))
            .map(
                pair -> {
                  // Decrypt the private key for internal use
                  pair.setPrivateKey(encryptionService.decrypt(pair.getPrivateKey()));
                  return pair;
                })
            .filter(
                p -> {
                  try {
                    keyPairGenerator.buildKey(p.getId(), p.getPublicKey(), p.getPrivateKey());
                    return true;
                  } catch (Exception e) {
                    log.error("Invalid key pair in repository: {}", p.getId(), e);
                    return false;
                  }
                })
            .toList());

    if (keyPairs.isEmpty()) {
      MDC.put("type", keyPairGenerator.type().name());
      log.debug("Found no certificates of this type. Generating a new one");
      MDC.clear();
      var pair = keyPairGenerator.generateKeyPair();
      var keyPair =
          KeyPair.builder()
              .publicKey(keyPairMapper.toString(pair.getPublic()))
              .privateKey(encryptionService.encrypt(keyPairMapper.toString(pair.getPrivate())))
              .pairType(keyPairGenerator.type())
              .build();
      // Save the encrypted private key to the repository
      jpaKeyPairRepository.save(keyPair);
      // Add the key pair with the decrypted private key for internal use
      keyPairs.add(
          KeyPair.builder()
              .id(keyPair.getId())
              .publicKey(keyPairMapper.toString(pair.getPublic()))
              .privateKey(keyPairMapper.toString(pair.getPrivate()))
              .pairType(keyPairGenerator.type())
              .build());
    }
  }

  /**
   * Retrieves a list of JWKs that match the given JWK selector and security context.
   *
   * @param jwkSelector The JWK selector to filter the keys.
   * @param securityContext The security context.
   * @return A list of JWKs matching the selector.
   * @throws KeySourceException If an error occurs while retrieving the keys.
   */
  @SneakyThrows
  public List<JWK> get(JWKSelector jwkSelector, SecurityContext securityContext)
      throws KeySourceException {
    log.debug("Trying to get JWK");
    List<JWK> result = new ArrayList<>();
    for (var keyPair : keyPairs) {
      MDC.put("id", keyPair.getId());
      MDC.put("type", keyPair.getPairType().name());
      log.debug("Validating a key pair");
      if (!keyPair.getPairType().equals(keyPairGenerator.type())) {
        log.debug("Key pair is not supported");
        MDC.clear();
        continue;
      }
      log.debug("Key pair is supported");
      MDC.clear();
      var key =
          keyPairGenerator.buildKey(
              keyPair.getId(), keyPair.getPublicKey(), keyPair.getPrivateKey());
      if (jwkSelector.getMatcher().matches(key)) result.add(key);
    }

    return result;
  }

  /**
   * Customizes the JWT encoding context with the appropriate key and claims.
   *
   * @param context The JWT encoding context.
   */
  public void customize(JwtEncodingContext context) {
    var keyPair =
        keyPairs.stream()
            .filter(p -> p.getPairType().equals(keyPairGenerator.type()))
            .findFirst()
            .orElseThrow(
                () -> new UnsupportedOperationException("Could not find any suitable keypair"));

    log.debug("Using key pair with ID: {}", keyPair.getId());

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
        .keyId(keyPair.getId())
        .algorithm(
            keyPair.getPairType().equals(KeyPairType.EC)
                ? SignatureAlgorithm.ES256
                : SignatureAlgorithm.RS256);
  }
}
