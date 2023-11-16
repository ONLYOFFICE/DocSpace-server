/**
 *
 */
package com.onlyoffice.authorization.external.controllers;

import com.onlyoffice.authorization.core.usecases.repositories.ClientPersistenceQueryUsecases;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.stereotype.Controller;
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
    private final String CLIENT_ID = "client_id";
    private final ClientPersistenceQueryUsecases queryUsecases;

    @GetMapping("/oauth2/login")
    public String login(
            HttpServletRequest request,
            @RequestParam(name = CLIENT_ID) String clientId
    ) {
        MDC.put("client_id", clientId);
        log.info("Get login request");
        MDC.clear();
        var client = queryUsecases.getClientByClientId(clientId);
        return String.format("redirect:%s", UriComponentsBuilder
                .fromUriString(client.getTenantUrl())
                .path("login")
                .queryParam("client_id", clientId)
                .queryParam("type", "oauth2")
                .build());
    }
}