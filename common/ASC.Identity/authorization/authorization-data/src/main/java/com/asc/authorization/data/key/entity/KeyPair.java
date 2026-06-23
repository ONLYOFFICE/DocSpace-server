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

package com.asc.authorization.data.key.entity;

import com.asc.common.core.domain.value.KeyPairType;
import jakarta.persistence.*;
import java.time.ZonedDateTime;
import lombok.*;
import org.hibernate.annotations.UuidGenerator;
import org.springframework.data.annotation.CreatedDate;

/**
 * Entity class representing a cryptographic key pair. This class is mapped to the `identity_certs`
 * table and stores data related to public-private key pairs.
 */
@Entity
@Getter
@Setter
@Builder
@NoArgsConstructor
@AllArgsConstructor
@Table(name = "identity_certs")
public class KeyPair {

  /** The unique identifier for the key pair. Automatically generated as a UUID. */
  @Id @UuidGenerator private String id;

  /** The public key component of the key pair. This field is required and cannot be null. */
  @Column(name = "public_key", nullable = false)
  private String publicKey;

  /** The private key component of the key pair. This field is required and cannot be null. */
  @Column(name = "private_key", nullable = false)
  private String privateKey;

  /**
   * The type of the key pair, represented as an enumerated value. Stored in the database as an
   * ordinal value.
   */
  @Column(name = "pair_type", nullable = false)
  @Enumerated(EnumType.ORDINAL)
  private KeyPairType pairType;

  /**
   * The timestamp indicating when the key pair was created. Automatically set at the time of entity
   * creation.
   */
  @Column(name = "created_at")
  @CreatedDate
  private ZonedDateTime createdAt;

  /**
   * Lifecycle callback method invoked before persisting the entity. Ensures the `createdAt`
   * timestamp is set to the current time.
   */
  @PrePersist
  void fillAction() {
    createdAt = ZonedDateTime.now();
  }
}
