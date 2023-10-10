package com.onlyoffice.authorization.extensions.providers;

import com.onlyoffice.authorization.client.DocspaceClient;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.security.authentication.AuthenticationProvider;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.Authentication;
import org.springframework.security.core.AuthenticationException;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.util.Arrays;
import java.util.UUID;

@Component
@RequiredArgsConstructor
@Slf4j
public class DocspaceAuthenticationProvider implements AuthenticationProvider {
    private final DocspaceClient docspaceClient;

    public Authentication authenticate(Authentication authentication) throws AuthenticationException {
        var request = ((ServletRequestAttributes) RequestContextHolder.currentRequestAttributes())
                .getRequest();

        log.debug("trying to authenticate a docspace user {}", authentication.getPrincipal().toString());

        var authCookie = Arrays.stream(request.getCookies()).filter(c -> c.getName().equalsIgnoreCase("asc_auth_key"))
                .findFirst();

        if (authCookie.isEmpty())
            throw new BadCredentialsException("Authorization cookie is empty");

        var cookie = String.format("%s=%s", authCookie.get().getName(),
                authCookie.get().getValue());

        var me = docspaceClient.getMe(cookie);
        if (me.getStatusCode() == HttpStatus.OK.value() && !me.getResponse().getIsAdmin())
            throw new BadCredentialsException("Invalid Docspace authorization");

        return new UsernamePasswordAuthenticationToken(UUID.randomUUID().toString(), UUID.randomUUID().toString(), null);
    }

    public boolean supports(Class<?> authentication) {
        return UsernamePasswordAuthenticationToken.class.equals(authentication);
    }
}