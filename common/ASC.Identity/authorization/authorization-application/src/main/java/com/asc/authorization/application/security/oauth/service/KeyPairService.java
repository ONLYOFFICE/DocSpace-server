// Copyright (C) Ascensio System SIA, 2009-2026
//
// This program is a free software product. You can redistribute it and/or
// modify it under the terms of the GNU Affero General Public License (AGPL)
// version 3 as published by the Free Software Foundation, together with the
// additional terms provided in the LICENSE file.
//
// This program is distributed WITHOUT ANY WARRANTY; without even the implied
// warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. For
// details, see the GNU AGPL at: https://www.gnu.org/licenses/agpl-3.0.html
//
// You can contact Ascensio System SIA by email at info@onlyoffice.com
// or by postal mail at 20A-6 Ernesta Birznieka-Upisha Street, Riga,
// LV-1050, Latvia, European Union.
//
// The interactive user interfaces in modified versions of the Program
// are required to display Appropriate Legal Notices in accordance with
// Section 5 of the GNU AGPL version 3.
//
// No trademark rights are granted under this License.
//
// All non-code elements of the Product, including illustrations,
// icon sets, and technical writing content, are licensed under the
// Creative Commons Attribution-ShareAlike 4.0 International License:
// https://creativecommons.org/licenses/by-sa/4.0/legalcode
//
// This license applies only to such non-code elements and does not
// modify or replace the licensing terms applicable to the Program's
// source code, which remains licensed under the GNU Affero General
// Public License v3.
//
// SPDX-License-Identifier: AGPL-3.0-only

package com.asc.authorization.application.security.oauth.service;

import com.asc.authorization.data.key.entity.KeyPair;
import com.asc.authorization.data.key.repository.JpaKeyPairRepository;
import com.asc.common.utilities.crypto.DecryptionException;
import com.asc.common.utilities.crypto.EncryptionService;
import java.time.ZonedDateTime;
import java.util.Objects;
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
        .filter(Objects::nonNull)
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
    try {
      pair.setPrivateKey(encryptionService.decrypt(pair.getPrivateKey()));
      return pair;
    } catch (DecryptionException e) {
      return null;
    }
  }
}
