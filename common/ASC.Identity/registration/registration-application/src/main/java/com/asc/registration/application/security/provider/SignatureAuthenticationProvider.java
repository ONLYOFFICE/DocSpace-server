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

package com.asc.registration.application.security.provider;

import com.asc.common.core.domain.value.Role;
import com.asc.registration.application.security.authentication.BasicSignature;
import com.asc.registration.application.security.authentication.BasicSignatureToken;
import com.asc.registration.application.security.authentication.BasicSignatureTokenPrincipal;
import com.asc.registration.application.security.service.SignatureService;
import java.util.List;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Qualifier;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.stereotype.Component;

/**
 * An {@link AuthenticationProvider} implementation that authenticates requests using a basic
 * signature token.
 *
 * <p>This provider validates the signature provided in the {@link BasicSignatureToken}, extracts
 * user details, and assigns appropriate roles based on the user's administrative status. If the
 * signature or credentials are invalid, an {@link AuthenticationException} is thrown.
 */
@Slf4j
@Component("registrationSignatureAuthenticationProvider")
public class SignatureAuthenticationProvider implements AuthenticationProvider {
  private final SignatureService signatureService;

  public SignatureAuthenticationProvider(
      @Qualifier("registrationSignatureService") SignatureService signatureService) {
    this.signatureService = signatureService;
  }

  /**
   * Authenticates the provided request using a signature token.
   *
   * <p>This method validates the signature, extracts user details, and creates a new {@link
   * BasicSignatureToken} with the authenticated user's information and authorities.
   *
   * @param authentication the {@link Authentication} request object containing the credentials.
   * @return a fully authenticated {@link Authentication} object containing user details and roles.
   * @throws AuthenticationException if authentication fails due to invalid credentials or errors.
   */
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    log.info("Trying to authenticate a user");

    var token = authentication.getCredentials();
    if (token == null || token.toString().isBlank())
      throw new BadCredentialsException("Could not extract authentication signature");

    var signature = signatureService.validate(token.toString(), BasicSignature.class);
    validateSignature(signature);

    var role =
        signature.isAdmin()
            ? Role.ROLE_ADMIN
            : signature.isGuest()
                ? Role.ROLE_GUEST
                : signature.isPublic() ? Role.ROLE_USER : Role.ROLE_GUEST;
    return new BasicSignatureToken(
        new BasicSignatureTokenPrincipal(
            signature.getUserId(),
            signature.getUserName(),
            signature.getUserEmail(),
            signature.getTenantId(),
            signature.getTenantUrl(),
            role),
        token.toString(),
        List.of(new SimpleGrantedAuthority(role.name())));
  }

  /**
   * Validates the {@link BasicSignature} object for required fields.
   *
   * <p>This method ensures that all necessary fields are present in the signature and throws a
   * {@link BadCredentialsException} if any validation fails.
   *
   * @param signature the {@link BasicSignature} object to validate.
   * @throws BadCredentialsException if any required field is missing or invalid.
   */
  private void validateSignature(BasicSignature signature) {
    if (signature.getUserId() == null || signature.getUserId().isBlank())
      throw new BadCredentialsException("Authentication signature does not contain userId");
    if (signature.getUserName() == null || signature.getUserName().isBlank())
      throw new BadCredentialsException("Authentication signature does not contain userName");
    if (signature.getUserEmail() == null || signature.getUserEmail().isBlank())
      throw new BadCredentialsException("Authentication signature does not contain userEmail");
    if (signature.getTenantId() < 1)
      throw new BadCredentialsException(
          "Authentication signature does not contain a valid tenantId");
    if (signature.getTenantUrl() == null || signature.getTenantUrl().isBlank())
      throw new BadCredentialsException("Authentication signature does not contain tenantUrl");
  }

  /**
   * Checks if this {@link AuthenticationProvider} supports the provided authentication type.
   *
   * <p>This implementation supports authentication requests of type {@link BasicSignatureToken}.
   *
   * @param authentication the class of the authentication object.
   * @return {@code true} if the authentication type is supported; {@code false} otherwise.
   */
  public boolean supports(Class<?> authentication) {
    return BasicSignatureToken.class.equals(authentication);
  }
}
