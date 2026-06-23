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

package com.asc.authorization.application.mapper;

import com.asc.authorization.data.consent.entity.ConsentEntity;
import java.time.ZonedDateTime;
import org.springframework.security.oauth2.server.authorization.OAuth2AuthorizationConsent;
import org.springframework.stereotype.Component;

/**
 * Mapper class for converting between {@link ConsentEntity} and {@link OAuth2AuthorizationConsent}.
 *
 * <p>This class provides utility methods to map data between the consent entity used for
 * persistence ({@link ConsentEntity}) and the OAuth2 consent model ({@link
 * OAuth2AuthorizationConsent}).
 */
@Component
public class ConsentMapper {
  /**
   * Converts an {@link OAuth2AuthorizationConsent} to a {@link ConsentEntity}.
   *
   * <p>This method maps the OAuth2 consent data into a database entity, including the registered
   * client ID, principal name, scopes, and additional metadata such as the modification timestamp.
   *
   * @param authorizationConsent the {@link OAuth2AuthorizationConsent} to convert.
   * @return the corresponding {@link ConsentEntity}.
   */
  public ConsentEntity toEntity(OAuth2AuthorizationConsent authorizationConsent) {
    return ConsentEntity.builder()
        .registeredClientId(authorizationConsent.getRegisteredClientId())
        .principalId(authorizationConsent.getPrincipalName())
        .scopes(authorizationConsent.getScopes())
        .modifiedAt(ZonedDateTime.now())
        .invalidated(false)
        .build();
  }

  /**
   * Converts a {@link ConsentEntity} to an {@link OAuth2AuthorizationConsent}.
   *
   * <p>This method constructs an OAuth2AuthorizationConsent object from the consent entity,
   * including its associated registered client ID, principal ID, and scopes.
   *
   * @param consent the {@link ConsentEntity} to convert.
   * @return the corresponding {@link OAuth2AuthorizationConsent}.
   */
  public OAuth2AuthorizationConsent toConsent(ConsentEntity consent) {
    var registeredClientId = consent.getRegisteredClientId();
    var builder = OAuth2AuthorizationConsent.withId(registeredClientId, consent.getPrincipalId());
    consent.getScopes().forEach(builder::scope);
    return builder.build();
  }
}
