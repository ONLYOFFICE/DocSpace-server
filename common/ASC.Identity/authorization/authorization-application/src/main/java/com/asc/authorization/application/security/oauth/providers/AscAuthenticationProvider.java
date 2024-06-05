package com.asc.authorization.application.security.oauth.providers;

import com.asc.authorization.application.security.oauth.authorities.TenantAuthority;
import com.asc.common.application.client.AscApiClient;
import com.asc.common.data.client.repository.JpaClientRepository;
import io.github.resilience4j.ratelimiter.annotation.RateLimiter;
import java.net.URI;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.data.util.Pair;
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
  private final JpaClientRepository jpaClientRepository;

  private final ExecutorService executorService = Executors.newVirtualThreadPerTaskExecutor();

  /**
   * Authenticates the provided authentication request using an ASC authorization cookie.
   *
   * @param authentication the authentication request object.
   * @return a fully authenticated object including credentials.
   * @throws AuthenticationException if authentication fails.
   */
  @RateLimiter(name = "globalRateLimiter")
  @Override
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    log.info("Trying to authenticate a user");

    var clientId = (String) authentication.getPrincipal();
    var request =
        ((ServletRequestAttributes) RequestContextHolder.getRequestAttributes()).getRequest();
    var authCookie =
        Arrays.stream(request.getCookies())
            .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_COOKIE))
            .findFirst()
            .orElseThrow(
                () -> new BadCredentialsException("Could not get an ASC authorization cookie"));

    var future =
        CompletableFuture.supplyAsync(
                () -> {
                  try (var ignored = MDC.putCloseable("client_id", clientId)) {
                    log.info("Trying to get client by client id");
                    return jpaClientRepository.findClientByClientId(clientId);
                  }
                },
                executorService)
            .thenApplyAsync(
                response -> {
                  if (response.isEmpty()) throw new BadCredentialsException("Client not found");

                  var client = response.get();
                  var cookie = String.format("%s=%s", authCookie.getName(), authCookie.getValue());

                  try (var ignored = MDC.putCloseable("cookie", authCookie.getValue())) {
                    log.debug("Trying to validate an ASC authorization");
                  }

                  try (var ignored = MDC.putCloseable("tenant_url", client.getTenantUrl())) {
                    log.info("Trying to get current user profile");
                    var me = apiClient.getMe(URI.create(client.getTenantUrl()), cookie);
                    if (me.getStatusCode() != HttpStatus.OK.value())
                      throw new BadCredentialsException("Invalid ASC authorization");
                    return Pair.of(client, me);
                  }
                },
                executorService);

    try {
      var response = future.get();
      var client = response.getFirst();
      var me = response.getSecond();

      var authenticationToken =
          new UsernamePasswordAuthenticationToken(
              me.getResponse().getEmail(),
              null,
              List.of(new TenantAuthority(client.getTenantUrl())));
      authenticationToken.setDetails(client.getClientId());
      return authenticationToken;
    } catch (InterruptedException | ExecutionException e) {
      throw new BadCredentialsException("Authentication failed", e);
    }
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
