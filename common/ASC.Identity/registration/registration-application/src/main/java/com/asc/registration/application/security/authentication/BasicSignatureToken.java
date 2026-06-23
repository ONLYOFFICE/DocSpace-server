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

package com.asc.registration.application.security.authentication;

import java.util.Collection;
import lombok.Getter;
import org.springframework.security.authentication.AbstractAuthenticationToken;
import org.springframework.security.core.GrantedAuthority;
import org.springframework.security.core.authority.AuthorityUtils;
import org.springframework.util.Assert;

/** Custom authentication token for ASC application. */
@Getter
public class BasicSignatureToken extends AbstractAuthenticationToken {
  private final BasicSignatureTokenPrincipal principal;
  private String credentials;

  /**
   * Constructs an unauthenticated token with the specified credentials (signature).
   *
   * @param credentials the credentials.
   */
  public BasicSignatureToken(String credentials) {
    super(AuthorityUtils.NO_AUTHORITIES);
    this.principal = null;
    this.credentials = credentials;
    this.setAuthenticated(false);
  }

  /**
   * Constructs an authenticated token with the specified principal, credentials, and authorities.
   *
   * @param principal the principal.
   * @param credentials the credentials.
   * @param authorities the authorities.
   */
  public BasicSignatureToken(
      BasicSignatureTokenPrincipal principal,
      String credentials,
      Collection<? extends GrantedAuthority> authorities) {
    super(authorities);
    this.principal = principal;
    this.credentials = credentials;
    super.setAuthenticated(true);
  }

  /**
   * Factory method to create an unauthenticated token.
   *
   * @param credentials the credentials.
   * @return the unauthenticated token.
   */
  public static BasicSignatureToken unauthenticated(String credentials) {
    return new BasicSignatureToken(credentials);
  }

  /**
   * Factory method to create an authenticated token.
   *
   * @param principal the principal.
   * @param credentials the credentials.
   * @param authorities the authorities.
   * @return the authenticated token.
   */
  public static BasicSignatureToken authenticated(
      BasicSignatureTokenPrincipal principal,
      String credentials,
      Collection<? extends GrantedAuthority> authorities) {
    return new BasicSignatureToken(principal, credentials, authorities);
  }

  /**
   * Sets the authentication status of this token. This method can only set the token to
   * unauthenticated.
   *
   * @param isAuthenticated the authentication status.
   * @throws IllegalArgumentException if attempting to set the token to authenticated.
   */
  public void setAuthenticated(boolean isAuthenticated) throws IllegalArgumentException {
    Assert.isTrue(
        !isAuthenticated,
        "Cannot set this token to trusted - use constructor which takes a GrantedAuthority list instead");
    super.setAuthenticated(false);
  }

  /** Erases the credentials stored in this token. */
  public void eraseCredentials() {
    super.eraseCredentials();
    this.credentials = null;
  }
}
