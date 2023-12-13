/**
 *
 */
package com.onlyoffice.authorization.security.oauth.providers;

import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import com.onlyoffice.authorization.external.clients.DocspaceClient;
import com.onlyoffice.authorization.security.oauth.authorities.TenantAuthority;
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

import java.net.URI;
import java.util.List;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class DocspaceAuthenticationProvider implements AuthenticationProvider {
    private final DocspaceClient docspaceClient;
    private final ClientPersistenceQueryUsecases queryUsecases;

    public Authentication authenticate(Authentication authentication)
            throws AuthenticationException {
        log.info("Trying to authenticate a user");
        var clientId = (String) authentication.getPrincipal();
        var authCookie = (jakarta.servlet.http.Cookie) authentication.getCredentials();

        MDC.put("client_id", clientId);
        log.info("Trying to get client by client id");
        MDC.clear();

        var client = queryUsecases.getClientByClientId(clientId);

        MDC.put("cookie", authCookie.getValue());
        log.debug("Trying to validate an ASC authorization");
        MDC.clear();

        var cookie = String.format("%s=%s", authCookie.getName(),
                authCookie.getValue());

        MDC.put("tenant_url", client.getTenantUrl());
        log.info("Trying to get current user profile");
        MDC.clear();

        var me = docspaceClient.getMe(URI.create(client.getTenantUrl()), cookie);
        if (me == null || me.getStatusCode() != HttpStatus.OK.value())
            throw new BadCredentialsException("Invalid ASC authorization");

        var authenticationToken = new UsernamePasswordAuthenticationToken(me.getResponse()
                .getEmail(), null, List.of(new TenantAuthority(client.getTenantUrl())));
        authenticationToken.setDetails(me.getResponse().getId());
        return authenticationToken;
    }

    public boolean supports(Class<?> authentication) {
        return UsernamePasswordAuthenticationToken.class.equals(authentication);
    }
}