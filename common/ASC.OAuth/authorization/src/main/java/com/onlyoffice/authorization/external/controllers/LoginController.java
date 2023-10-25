/**
 *
 */
package com.onlyoffice.authorization.external.controllers;

import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.CookieValue;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.util.UriComponentsBuilder;

/**
 *
 */
@Controller
@RequiredArgsConstructor
@Slf4j
public class LoginController {
    private final String CLIENT_ID_COOKIE = "client_id";
    private final String FORWARD = "FORWARD";
    private final ClientPersistenceQueryUsecases queryUsecases;

    @GetMapping("/oauth2/login")
    public String login(
            HttpServletRequest request,
            @RequestParam(name = "error", required = false) String error,
            @CookieValue(name = CLIENT_ID_COOKIE) String clientId
    ) {
        var client = queryUsecases.getClientByClientId(clientId);
        var loginUrl = String.format("redirect:%s", UriComponentsBuilder
                .fromUriString(client.getTenantUrl())
                .path("login")
                .queryParam("client_id", clientId)
                .queryParam("type", "oauth2")
                .build());

        log.info("a new login request");
        if (request.getDispatcherType().name() == null || !request.getDispatcherType().name().equals(FORWARD))
            return loginUrl;

        if (error != null && !error.isBlank())
            return loginUrl;

        return "login";
    }
}