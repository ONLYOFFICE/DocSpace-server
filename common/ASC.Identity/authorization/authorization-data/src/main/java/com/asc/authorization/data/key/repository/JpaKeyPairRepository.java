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

package com.asc.authorization.data.key.repository;

import com.asc.authorization.data.key.entity.KeyPair;
import java.time.ZonedDateTime;
import java.util.Set;
import org.springframework.data.jpa.repository.Modifying;
import org.springframework.data.jpa.repository.Query;
import org.springframework.data.repository.CrudRepository;
import org.springframework.data.repository.query.Param;

/**
 * Repository interface for managing {@link KeyPair} entities. This interface extends {@link
 * CrudRepository} to provide basic CRUD operations, and it defines custom queries for managing and
 * retrieving key pairs.
 */
public interface JpaKeyPairRepository extends CrudRepository<KeyPair, String> {

  /**
   * Retrieves all key pairs created after the specified cutoff timestamp.
   *
   * @param cutoff The cutoff timestamp. Only key pairs created after this timestamp are included.
   * @return A {@link Set} of {@link KeyPair} objects that are considered active.
   */
  @Query("SELECT kp FROM KeyPair kp WHERE kp.createdAt > :cutoff")
  Set<KeyPair> findActiveKeyPairs(@Param("cutoff") ZonedDateTime cutoff);

  /**
   * Deletes all key pairs created before the specified cutoff timestamp. This method is used to
   * invalidate or remove outdated key pairs.
   *
   * @param cutoff The cutoff timestamp. Key pairs created before this timestamp are deleted.
   */
  @Modifying
  @Query("DELETE FROM KeyPair kp WHERE kp.createdAt < :cutoff")
  void invalidateKeyPairs(@Param("cutoff") ZonedDateTime cutoff);
}
