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

package com.asc.registration.application.security.authentication;

import java.util.Collection;
import lombok.Getter;
import org.springframework.security.authentication.AbstractAuthenticationToken;
import org.springframework.security.core.GrantedAuthority;
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
    super(null);
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
