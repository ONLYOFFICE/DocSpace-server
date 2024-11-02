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

package com.asc.authorization.application.security.provider;

import com.asc.authorization.application.exception.authentication.AuthenticationProcessingException;
import com.asc.authorization.application.security.authority.TenantAuthority;
import com.asc.authorization.application.security.error.AuthenticationError;
import com.asc.authorization.application.security.service.CacheableRegisteredClientQueryService;
import com.asc.common.application.client.AscApiClient;
import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscSettingsResponse;
import com.asc.common.application.transfer.response.AscTenantResponse;
import com.asc.common.core.domain.value.enums.AuditCode;
import com.asc.common.service.ports.output.message.publisher.AuditMessagePublisher;
import com.asc.common.service.transfer.message.AuditMessage;
import com.asc.common.utilities.HttpUtils;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import jakarta.servlet.http.HttpServletRequest;
import java.net.URI;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

/** Custom Authentication Provider that authenticates users using an ASC authorization cookie. */
@Slf4j
@Component
@RequiredArgsConstructor
public class AscCodeAuthenticationProvider implements AuthenticationProvider {
  private static final String ASC_AUTH_COOKIE = "asc_auth_key";

  @Value("${spring.application.name}")
  private String serviceName;

  private final HttpUtils httpUtils;
  private final AscApiClient apiClient;
  private final AuditMessagePublisher auditMessagePublisher;
  private final CacheableRegisteredClientQueryService cacheableRegisteredClientQueryService;

  /**
   * Authenticates the provided authentication request using an ASC authorization cookie.
   *
   * @param authentication the authentication request object.
   * @return a fully authenticated object including credentials.
   * @throws AuthenticationException if authentication fails.
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

    var address =
        httpUtils
            .getRequestHostAddress(request)
            .orElseThrow(
                () -> new BadCredentialsException("Could not find ASC request host address"));
    var hostAddress =
        httpUtils
            .getRequestDomain(request)
            .orElseThrow(
                () -> new BadCredentialsException("Could not find ASC request domain address"));

    var clientId = (String) authentication.getPrincipal();
    if (clientId == null || clientId.isBlank())
      throw new AuthenticationProcessingException(
          AuthenticationError.SOMETHING_WENT_WRONG_ERROR,
          "Authentication failed due to missing client ID in principal");

    var authCookie =
        Arrays.stream(request.getCookies())
            .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_COOKIE))
            .findFirst()
            .orElse(null);

    if (authCookie == null)
      throw new AuthenticationProcessingException(
          AuthenticationError.MISSING_ASC_COOKIE_ERROR,
          "Authentication failed due to missing auth cookie");

    try (var ignored = MDC.putCloseable("client_id", clientId)) {
      var clientFuture =
          CompletableFuture.supplyAsync(
              () -> {
                log.info("Trying to get client by client id");
                return cacheableRegisteredClientQueryService.findByClientId(clientId);
              });

      var uri = URI.create(address);
      var cookie = String.format("%s=%s", authCookie.getName(), authCookie.getValue());

      var meFuture = CompletableFuture.supplyAsync(() -> apiClient.getMe(uri, cookie));
      var tenantFuture = CompletableFuture.supplyAsync(() -> apiClient.getTenant(uri, cookie));
      var settingsFuture = CompletableFuture.supplyAsync(() -> apiClient.getSettings(uri, cookie));

      CompletableFuture.allOf(clientFuture, meFuture, tenantFuture, settingsFuture).join();

      var client = clientFuture.get();
      if (client == null)
        throw new AuthenticationProcessingException(
            AuthenticationError.CLIENT_NOT_FOUND_ERROR, "Authentication failed: client not found");

      var me = meFuture.get();
      var tenant = tenantFuture.get();
      var settings = settingsFuture.get();

      if (me.getStatusCode() != HttpStatus.OK.value()
          || tenant.getStatusCode() != HttpStatus.OK.value()
          || settings.getStatusCode() != HttpStatus.OK.value())
        throw new AuthenticationProcessingException(
            AuthenticationError.ASC_RETRIEVAL_ERROR,
            "Invalid ASC authorization: could not fetch either me, tenant, settings or any combination of those");

      if (!client.isEnabled())
        throw new AuthenticationProcessingException(
            AuthenticationError.CLIENT_DISABLED_ERROR, "Client is disabled");

      var tenantResponse = tenant.getResponse();
      if (tenantResponse.getTenantId() != client.getTenant() && !client.isPublic())
        throw new AuthenticationProcessingException(
            AuthenticationError.CLIENT_PERMISSION_DENIED_ERROR,
            "Client is not public and does not belong to current user's tenant");

      setRequestAttributes(request, me.getResponse(), tenantResponse, settings.getResponse());

      var authenticationToken =
          new UsernamePasswordAuthenticationToken(
              me.getResponse().getId(),
              null,
              List.of(new TenantAuthority(tenant.getResponse().getTenantId(), hostAddress)));
      authenticationToken.setDetails(client.getClientId());

      auditMessagePublisher.publish(
          AuditMessage.builder()
              .ip(
                  httpUtils
                      .getRequestClientAddress(request)
                      .map(httpUtils::extractHostFromUrl)
                      .orElseGet(
                          () -> httpUtils.extractHostFromUrl(httpUtils.getFirstRequestIP(request))))
              .initiator(serviceName)
              .target(clientId)
              .browser(httpUtils.getClientBrowser(request))
              .platform(httpUtils.getClientOS(request))
              .tenantId(tenant.getResponse().getTenantId())
              .userId(me.getResponse().getId())
              .userEmail(me.getResponse().getEmail())
              .userName(me.getResponse().getUserName())
              .page(httpUtils.getFullURL(request))
              .action(AuditCode.GENERATE_AUTHORIZATION_CODE_TOKEN.getCode())
              .build());

      return authenticationToken;
    } catch (InterruptedException | ExecutionException | RuntimeException e) {
      throw new AuthenticationProcessingException(
          AuthenticationError.SOMETHING_WENT_WRONG_ERROR, "Authentication failed", e);
    }
  }

  /**
   * Sets the request attributes for person, tenant, and settings.
   *
   * @param request the HTTP request.
   * @param person the authenticated user.
   * @param tenant the tenant information.
   * @param settings the settings information.
   */
  private void setRequestAttributes(
      HttpServletRequest request,
      AscPersonResponse person,
      AscTenantResponse tenant,
      AscSettingsResponse settings) {
    request.setAttribute("person", person);
    request.setAttribute("tenant", tenant);
    request.setAttribute("settings", settings);
  }

  /**
   * Checks if this AuthenticationProvider supports the provided authentication type.
   *
   * @param authentication the authentication class.
   * @return true if the authentication type is supported, false otherwise.
   */
  public boolean supports(Class<?> authentication) {
    return UsernamePasswordAuthenticationToken.class.equals(authentication);
  }
}
