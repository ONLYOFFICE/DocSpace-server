/**
 *
 */
package com.onlyoffice.authorization.web.server.controllers;

import com.onlyoffice.authorization.web.server.utilities.HttpUtils;
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
public class LoginController {
    private final String CLIENT_ID = "client_id";

    /**
     *
     * @param clientId
     * @return
     */
    @GetMapping("/oauth2/login")
    public String login(
            HttpServletRequest request,
            @RequestParam(name = CLIENT_ID) String clientId
    ) {
        MDC.put("clientId", clientId);
        log.info("Get login request");
        MDC.clear();

        return String.format("redirect:%s", UriComponentsBuilder
                .fromUriString(String.format("%s://%s", request.getScheme(),
                                HttpUtils.getRequestIP(request)))
                .path("login")
                .queryParam("client_id", clientId)
                .queryParam("type", "oauth2")
                .build());
    }
}