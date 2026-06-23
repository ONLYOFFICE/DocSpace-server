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

import com.asc.authorization.application.mapper.ConsentMapper;
import com.asc.authorization.data.consent.entity.ConsentEntity;
import com.asc.authorization.data.consent.repository.JpaConsentRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsentService;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Service for managing OAuth2 Authorization Consents.
 *
 * <p>This service provides functionality to save, remove, and find OAuth2AuthorizationConsent
 * objects. It handles persistence operations using a JPA repository and maps entities to domain
 * objects using a {@link ConsentMapper}.
 */
@Slf4j
@RequiredArgsConstructor
@Service("authorizationConsentService")
public class ConsentService implements OAuth2AuthorizationConsentService {
  private final JpaConsentRepository jpaConsentRepository;
  private final ConsentMapper consentMapper;

  /**
   * Saves the provided OAuth2AuthorizationConsent.
   *
   * <p>This method persists the consent information in the database. It logs the registered client
   * ID and principal name for debugging purposes and clears the diagnostic context after execution.
   *
   * @param authorizationConsent the {@link OAuth2AuthorizationConsent} object to save.
   */
  @Transactional(
      timeout = 2,
      rollbackFor = {Exception.class})
  public void save(OAuth2AuthorizationConsent authorizationConsent) {
    try {
      MDC.put("client_id", authorizationConsent.getRegisteredClientId());
      MDC.put("principal_name", authorizationConsent.getPrincipalName());
      log.info("Saving an authorization consent");

      jpaConsentRepository.save(consentMapper.toEntity(authorizationConsent));
    } catch (Exception e) {
      log.error("Failed to save an authorization consent", e);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Removes the provided OAuth2AuthorizationConsent.
   *
   * <p>This method deletes the consent record associated with the given registered client ID and
   * principal name. It logs relevant details and clears the diagnostic context after execution.
   *
   * @param authorizationConsent the {@link OAuth2AuthorizationConsent} object to remove.
   */
  @Transactional(
      timeout = 2,
      rollbackFor = {Exception.class})
  public void remove(OAuth2AuthorizationConsent authorizationConsent) {
    try {
      MDC.put("client_id", authorizationConsent.getRegisteredClientId());
      MDC.put("principal_name", authorizationConsent.getPrincipalName());
      log.info("Removing an authorization consent");

      jpaConsentRepository.deleteById(
          new ConsentEntity.ConsentId(
              authorizationConsent.getRegisteredClientId(),
              authorizationConsent.getPrincipalName()));
    } catch (Exception e) {
      log.error("Failed to remove an authorization consent", e);
    } finally {
      MDC.clear();
    }
  }

  /**
   * Finds an OAuth2AuthorizationConsent by its registered client ID and principal name.
   *
   * <p>This method retrieves a consent record from the database if it exists and is not marked as
   * invalidated. It maps the retrieved entity to a domain object using the {@link ConsentMapper}.
   *
   * @param registeredClientId the registered client ID associated with the consent.
   * @param principalName the principal name associated with the consent.
   * @return the {@link OAuth2AuthorizationConsent} object if found and valid, or {@code null} if
   *     not found or invalidated.
   */
  public OAuth2AuthorizationConsent findById(String registeredClientId, String principalName) {
    return jpaConsentRepository
        .findById(new ConsentEntity.ConsentId(registeredClientId, principalName))
        .filter(c -> !c.isInvalidated())
        .map(consentMapper::toConsent)
        .orElse(null);
  }
}
