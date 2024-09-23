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

package com.asc.authorization.application.security.converter;

import com.asc.authorization.application.security.authentication.PersonalAccessTokenAuthenticationToken;
import com.asc.authorization.application.security.grant.ExtendedAuthorizationGrantType;
import com.asc.common.application.client.AscApiClient;
import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.common.utilities.HttpUtils;
import jakarta.servlet.http.HttpServletRequest;
import java.net.URI;
import java.util.Arrays;
import java.util.HashMap;
import java.util.HashSet;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.oauth2.core.OAuth2AuthenticationException;
import org.springframework.security.oauth2.core.OAuth2Error;
import org.springframework.security.oauth2.core.OAuth2ErrorCodes;
import org.springframework.security.oauth2.core.endpoint.OAuth2ParameterNames;
import org.springframework.security.web.authentication.AuthenticationConverter;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.util.StringUtils;

/**
 * This class converts an HttpServletRequest into an Authentication instance for Personal Access
 * Tokens.
 */
@Slf4j
@RequiredArgsConstructor
public final class PersonalAccessTokenAuthenticationConverter implements AuthenticationConverter {
  private static final String ERROR_URI =
      "https://datatracker.ietf.org/doc/html/rfc6749#section-5.2";
  private static final String ASC_AUTH_COOKIE = "asc_auth_key";

  private final AscApiClient apiClient;

  /**
   * Converts the provided HttpServletRequest into an Authentication object.
   *
   * @param request The HTTP request containing authentication details.
   * @return An Authentication instance representing the authenticated token.
   * @throws OAuth2AuthenticationException if the conversion fails.
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

    var address = HttpUtils.getRequestHostAddress(request).orElse(null);
    if (address == null) {
      log.error("Invalid request address");
      throwError(OAuth2ErrorCodes.INVALID_REQUEST, OAuth2ParameterNames.RESOURCE);
    }

    var hostAddress = HttpUtils.getRequestDomain(request).orElse(null);
    if (hostAddress == null) {
      log.error("Invalid request domain");
      throwError(OAuth2ErrorCodes.INVALID_REQUEST, OAuth2ParameterNames.RESOURCE);
    }

    var cookies = request.getCookies();
    if (cookies == null) {
      log.error("Cookies not found");
      throwError(OAuth2ErrorCodes.UNAUTHORIZED_CLIENT, OAuth2ParameterNames.PASSWORD);
    }

    var authCookie =
        Arrays.stream(cookies)
            .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_COOKIE))
            .findFirst()
            .orElse(null);
    if (authCookie == null) {
      log.error("Authorization cookie not found");
      throwError(OAuth2ErrorCodes.UNAUTHORIZED_CLIENT, OAuth2ParameterNames.PASSWORD);
    }

    try (var ignored =
        MDC.putCloseable("client_id", parameters.get(OAuth2ParameterNames.CLIENT_ID).getFirst())) {
      var uri = URI.create(address);
      var cookie = String.format("%s=%s", authCookie.getName(), authCookie.getValue());

      var meFuture = CompletableFuture.supplyAsync(() -> apiClient.getMe(uri, cookie));
      var tenantFuture = CompletableFuture.supplyAsync(() -> apiClient.getTenant(uri, cookie));

      CompletableFuture.allOf(meFuture, tenantFuture).join();

      var me = meFuture.get();
      var tenant = tenantFuture.get();

      if (me.getStatusCode() != HttpStatus.OK.value()
          || tenant.getStatusCode() != HttpStatus.OK.value()) {
        log.error("Failed to retrieve user or tenant information");
        throwError(OAuth2ErrorCodes.UNAUTHORIZED_CLIENT, OAuth2ParameterNames.RESOURCE);
      }

      var tenantResponse = tenant.getResponse();
      setRequestAttributes(request, me.getResponse(), tenantResponse);

      log.debug("Conversion to PersonalAccessTokenAuthenticationToken successful");
      return new PersonalAccessTokenAuthenticationToken(
          clientPrincipal,
          requestedScopes,
          me.getResponse().getId(),
          me.getResponse().getUserName(),
          me.getResponse().getEmail(),
          tenantResponse.getTenantId(),
          hostAddress,
          additionalParameters);
    } catch (InterruptedException | ExecutionException e) {
      log.error("Error during authentication conversion", e);
      throwError(OAuth2ErrorCodes.SERVER_ERROR, "internal_parameter");
      return null;
    }
  }

  private void setRequestAttributes(
      HttpServletRequest request, AscPersonResponse person, AscTenantResponse tenant) {
    request.setAttribute("person", person);
    request.setAttribute("tenant", tenant);
  }

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

  static void throwError(String errorCode, String parameterName) {
    var error =
        new OAuth2Error(
            errorCode,
            "OAuth 2.0 Parameter: " + parameterName,
            PersonalAccessTokenAuthenticationConverter.ERROR_URI);
    throw new OAuth2AuthenticationException(error);
  }
}
