package com.asc.authorization.application.security.oauth.managers;

import java.util.List;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;

/**
 * Custom Authentication Manager that delegates authentication to a list of authentication
 * providers.
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AscAuthenticationManager implements AuthenticationManager {
  private static final String UNSUPPORTED_ERROR =
      "Authentication type is not supported by any authentication provider";
  private final List<AuthenticationProvider> providers;

  /**
   * Attempts to authenticate the provided authentication object using the configured providers.
   *
   * @param authentication the authentication request object.
   * @return a fully authenticated object including credentials.
   * @throws AuthenticationException if authentication fails.
   */
  @Override
  public Authentication authenticate(Authentication authentication) throws AuthenticationException {
    for (AuthenticationProvider provider : providers) {
      MDC.put("provider", provider.getClass().getName());
      log.debug("Checking an authentication provider");

      if (provider.supports(authentication.getClass())) {
        log.debug("Provider is supported");
        MDC.clear();

        return provider.authenticate(authentication);
      }

      MDC.clear();
    }

    log.error(UNSUPPORTED_ERROR);
    throw new BadCredentialsException(UNSUPPORTED_ERROR);
  }
}
