/**
 *
 */
package com.asc.authorization.web.security.oauth.providers;

import com.asc.authorization.core.usecases.service.client.ClientRetrieveUsecases;
import com.asc.authorization.web.clients.APIClient;
import com.asc.authorization.web.security.oauth.authorities.TenantAuthority;
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

import java.net.URI;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AscAuthenticationProvider implements AuthenticationProvider {
    private final String ASC_AUTH_COOKIE = "asc_auth_key";

    private final APIClient apiClient;
    private final ClientRetrieveUsecases retrieveUsecases;

    /**
     *
     * @param authentication
     * @return
     * @throws AuthenticationException
     */
    public Authentication authenticate(Authentication authentication)
            throws AuthenticationException {
        log.info("Trying to authenticate a user");

        var clientId = (String) authentication.getPrincipal();
        var request = ((ServletRequestAttributes) RequestContextHolder
                .getRequestAttributes()).getRequest();
        var authCookie = Arrays.stream(request.getCookies())
                .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_COOKIE))
                .findFirst().orElseThrow(() -> new BadCredentialsException("Could not get an ASC authorization cookie"));

        var future = CompletableFuture.supplyAsync(() ->  {
            MDC.put("clientId", clientId);
            log.info("Trying to get client by client id");
            MDC.clear();

            return retrieveUsecases.getClientByClientId(clientId);
        }).thenApplyAsync((client -> {
            MDC.put("cookie", authCookie.getValue());
            log.debug("Trying to validate an ASC authorization");
            MDC.clear();

            var cookie = String.format("%s=%s", authCookie.getName(),
                    authCookie.getValue());

            MDC.put("tenantUrl", client.getTenantUrl());
            log.info("Trying to get current user profile");
            MDC.clear();

            return Pair.of(client, apiClient.getMe(URI.create(client.getTenantUrl()), cookie));
        }));

        try {
            var response = future.get();
            var client = response.getFirst();
            var me = response.getSecond();

            if (me == null || me.getStatusCode() != HttpStatus.OK.value())
                throw new BadCredentialsException("Invalid ASC authorization");

            var authenticationToken = new UsernamePasswordAuthenticationToken(me.getResponse()
                    .getEmail(), null, List.of(new TenantAuthority(client.getTenantUrl())));
            authenticationToken.setDetails(me.getResponse().getId());

            return authenticationToken;
        } catch (InterruptedException | ExecutionException e) {
            throw new BadCredentialsException(e.getMessage());
        }
    }

    /**
     *
     * @param authentication
     * @return
     */
    public boolean supports(Class<?> authentication) {
        return UsernamePasswordAuthenticationToken.class.equals(authentication);
    }
}