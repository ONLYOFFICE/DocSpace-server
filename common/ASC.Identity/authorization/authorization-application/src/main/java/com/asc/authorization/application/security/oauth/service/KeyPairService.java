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

import com.asc.authorization.data.key.entity.KeyPair;
import com.asc.authorization.data.key.repository.JpaKeyPairRepository;
import com.asc.common.utilities.crypto.EncryptionService;
import java.time.ZonedDateTime;
import java.util.Set;
import java.util.stream.Collectors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Service for managing cryptographic key pairs.
 *
 * <p>This service provides functionality for saving, retrieving, and invalidating key pairs. It
 * ensures the private keys are securely encrypted during storage and decrypted during retrieval.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class KeyPairService {
  private final EncryptionService encryptionService;
  private final JpaKeyPairRepository jpaKeyPairRepository;

  /**
   * Retrieves active key pairs created before a specified cutoff time.
   *
   * <p>Active key pairs are retrieved from the database and their private keys are decrypted before
   * being returned.
   *
   * @param cutoff the cutoff time for filtering active key pairs.
   * @return a set of active {@link KeyPair} objects.
   */
  public Set<KeyPair> findActiveKeyPairs(ZonedDateTime cutoff) {
    return jpaKeyPairRepository.findActiveKeyPairs(cutoff).stream()
        .map(this::decryptPrivateKey)
        .collect(Collectors.toSet());
  }

  /**
   * Saves a key pair to the repository with an encrypted private key.
   *
   * <p>This method ensures that private keys are encrypted before they are persisted in the
   * database.
   *
   * @param keyPair the {@link KeyPair} to save.
   * @return the saved {@link KeyPair}.
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
   * Invalidates key pairs created before a specified cutoff time.
   *
   * <p>Invalidated key pairs are marked as inactive in the database and can no longer be used.
   *
   * @param cutoff the cutoff time for invalidating key pairs.
   */
  @Transactional
  public void invalidateKeyPairs(ZonedDateTime cutoff) {
    jpaKeyPairRepository.invalidateKeyPairs(cutoff);
  }

  /**
   * Decrypts the private key of the given key pair.
   *
   * <p>This method ensures the private key is in plaintext for operations that require it, while
   * maintaining secure storage in the database.
   *
   * @param pair the {@link KeyPair} to decrypt.
   * @return the {@link KeyPair} with its private key decrypted.
   */
  private KeyPair decryptPrivateKey(KeyPair pair) {
    pair.setPrivateKey(encryptionService.decrypt(pair.getPrivateKey()));
    return pair;
  }
}
