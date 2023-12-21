package com.onlyoffice.authorization.api.web.security.filters;

import com.onlyoffice.authorization.api.web.client.APIClient;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.security.context.SettingsContextContainer;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.stereotype.Component;

import java.net.URI;
import java.util.Arrays;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class CheckAscCookieCommonProcessor {
    private final String AUTH_COOKIE_NAME = "asc_auth_key";
    private final String X_DOCSPACE_ADDRESS = "x-docspace-address";

    private final APIClient apiClient;

    /**
     *
     * @param request
     * @throws BadCredentialsException
     */
    public void processAscCookies(HttpServletRequest request) throws BadCredentialsException {
        log.debug("Trying to authenticate an incoming request", request);

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

        var address = URI.create(addressCookie.get().getValue());
        var ascCookie = String.format("%s=%s", authCookie.get().getName(),
                authCookie.get().getValue());

        try {
            var firstFuture = CompletableFuture.supplyAsync(() -> apiClient
                    .getMe(address, ascCookie));
            var secondFuture = CompletableFuture.supplyAsync(() -> apiClient
                    .getTenant(address, ascCookie));
            var thirdFuture = CompletableFuture.supplyAsync(() -> apiClient
                    .getSettings(address, ascCookie));

            CompletableFuture.allOf(firstFuture, secondFuture, thirdFuture).join();
            var user = firstFuture.get();
            if (user == null || user.getResponse() == null)
                throw new BadCredentialsException("Could not fetch ASC user");

            var tenant = secondFuture.get();
            if (tenant == null || tenant.getResponse() == null)
                throw new BadCredentialsException("Could not fetch ASC tenant");

            var settings = thirdFuture.get();
            if (settings == null || settings.getResponse() == null)
                throw new BadCredentialsException("Could not fetch ASC tenant settings");

            PersonContextContainer.context.set(user);
            TenantContextContainer.context.set(tenant);
            SettingsContextContainer.context.set(settings);
        } catch (InterruptedException | ExecutionException e) {
            throw new BadCredentialsException("Could not fetch ASC data from an instance");
        }
    }
}
