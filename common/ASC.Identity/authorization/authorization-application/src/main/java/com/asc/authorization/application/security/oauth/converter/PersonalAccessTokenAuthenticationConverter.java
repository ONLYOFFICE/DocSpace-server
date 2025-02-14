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

package com.asc.authorization.application.security.oauth.converter;

import com.asc.authorization.application.configuration.properties.SecurityConfigurationProperties;
import com.asc.authorization.application.security.authentication.BasicSignature;
import com.asc.authorization.application.security.oauth.authentication.PersonalAccessTokenAuthenticationToken;
import com.asc.authorization.application.security.oauth.grant.ExtendedAuthorizationGrantType;
import com.asc.authorization.application.security.service.SignatureService;
import jakarta.servlet.http.HttpServletRequest;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2Error;
import org.springframework.security.oauth2.core.OAuth2ErrorCodes;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.web.authentication.AuthenticationConverter;
import org.springframework.stereotype.Component;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.util.StringUtils;

/**
 * Converts an {@link HttpServletRequest} into an {@link Authentication} instance for Personal
 * Access Tokens.
 *
 * <p>This converter handles requests for the {@code personal_access_token} grant type, validates
 * the required parameters, and constructs a {@link PersonalAccessTokenAuthenticationToken} for
 * further authentication processing.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public final class PersonalAccessTokenAuthenticationConverter implements AuthenticationConverter {
  /** The URI for OAuth2 error details. */
  private static final String ERROR_URI =
      "https://datatracker.ietf.org/doc/html/rfc6749#section-5.2";

  private final SecurityConfigurationProperties securityConfigurationProperties;
  private final SignatureService signatureService;

  /**
   * Converts the provided HTTP request into an {@link Authentication} object.
   *
   * <p>The method validates the OAuth2 parameters such as {@code grant_type}, {@code scope}, and
   * signature token, extracts additional parameters, and constructs a {@link
   * PersonalAccessTokenAuthenticationToken}.
   *
   * @param request the HTTP request containing authentication details.
   * @return an {@link Authentication} instance representing the authenticated token.
   * @throws OAuth2AuthenticationException if validation or conversion fails.
   */
  public Authentication convert(HttpServletRequest request) {
    log.debug("Converting request to authentication: {}", request.getRequestURI());
    var parameters = getFormParameters(request);

    var grantType = parameters.getFirst(OAuth2ParameterNames.GRANT_TYPE);
    if (!ExtendedAuthorizationGrantType.PERSONAL_ACCESS_TOKEN.getValue().equals(grantType)) {
      log.warn("Invalid grant type: {}", grantType);
      throwError(OAuth2ErrorCodes.INVALID_REQUEST, OAuth2ParameterNames.GRANT_TYPE);
    }

    var clientPrincipal = SecurityContextHolder.getContext().getAuthentication();

    var scope = parameters.getFirst(OAuth2ParameterNames.SCOPE);
    if (!StringUtils.hasText(scope) || parameters.get(OAuth2ParameterNames.SCOPE).size() != 1) {
      log.warn("Invalid scope: {}", scope);
      throwError(OAuth2ErrorCodes.INVALID_REQUEST, OAuth2ParameterNames.SCOPE);
    }

    var requestedScopes =
        new HashSet<>(Arrays.asList(StringUtils.delimitedListToStringArray(scope, " ")));

    var additionalParameters = new HashMap<String, Object>();
    parameters.forEach(
        (key, value) -> {
          if (!key.equals(OAuth2ParameterNames.GRANT_TYPE)
              && !key.equals(OAuth2ParameterNames.SCOPE)) {
            additionalParameters.put(
                key, (value.size() == 1) ? value.getFirst() : value.toArray(new String[0]));
          }
        });

    var token = request.getHeader(securityConfigurationProperties.getSignatureHeader());
    if (token == null || token.isBlank()) {
      log.error("Signature not found");
      throwError(OAuth2ErrorCodes.UNAUTHORIZED_CLIENT, OAuth2ParameterNames.PASSWORD);
    }

    try (var ignored =
        MDC.putCloseable("client_id", parameters.get(OAuth2ParameterNames.CLIENT_ID).getFirst())) {
      var signature = signatureService.validate(token, BasicSignature.class);
      setRequestAttributes(request, signature);

      log.debug("Conversion to PersonalAccessTokenAuthenticationToken successful");
      return new PersonalAccessTokenAuthenticationToken(
          clientPrincipal,
          requestedScopes,
          signature.getUserId(),
          signature.getUserName(),
          signature.getUserEmail(),
          signature.getTenantId(),
          signature.getTenantUrl(),
          additionalParameters);
    } catch (RuntimeException e) {
      log.error("Error during authentication conversion", e);
      throwError(OAuth2ErrorCodes.SERVER_ERROR, "internal_parameter");
      return null;
    }
  }

  /**
   * Sets request attributes with the details from the validated signature.
   *
   * @param request the HTTP request.
   * @param basicSignature the validated {@link BasicSignature}.
   */
  private void setRequestAttributes(HttpServletRequest request, BasicSignature basicSignature) {
    request.setAttribute(securityConfigurationProperties.getSignatureHeader(), basicSignature);
  }

  /**
   * Extracts form parameters from the HTTP request.
   *
   * <p>This method parses the request's parameter map, excluding query string parameters.
   *
   * @param request the HTTP request.
   * @return a {@link MultiValueMap} containing the form parameters.
   */
  static MultiValueMap<String, String> getFormParameters(HttpServletRequest request) {
    var parameterMap = request.getParameterMap();
    var parameters = new LinkedMultiValueMap<String, String>();
    parameterMap.forEach(
        (key, values) -> {
          var queryString =
              StringUtils.hasText(request.getQueryString()) ? request.getQueryString() : "";
          if (!queryString.contains(key)) {
            for (String value : values) {
              parameters.add(key, value);
            }
          }
        });
    return parameters;
  }

  /**
   * Throws an {@link OAuth2AuthenticationException} with the specified error code and parameter
   * name.
   *
   * @param errorCode the error code for the exception.
   * @param parameterName the name of the parameter causing the error.
   * @throws OAuth2AuthenticationException the constructed exception.
   */
  static void throwError(String errorCode, String parameterName) {
    var error =
        new OAuth2Error(
            errorCode,
            "OAuth 2.0 Parameter: " + parameterName,
            PersonalAccessTokenAuthenticationConverter.ERROR_URI);
    throw new OAuth2AuthenticationException(error);
  }
}
