/**
 *
 */
package com.asc.authorization.web.server.controllers;

import com.asc.authorization.web.server.utilities.HttpUtils;
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
@Slf4j
@Controller
@RequiredArgsConstructor
public class AuthorizationConsentController {
    private final String CLIENT_ID = "client_id";

    @GetMapping(value = "/oauth2/consent")
    public String consent(
            HttpServletRequest request,
            @RequestParam(CLIENT_ID) String clientId
    ) {
        MDC.put("clientId", clientId);
        log.info("Got a new consent request");
        MDC.clear();

        return String.format("redirect:%s", UriComponentsBuilder
                .fromUriString(String.format("%s://%s", request.getScheme(),
                        HttpUtils.getRequestIP(request)))
                .path("login")
                .queryParam("type", "oauth2")
                .queryParam("client_id", clientId)
                .build());
    }
}
