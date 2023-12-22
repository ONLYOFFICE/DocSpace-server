package com.onlyoffice.authorization.web.security.oauth.managers;

import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class ASCAuthenticationManager implements AuthenticationManager {
    private final String UNSUPPORTED_ERROR = "Authentication type is not supported by any authentication provider";
    private final List<AuthenticationProvider> providers;

    /**
     *
     * @param authentication
     * @return
     * @throws AuthenticationException
     */
    public Authentication authenticate(Authentication authentication) throws AuthenticationException {
        for (AuthenticationProvider provider : providers) {
            if (provider.supports(authentication.getClass()))
                return provider.authenticate(authentication);
        }

        throw new BadCredentialsException(UNSUPPORTED_ERROR);
    }
}
