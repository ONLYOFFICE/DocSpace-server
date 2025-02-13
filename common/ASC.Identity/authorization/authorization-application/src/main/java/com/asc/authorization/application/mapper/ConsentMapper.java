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
