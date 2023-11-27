package com.onlyoffice.authorization.security.oauth.handlers;

import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.security.core.Authentication;
import org.springframework.security.oauth2.server.authorization.authentication.OAuth2AuthorizationCodeRequestAuthenticationToken;
import org.springframework.security.web.authentication.AuthenticationSuccessHandler;
import org.springframework.stereotype.Component;

import java.io.IOException;

@Component
@Slf4j
public class AuthorizationSuccessResponseHandler implements AuthenticationSuccessHandler {
    private final String REDIRECT_HEADER = "X-Redirect-URI";
    @Override
    public void onAuthenticationSuccess(HttpServletRequest request, HttpServletResponse response, Authentication authentication) throws IOException, ServletException {
        log.info("Authorization success");
        if (authentication instanceof OAuth2AuthorizationCodeRequestAuthenticationToken token) {
            log.info("Redirecting to redirect uri with authorization code");
            String state = token.getState();
            StringBuilder redirectUrl = new StringBuilder(String.format("%s?code=%s",
                    token.getRedirectUri(), token.getAuthorizationCode().getTokenValue()));
            if (state != null && !state.isBlank())
                redirectUrl.append(String.format("&state=%s", state));
            response.setStatus(HttpStatus.OK.value());
            response.setHeader(REDIRECT_HEADER, redirectUrl.toString());
        }
    }
}
