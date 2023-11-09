package com.onlyoffice.authorization.security.oauth.handlers;

import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.extern.slf4j.Slf4j;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeRequestAuthenticationToken;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.stereotype.Component;

import java.io.IOException;

@Component
@Slf4j
public class AuthorizationSuccessResponseHandler implements AuthenticationSuccessHandler {
    @Override
    public void onAuthenticationSuccess(HttpServletRequest request, HttpServletResponse response, Authentication authentication) throws IOException, ServletException {
        log.info("Authorization success");
        if (authentication instanceof OAuth2AuthorizationCodeRequestAuthenticationToken token) {
            log.info("Redirecting to redirect uri with authorization code");
            response.sendRedirect(String
                    .format("%s?code=%s", token.getRedirectUri(),
                            token.getAuthorizationCode().getTokenValue()));
        }
    }
}
