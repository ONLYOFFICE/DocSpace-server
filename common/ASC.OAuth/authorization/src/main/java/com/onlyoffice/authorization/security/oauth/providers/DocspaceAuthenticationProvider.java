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
    private final DocspaceClient docspaceClient;
    private final ClientPersistenceQueryUsecases queryUsecases;

    public Authentication authenticate(Authentication authentication)
            throws AuthenticationException {
        var request = ((ServletRequestAttributes) RequestContextHolder.currentRequestAttributes())
                .getRequest();


        var clientCookie = Arrays.stream(request.getCookies()).filter(c -> c.getName().equalsIgnoreCase(CLIENT_ID_COOKIE))
                .findFirst();

        if (clientCookie.isEmpty())
            throw new BadCredentialsException("Docspace client cookie is empty");

        var client = queryUsecases.getClientByClientId(clientCookie.get().getValue());

        var authCookie = Arrays.stream(request.getCookies()).filter(c -> c.getName().equalsIgnoreCase("asc_auth_key"))
                .findFirst();

        if (authCookie.isEmpty())
            throw new BadCredentialsException("Docspace authorization cookie is empty");

        MDC.put("cookie", authCookie.get().getValue());
        log.info("trying to validate a docspace authorization");
        MDC.clear();

        var cookie = String.format("%s=%s", authCookie.get().getName(),
                authCookie.get().getValue());

        var me = docspaceClient.getMe(URI.create(client.getTenantUrl()), cookie);
        if (me.getStatusCode() == HttpStatus.OK.value() && !me.getResponse().getIsAdmin())
            throw new BadCredentialsException("Invalid docspace authorization");

        return new UsernamePasswordAuthenticationToken(me.getResponse().getEmail(), UUID.randomUUID().toString(), null);
    }

    public boolean supports(Class<?> authentication) {
        return UsernamePasswordAuthenticationToken.class.equals(authentication);
    }
}