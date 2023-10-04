package com.onlyoffice.authorization.controllers;

import com.onlyoffice.authorization.configuration.ApplicationConfiguration;
import jakarta.servlet.http.HttpServletRequest;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestParam;

@Controller
@RequiredArgsConstructor
@Slf4j
public class LoginController {
    private final String FORWARD = "FORWARD";
    private final ApplicationConfiguration configuration;

    @GetMapping("/oauth2/login")
    public String login(HttpServletRequest request) {
        log.debug("A new login request");
        if (request.getDispatcherType().name() == null || !request.getDispatcherType().name().equals(FORWARD))
            return "error";
        return "login";
    }

    @GetMapping("/authorized")
    public String authorized(
            @RequestParam(name = "error", required = false) String error,
            @RequestParam(name = "error_description", required = false) String description
    ) {
        log.debug("Authorized redirect");
        if (error != null && !error.isBlank() && description != null && !description.isBlank()) {
            log.debug("Authorization error has occurred {} - {}", error, description);
            return "error";
        }
        return "authorized";
    }
}