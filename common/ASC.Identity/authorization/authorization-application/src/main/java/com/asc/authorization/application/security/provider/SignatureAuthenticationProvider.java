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

package com.asc.authorization.application.security.provider;

import com.asc.authorization.application.configuration.properties.SecurityConfigurationProperties;
import com.asc.authorization.application.exception.authentication.AuthenticationProcessingException;
import com.asc.authorization.application.security.authentication.BasicSignature;
import com.asc.authorization.application.security.authentication.TenantAuthority;
import com.asc.authorization.application.security.oauth.error.AuthenticationError;
import com.asc.authorization.application.security.oauth.service.GrpcRegisteredClientService;
import com.asc.authorization.application.security.service.SignatureService;
import com.asc.common.application.proto.ClientResponse;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import java.util.Arrays;
import java.util.List;
import java.util.Optional;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

/**
 * Custom Authentication Provider for authenticating users using a signature.
 *
 * <p>This provider validates the ASC signature token, fetches client information from a gRPC
 * service, validates the client, and constructs an authenticated token with user and tenant
 * details. It also publishes audit logs for successful authentications.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class SignatureAuthenticationProvider implements AuthenticationProvider {
  private final SignatureService signatureService;
  private final GrpcRegisteredClientService registeredClientService;

  private final SecurityConfigurationProperties configurationProperties;

  /**
   * Authenticates the provided request using an ASC signature.
   *
   * <p>Validates the client ID, ASC signature, and client status. If authentication succeeds,
   * publishes an audit log and returns an authenticated token.
   *
   * @param authentication the {@link Authentication} request object.
   * @return a fully authenticated {@link Authentication} object.
   * @throws AuthenticationException if authentication fails due to invalid credentials or errors.
   */
  @RateLimiter(name = "globalRateLimiter")
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    log.info("Trying to authenticate a user");

    var ctx = (ServletRequestAttributes) RequestContextHolder.getRequestAttributes();
    if (ctx == null)
      throw new BadCredentialsException("Authentication failed due to missing request context");

    var request = ctx.getRequest();
    var response = ctx.getResponse();
    if (response == null)
      throw new BadCredentialsException("Authentication failed due to missing response context");

    var principal = authentication.getPrincipal();
    if (principal == null)
      throw new AuthenticationProcessingException(
          AuthenticationError.SOMETHING_WENT_WRONG_ERROR,
          "Authentication failed due to missing client ID in principal");

    var clientId = principal.toString();
    if (clientId == null || clientId.isBlank())
      throw new AuthenticationProcessingException(
          AuthenticationError.SOMETHING_WENT_WRONG_ERROR,
          "Authentication failed due to missing client ID in principal");

    var token =
        Arrays.stream(Optional.ofNullable(request.getCookies()).orElse(new Cookie[0]))
            .filter(c -> c.getName().equalsIgnoreCase(configurationProperties.getSignatureCookie()))
            .findFirst()
            .map(Cookie::getValue)
            .orElseGet(() -> request.getHeader(configurationProperties.getSignatureCookie()));

    if (token == null || token.isBlank())
      throw new AuthenticationProcessingException(
          AuthenticationError.MISSING_ASC_SIGNATURE,
          "Authentication failed due to missing asc signature");

    try (var ignored = MDC.putCloseable("client_id", clientId)) {
      var clientFuture = requestClient(clientId);
      var signature = signatureService.validate(token, BasicSignature.class);
      var client = clientFuture.get();

      validateClient(client);
      setRequestAttributes(request, signature);

      return buildAuthentication(signature, client);
    } catch (InterruptedException | ExecutionException e) {
      throw new AuthenticationProcessingException(
          AuthenticationError.SOMETHING_WENT_WRONG_ERROR, "Authentication failed", e);
    }
  }

  /**
   * Asynchronously requests client information from the gRPC service.
   *
   * @param clientId the client ID to fetch information for.
   * @return a {@link CompletableFuture} with the {@link ClientResponse}.
   */
  private CompletableFuture<ClientResponse> requestClient(String clientId) {
    return CompletableFuture.supplyAsync(
        () -> {
          try {
            return registeredClientService.getClient(clientId);
          } catch (Exception e) {
            return null;
          }
        });
  }

  /**
   * Validates the client information to ensure it is enabled, public, and accessible.
   *
   * @param client the {@link ClientResponse} containing client details.
   * @throws AuthenticationProcessingException if the client is invalid or not accessible.
   */
  private void validateClient(ClientResponse client) {
    if (client == null)
      throw new AuthenticationProcessingException(
          AuthenticationError.CLIENT_NOT_FOUND_ERROR, "Authentication failed: client not found");

    if (!client.getEnabled())
      throw new AuthenticationProcessingException(
          AuthenticationError.CLIENT_DISABLED_ERROR, "Client is disabled");

    if (!client.getIsPublic())
      throw new AuthenticationProcessingException(
          AuthenticationError.CLIENT_PERMISSION_DENIED_ERROR, "Client is not public");
  }

  /**
   * Sets request attributes with signature details.
   *
   * <p>Attributes include the tenant, user, and ASC signature for downstream processing.
   *
   * @param request the {@link HttpServletRequest}.
   * @param signature the {@link BasicSignature} object containing user and tenant details.
   */
  private void setRequestAttributes(HttpServletRequest request, BasicSignature signature) {
    request.setAttribute(configurationProperties.getSignatureCookie(), signature);
  }

  /**
   * Builds an authenticated token with user and tenant details.
   *
   * @param signature the {@link BasicSignature}.
   * @param client the {@link ClientResponse}.
   * @return a {@link UsernamePasswordAuthenticationToken}.
   */
  private UsernamePasswordAuthenticationToken buildAuthentication(
      BasicSignature signature, ClientResponse client) {
    var authenticationToken =
        new UsernamePasswordAuthenticationToken(
            signature.getUserId(),
            signature,
            List.of(new TenantAuthority(signature.getTenantId(), signature.getTenantUrl())));
    authenticationToken.setDetails(client.getClientId());
    return authenticationToken;
  }

  /**
   * Checks if this {@link AuthenticationProvider} supports the provided authentication type.
   *
   * @param authentication the authentication class.
   * @return {@code true} if the authentication type is supported, {@code false} otherwise.
   */
  public boolean supports(Class<?> authentication) {
    return UsernamePasswordAuthenticationToken.class.equals(authentication);
  }
}
