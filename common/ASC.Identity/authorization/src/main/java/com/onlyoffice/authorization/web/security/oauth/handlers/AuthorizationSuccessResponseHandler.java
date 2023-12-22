package com.onlyoffice.authorization.web.security.oauth.handlers;

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

/**
 *
 */
@Slf4j
@Component
public class AuthorizationSuccessResponseHandler implements AuthenticationSuccessHandler {
    private final String DISABLE_REDIRECT_HEADER = "X-Disable-Redirect";
    private final String REDIRECT_HEADER = "X-Redirect-URI";

    /**
     *
     * @param request
     * @param response
     * @param authentication
     * @throws IOException
     * @throws ServletException
     */
    public void onAuthenticationSuccess(HttpServletRequest request, HttpServletResponse response, Authentication authentication) throws IOException, ServletException {
        log.info("Authorization success");

        if (authentication instanceof OAuth2AuthorizationCodeRequestAuthenticationToken token) {
            log.info("Redirecting to redirect uri with authorization code");

            String state = token.getState();
            StringBuilder redirectUrl = new StringBuilder(String.format("%s?code=%s",
                    token.getRedirectUri(), token.getAuthorizationCode().getTokenValue()));
            if (state != null && !state.isBlank())
                redirectUrl.append(String.format("&state=%s", state));

            String redirectHeader = request.getHeader(DISABLE_REDIRECT_HEADER);
            if (redirectHeader != null) {
                response.setStatus(HttpStatus.OK.value());
                response.setHeader(REDIRECT_HEADER, redirectUrl.toString());
                return;
            }

            response.sendRedirect(redirectUrl.toString());
        }
    }
}