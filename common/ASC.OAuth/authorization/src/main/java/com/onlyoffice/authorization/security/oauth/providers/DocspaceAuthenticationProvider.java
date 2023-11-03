/**
 *
 */
package com.onlyoffice.authorization.security.oauth.providers;

import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import com.onlyoffice.authorization.external.clients.DocspaceClient;
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

import java.net.URI;
import java.util.Arrays;
import java.util.UUID;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class DocspaceAuthenticationProvider implements AuthenticationProvider {
    private final String CLIENT_ID_COOKIE = "client_id";
    private final String ASC_AUTH_COOKIE = "asc_auth_key";
    private final DocspaceClient docspaceClient;
    private final ClientPersistenceQueryUsecases queryUsecases;

    public Authentication authenticate(Authentication authentication)
            throws AuthenticationException {
        log.info("Trying to authenticate a user");
        var request = ((ServletRequestAttributes) RequestContextHolder.currentRequestAttributes())
                .getRequest();

        var clientCookie = Arrays.stream(request.getCookies()).filter(c -> c.getName().equalsIgnoreCase(CLIENT_ID_COOKIE))
                .findFirst();

        if (clientCookie.isEmpty()) {
            log.warn("Docspace client cookie is empty");
            throw new BadCredentialsException("Docspace client cookie is empty");
        }

        MDC.put("client_id", clientCookie.get().getValue());
        log.info("Trying to get client by client id");
        MDC.clear();
        var client = queryUsecases.getClientByClientId(clientCookie.get().getValue());

        var authCookie = Arrays.stream(request.getCookies())
                .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_COOKIE))
                .findFirst();

        if (authCookie.isEmpty()) {
            log.warn("Docspace authorization cookie is empty");
            throw new BadCredentialsException("Docspace authorization cookie is empty");
        }

        MDC.put("cookie", authCookie.get().getValue());
        log.debug("Trying to validate a Docspace authorization");
        MDC.clear();

        var cookie = String.format("%s=%s", authCookie.get().getName(),
                authCookie.get().getValue());

        MDC.put("tenant_url", client.getTenantUrl());
        log.info("Trying to get current user profile");
        MDC.clear();
        var me = docspaceClient.getMe(URI.create(client.getTenantUrl()), cookie);
        if (me.getStatusCode() == HttpStatus.OK.value() && !me.getResponse().getIsAdmin())
            throw new BadCredentialsException("Invalid docspace authorization");

        return new UsernamePasswordAuthenticationToken(me.getResponse().getEmail(), UUID.randomUUID().toString(), null);
    }

    public boolean supports(Class<?> authentication) {
        return UsernamePasswordAuthenticationToken.class.equals(authentication);
    }
}