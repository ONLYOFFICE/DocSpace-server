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

package com.asc.authorization.application.security.oauth.services;

import com.asc.authorization.application.mapper.ConsentMapper;
import com.asc.common.data.consent.entity.ConsentEntity;
import com.asc.common.data.consent.repository.JpaConsentRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsentService;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

/**
 * Service to handle OAuth2 Authorization Consent operations including saving, removing, and finding
 * consents.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class AscConsentService implements OAuth2AuthorizationConsentService {
  private final JpaConsentRepository jpaConsentRepository;
  private final ConsentMapper consentMapper;

  /**
   * Saves the given OAuth2AuthorizationConsent object.
   *
   * @param authorizationConsent the OAuth2AuthorizationConsent object to save.
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
   * Removes the given OAuth2AuthorizationConsent object by its registered client ID and principal
   * name.
   *
   * @param authorizationConsent the OAuth2AuthorizationConsent object to remove.
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
   * Finds an OAuth2AuthorizationConsent object by its registered client ID and principal name.
   *
   * @param registeredClientId the registered client ID.
   * @param principalName the principal name.
   * @return the found OAuth2AuthorizationConsent object, or null if not found.
   */
  public OAuth2AuthorizationConsent findById(String registeredClientId, String principalName) {
    return jpaConsentRepository
        .findById(new ConsentEntity.ConsentId(registeredClientId, principalName))
        .filter(c -> !c.isInvalidated())
        .map(consentMapper::toConsent)
        .orElse(null);
  }
}
