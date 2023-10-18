/**
 *
 */
package com.onlyoffice.authorization.external.controllers;

import com.onlyoffice.authorization.external.configuration.DocspaceConfiguration;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;

/**
 *
 */
@Controller
@RequiredArgsConstructor
@Slf4j
public class LoginController {
    private final String FORWARD = "FORWARD";
    private final DocspaceConfiguration docspaceConfiguration;

    @GetMapping("/oauth2/login")
    public String login(
            HttpServletRequest request,
            @RequestParam(name = "error", required = false) String error
    ) {
        log.info("a new login request");
        if (request.getDispatcherType().name() == null || !request.getDispatcherType().name().equals(FORWARD))
            return String.format("redirect:%s", docspaceConfiguration.getUrl());

        if (error != null && !error.isBlank())
            return String.format("redirect:%s", docspaceConfiguration.getUrl());

        return "login";
    }
}