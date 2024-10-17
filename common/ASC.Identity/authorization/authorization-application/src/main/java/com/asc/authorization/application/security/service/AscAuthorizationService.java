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

package com.asc.authorization.application.security.service;

import com.asc.authorization.application.exception.authorization.AuthorizationCleanupException;
import com.asc.authorization.application.exception.authorization.AuthorizationPersistenceException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.dao.CannotAcquireLockException;
import org.springframework.security.oauth2.server.authorization.OAuth2Authorization;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationService;
import org.springframework.security.oauth2.server.authorization.OAuth2TokenType;
import org.springframework.stereotype.Service;

/**
 * Service to handle OAuth2 Authorization operations including saving, removing, and finding
 * authorizations.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AscAuthorizationService implements OAuth2AuthorizationService {
  private final AscTransactionalAuthorizationService transactionalAuthorizationService;

  /**
   * Saves an OAuth2 authorization.
   *
   * @param authorization the authorization to save
   * @throws AuthorizationPersistenceException if an error occurs during saving
   */
  public void save(OAuth2Authorization authorization) {
    try {
      transactionalAuthorizationService.save(authorization);
    } catch (CannotAcquireLockException e) {
      throw new AuthorizationPersistenceException(e);
    }
  }

  /**
   * Removes an OAuth2 authorization.
   *
   * @param authorization the authorization to remove
   * @throws AuthorizationCleanupException if an error occurs during removal
   */
  public void remove(OAuth2Authorization authorization) {
    transactionalAuthorizationService.remove(authorization);
  }

  /**
   * Finds an OAuth2 authorization by its ID.
   *
   * @param id the ID of the authorization
   * @return the found authorization, or null if not found
   */
  public OAuth2Authorization findById(String id) {
    return transactionalAuthorizationService.findById(id);
  }

  /**
   * Finds an OAuth2 authorization by its token.
   *
   * @param token the token of the authorization
   * @param tokenType the type of the token
   * @return the found authorization, or null if not found
   */
  public OAuth2Authorization findByToken(String token, OAuth2TokenType tokenType) {
    return transactionalAuthorizationService.findByToken(token, tokenType);
  }
}
