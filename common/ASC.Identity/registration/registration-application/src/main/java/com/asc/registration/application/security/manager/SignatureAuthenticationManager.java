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

package com.asc.registration.application.security.manager;

import java.util.List;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;

/**
 * Custom Authentication Manager that delegates authentication to a list of authentication
 * providers.
 */
@Slf4j
@Component("registrationSignatureAuthenticationManager")
public class SignatureAuthenticationManager implements AuthenticationManager {
  private static final String UNSUPPORTED_ERROR =
      "Authentication type is not supported by any authentication provider";
  private final List<AuthenticationProvider> providers;

  public SignatureAuthenticationManager(
      @Qualifier("registrationSignatureAuthenticationProvider")
          AuthenticationProvider signatureProvider) {
    this.providers = List.of(signatureProvider);
  }

  /**
   * Attempts to authenticate the provided authentication object using the configured providers.
   *
   * @param authentication the authentication request object.
   * @return a fully authenticated object including credentials.
   * @throws AuthenticationException if authentication fails.
   */
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    for (AuthenticationProvider provider : providers) {
      MDC.put("provider", provider.getClass().getName());
      log.debug("Checking an authentication provider");

      if (provider.supports(authentication.getClass())) {
        log.debug("Provider is supported");
        MDC.clear();

        return provider.authenticate(authentication);
      }

      MDC.clear();
    }

    log.error(UNSUPPORTED_ERROR);
    throw new BadCredentialsException(UNSUPPORTED_ERROR);
  }
}
