package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.data.key.entity.KeyPair;
import com.asc.authorization.data.key.repository.JpaKeyPairRepository;
import com.asc.common.utilities.crypto.EncryptionService;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.stream.Collectors;
import lombok.AllArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/** Service for managing key pairs, including saving, retrieving, and invalidating key pairs. */
@Slf4j
@Service
@AllArgsConstructor
public class AscKeyPairService {
  private final JpaKeyPairRepository jpaKeyPairRepository;

  private final EncryptionService encryptionService;

  /**
   * Retrieves active key pairs that have been created before the specified cutoff time.
   *
   * @param cutoff the cutoff time for retrieving active key pairs.
   * @return a set of active key pairs.
   */
  public Set<KeyPair> findActiveKeyPairs(ZonedDateTime cutoff) {
    return jpaKeyPairRepository.findActiveKeyPairs(cutoff).stream()
        .map(this::decryptPrivateKey)
        .collect(Collectors.toSet());
  }

  /**
   * Saves a key pair to the repository, encrypting the private key before saving.
   *
   * @param keyPair the key pair to save.
   * @return the saved key pair.
   */
  @Transactional
  public KeyPair saveKeyPair(KeyPair keyPair) {
    return jpaKeyPairRepository.save(
        KeyPair.builder()
            .publicKey(keyPair.getPublicKey())
            .privateKey(encryptionService.encrypt(keyPair.getPrivateKey()))
            .pairType(keyPair.getPairType())
            .createdAt(keyPair.getCreatedAt())
            .build());
  }

  /**
   * Invalidates key pairs that were created before the specified cutoff time.
   *
   * @param cutoff the cutoff time for invalidating key pairs.
   */
  @Transactional
  public void invalidateKeyPairs(ZonedDateTime cutoff) {
    jpaKeyPairRepository.invalidateKeyPairs(cutoff);
  }

  /**
   * Decrypts the private key of the specified key pair.
   *
   * @param pair the key pair to decrypt.
   * @return the key pair with the decrypted private key.
   */
  private KeyPair decryptPrivateKey(KeyPair pair) {
    pair.setPrivateKey(encryptionService.decrypt(pair.getPrivateKey()));
    return pair;
  }
}
