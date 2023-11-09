/**
 *
 */
package com.onlyoffice.authorization.external.controllers;

import com.onlyoffice.authorization.core.usecases.repositories.AuthorizationPersistenceQueryUsecases;
import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import com.onlyoffice.authorization.external.clients.DocspaceClient;
import com.onlyoffice.authorization.security.access.aspects.annotations.InvalidateSession;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.CookieValue;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.util.UriComponentsBuilder;

import java.net.URI;

/**
 *
 */
@Controller
@RequiredArgsConstructor
@Slf4j
public class AuthorizationConsentController {
    private final String ASC_AUTH_COOKIE = "asc_auth_key";
    private final String CLIENT_ID = "client_id";
    private final DocspaceClient docspaceClient;
    private final AuthorizationPersistenceQueryUsecases authorizationRepository;
    private final ClientPersistenceQueryUsecases queryUsecases;

    @GetMapping(value = "/oauth2/consent")
    @InvalidateSession
    public String consent(
            HttpServletRequest request,
            @CookieValue(name = ASC_AUTH_COOKIE) String authCookie,
            @RequestParam(name = CLIENT_ID) String clientId
    ) {
        MDC.put("client_id", clientId);
        log.info("Got a new consent request");
        log.info("Trying to get client by client id");
        var client = queryUsecases.getClientByClientId(clientId);
        try {
            var cookie = String.format("%s=%s", "asc_auth_key", authCookie);
            var me = docspaceClient.getMe(URI.create(client.getTenantUrl()), cookie)
                    .getResponse();
            MDC.put("client_id", clientId);
            MDC.put("principal_name", me.getEmail());
            log.info("Trying to get consent by principal name and client");
            var auth = authorizationRepository.getByPrincipalNameAndRegisteredClientId(me.getEmail(), clientId);
            return String.format("redirect:%s", UriComponentsBuilder
                    .fromUriString(client.getTenantUrl())
                    .path("login/consent")
                    .queryParam("type", "oauth2")
                    .queryParam("client_id", clientId)
                    .queryParam("state", auth.getState())
                    .build());
        } catch (Exception e) {
            MDC.put("message", e.getMessage());
            log.info("Could not redirect to consent page. Redirecting to login");
            return String.format("redirect:%s", UriComponentsBuilder
                    .fromUriString(client.getTenantUrl())
                    .path("login")
                    .queryParam("type", "oauth2")
                    .queryParam("client_id", clientId)
                    .build());
        } finally {
            MDC.clear();
        }
    }
}
