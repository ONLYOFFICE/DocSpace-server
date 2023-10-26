package com.onlyoffice.authorization.security.access.handlers;

import com.onlyoffice.authorization.core.usecases.repositories.AuthorizationPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import com.onlyoffice.authorization.external.clients.DocspaceClient;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.security.core.Authentication;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.stereotype.Component;
import org.springframework.util.StringUtils;

import java.io.IOException;
import java.net.URI;
import java.util.Arrays;

@Component
@RequiredArgsConstructor
public class CookieSuccessAuthenticationHandler implements AuthenticationSuccessHandler {
    private final String ASC_AUTH_COOKIE = "asc_auth_key";
    private final String CLIENT_ID_COOKIE = "client_id";
    private final String CLIENT_STATE_COOKIE = "client_state";

    private final DocspaceClient docspaceClient;
    private final AuthorizationPersistenceQueryUsecases authorizationRepository;
    private final ClientPersistenceQueryUsecases queryUsecases;

    public void onAuthenticationSuccess(HttpServletRequest request, HttpServletResponse response, Authentication authentication) throws IOException, ServletException {
        var ascCookie = Arrays.stream(request.getCookies())
                .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_COOKIE))
                .findFirst()
                .get();

        var clientCookie =  Arrays.stream(request.getCookies())
                .filter(v -> v.getName().equalsIgnoreCase(CLIENT_ID_COOKIE))
                .findFirst()
                .get();

        var client = queryUsecases.getClientByClientId(clientCookie.getValue());
        var me = docspaceClient.getMe(URI.create(client.getTenantUrl()),
                        String.format("%s=%s", ASC_AUTH_COOKIE, ascCookie.getValue()))
                .getResponse();
        var auth = authorizationRepository
                .getByPrincipalNameAndRegisteredClientId(me.getEmail(), clientCookie.getValue());

        Cookie cookie = new Cookie(CLIENT_STATE_COOKIE, auth.getState());
        cookie.setPath("/");
        cookie.setMaxAge(60 * 60 * 24 * 365 * 10);
        cookie.setHttpOnly(true);
        cookie.setSecure(true);
        response.addCookie(cookie);
    }
}
