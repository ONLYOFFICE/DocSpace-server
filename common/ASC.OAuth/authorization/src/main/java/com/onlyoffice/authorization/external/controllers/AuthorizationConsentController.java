/**
 *
 */
package com.onlyoffice.authorization.external.controllers;

import com.onlyoffice.authorization.external.configuration.DocspaceConfiguration;
import com.onlyoffice.authorization.security.access.aspects.annotations.InvalidateSession;
import jakarta.annotation.PostConstruct;
import lombok.RequiredArgsConstructor;
import lombok.SneakyThrows;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.CookieValue;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.util.UriComponentsBuilder;

import java.net.URL;

/**
 *
 */
@Controller
@RequiredArgsConstructor
@Slf4j
public class AuthorizationConsentController {
    private URL docspaceURL;
    private final DocspaceConfiguration docspaceConfiguration;

    @PostConstruct
    @SneakyThrows
    private void init() {
        this.docspaceURL = new URL(docspaceConfiguration.getUrl());
    }

    @GetMapping(value = "/oauth2/consent")
    @InvalidateSession
    public String consent(@CookieValue(name = "client_id") String clientId) {
        log.info("got a new consent request");
        return String.format("redirect:%s", UriComponentsBuilder
                .fromUriString(this.docspaceURL.toString())
                .path("login/consent")
                .queryParam("type", "oauth2")
                .queryParam("client_id", clientId)
                .build());
    }
}
