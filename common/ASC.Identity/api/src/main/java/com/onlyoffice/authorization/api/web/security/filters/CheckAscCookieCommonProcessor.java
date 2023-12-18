package com.onlyoffice.authorization.api.web.security.filters;

import jakarta.servlet.http.HttpServletRequest;
import lombok.extern.slf4j.Slf4j;
import org.springframework.data.util.Pair;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.stereotype.Component;

import java.util.Arrays;

@Slf4j
@Component
public class CheckAscCookieCommonProcessor {
    private final String AUTH_COOKIE_NAME = "asc_auth_key";
    private final String X_DOCSPACE_ADDRESS = "x-docspace-address";
    public Pair<String, String> processAscCookies(HttpServletRequest request) throws BadCredentialsException {
        log.debug("Validating asc user");
        var cookies = request.getCookies();
        if (cookies == null || cookies.length < 1)
            throw new BadCredentialsException("Could not find any authentication cookie");

        var addressCookie = Arrays.stream(cookies)
                .filter(c -> c.getName().equalsIgnoreCase(X_DOCSPACE_ADDRESS))
                .findFirst();

        if (addressCookie.isEmpty())
            throw new BadCredentialsException("Could not find asc address cookie");

        var authCookie = Arrays.stream(cookies)
                .filter(c -> c.getName().equalsIgnoreCase(AUTH_COOKIE_NAME))
                .findFirst();

        if (authCookie.isEmpty())
            throw new BadCredentialsException("Could not find asc auth cookie");

        return Pair.of(addressCookie.get().getValue(),
                String.format("%s=%s", authCookie.get().getName(),
                        authCookie.get().getValue()));
    }
}
