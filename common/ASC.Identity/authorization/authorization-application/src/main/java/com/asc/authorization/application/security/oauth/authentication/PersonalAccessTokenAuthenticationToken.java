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

package com.asc.authorization.application.security.oauth.authentication;

import com.asc.authorization.application.security.oauth.grant.ExtendedAuthorizationGrantType;
import java.util.Collections;
import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import lombok.Getter;
import org.springframework.lang.Nullable;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationGrantAuthenticationToken;

/**
 * Authentication token for the Personal Access Token authorization grant.
 *
 * <p>This token represents an authentication request or an authenticated principal for the Personal
 * Access Token grant type. It encapsulates user and tenant details, requested scopes, and
 * additional parameters for the authorization request.
 */
@Getter
public class PersonalAccessTokenAuthenticationToken
    extends OAuth2AuthorizationGrantAuthenticationToken {

  /** The user ID associated with the personal access token. */
  private final String userId;

  /** The username associated with the personal access token. */
  private final String userName;

  /** The user email associated with the personal access token. */
  private final String userEmail;

  /** The tenant ID associated with the personal access token. */
  private final long tenantId;

  /** The tenant URL associated with the personal access token. */
  private final String tenantUrl;

  /** The requested scopes for the personal access token. */
  private final Set<String> scopes;

  /**
   * Constructs a new {@code PersonalAccessTokenAuthenticationToken}.
   *
   * <p>This constructor initializes the token with details about the client, user, tenant, and
   * additional authorization parameters. The scopes are converted into an immutable set.
   *
   * @param clientPrincipal the authenticated client principal.
   * @param scopes the requested scopes (nullable).
   * @param userId the user ID associated with the personal access token.
   * @param userName the username associated with the personal access token.
   * @param userEmail the user email associated with the personal access token.
   * @param tenantId the tenant ID associated with the personal access token.
   * @param tenantUrl the tenant URL associated with the personal access token.
   * @param additionalParameters additional parameters for the authorization request (nullable).
   */
  public PersonalAccessTokenAuthenticationToken(
      Authentication clientPrincipal,
      @Nullable Set<String> scopes,
      String userId,
      String userName,
      String userEmail,
      long tenantId,
      String tenantUrl,
      @Nullable Map<String, Object> additionalParameters) {
    super(
        ExtendedAuthorizationGrantType.PERSONAL_ACCESS_TOKEN,
        clientPrincipal,
        additionalParameters);
    this.scopes =
        Collections.unmodifiableSet(
            scopes != null ? new HashSet<>(scopes) : Collections.emptySet());
    this.userId = userId;
    this.userName = userName;
    this.userEmail = userEmail;
    this.tenantId = tenantId;
    this.tenantUrl = tenantUrl;
  }
}
