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

package com.asc.authorization.application.security.manager;

import java.util.List;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;

/**
 * Custom Authentication Manager that delegates authentication to a list of {@link
 * AuthenticationProvider}s.
 *
 * <p>This class implements {@link AuthenticationManager} and iterates over a list of providers to
 * authenticate a given authentication request. If no provider supports the authentication type, the
 * authentication attempt fails.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class SignatureAuthenticationManager implements AuthenticationManager {
  /** Error message logged when no provider supports the authentication type. */
  private static final String UNSUPPORTED_ERROR =
      "Authentication type is not supported by any authentication provider";

  private final List<AuthenticationProvider> providers;

  /**
   * Attempts to authenticate the provided authentication object using the configured providers.
   *
   * <p>This method iterates through the list of {@link AuthenticationProvider}s and delegates the
   * authentication to the first provider that supports the authentication type. If none of the
   * providers support the authentication, the method logs the failure and returns {@code null}.
   *
   * @param authentication the {@link Authentication} request object.
   * @return a fully authenticated {@link Authentication} object including credentials, or {@code
   *     null} if no provider supports the authentication.
   * @throws AuthenticationException if an authentication error occurs.
   */
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    for (AuthenticationProvider provider : providers) {
      MDC.put("provider", provider.getClass().getName());
      MDC.put("authentication", authentication.getClass().toString());
      log.debug("Checking an authentication provider");

      if (provider.supports(authentication.getClass())) {
        log.debug("Provider is supported");
        MDC.clear();

        return provider.authenticate(authentication);
      }

      MDC.clear();
    }

    log.debug(UNSUPPORTED_ERROR);
    return null;
  }
}
