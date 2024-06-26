package com.asc.authorization.application.security.oauth.providers;

import com.asc.authorization.application.exception.authentication.AuthenticationProcessingException;
import com.asc.authorization.application.security.oauth.authorities.TenantAuthority;
import com.asc.authorization.application.security.oauth.errors.AuthenticationError;
import com.asc.authorization.application.security.oauth.services.CacheableRegisteredClientQueryService;
import com.asc.common.application.client.AscApiClient;
import com.asc.common.application.transfer.response.AscPersonResponse;
import com.asc.common.application.transfer.response.AscSettingsResponse;
import com.asc.common.application.transfer.response.AscTenantResponse;
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
public class AscAuthenticationProvider implements AuthenticationProvider {
  private static final String ASC_AUTH_COOKIE = "asc_auth_key";

  private final AscApiClient apiClient;
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
        HttpUtils.getRequestHostAddress(request)
            .orElseThrow(
                () -> new BadCredentialsException("Could not find ASC request host address"));
    var hostAddress =
        HttpUtils.getRequestDomain(request)
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
      return authenticationToken;
    } catch (InterruptedException | ExecutionException e) {
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
