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
