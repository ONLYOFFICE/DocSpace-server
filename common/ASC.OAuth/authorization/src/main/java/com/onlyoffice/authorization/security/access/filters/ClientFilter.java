/**
 *
 */
package com.onlyoffice.authorization.security.access.filters;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;

/**
 *
 */
@Deprecated
public class ClientFilter extends OncePerRequestFilter {
    private final String CLIENT_ID = "client_id";

    protected void doFilterInternal(
            HttpServletRequest request,
            HttpServletResponse response,
            FilterChain filterChain
    ) throws ServletException, IOException {
        var clientId = request.getParameter(CLIENT_ID);
        if (clientId != null && !clientId.isBlank()) {
            Cookie cookie = new Cookie(CLIENT_ID, clientId);
            cookie.setMaxAge(10000);
            response.addCookie(cookie);
        }

        filterChain.doFilter(request, response);
    }
}
