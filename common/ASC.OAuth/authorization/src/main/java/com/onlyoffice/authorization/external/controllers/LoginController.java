/**
 *
 */
package com.onlyoffice.authorization.external.controllers;

import com.onlyoffice.authorization.external.configuration.DocspaceConfiguration;
import jakarta.annotation.PostConstruct;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.CookieValue;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.util.UriComponentsBuilder;

import java.net.URL;

/**
 *
 */
@Controller
@RequiredArgsConstructor
@Slf4j
public class LoginController {
    private URL docspaceURL;
    private final String FORWARD = "FORWARD";
    private final DocspaceConfiguration docspaceConfiguration;

    @PostConstruct
    @SneakyThrows
    private void init() {
        this.docspaceURL = new URL(docspaceConfiguration.getUrl());
    }

    @GetMapping("/oauth2/login")
    public String login(
            HttpServletRequest request,
            @RequestParam(name = "error", required = false) String error,
            @CookieValue(name = "client_id") String clientId
    ) {
        var loginUrl = String.format("redirect:%s", UriComponentsBuilder
                .fromUriString(this.docspaceURL.toString())
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