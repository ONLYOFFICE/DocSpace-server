/**
 *
 */
package com.onlyoffice.authorization.api.security.filters;

import com.onlyoffice.authorization.api.external.clients.DocspaceClient;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import org.springframework.http.HttpStatus;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.Arrays;

/**
 *
 */
@Component
@RequiredArgsConstructor
public class CheckAuthCookieFilter extends OncePerRequestFilter {
    private final String AUTH_COOKIE_NAME = "asc_auth_key";
    private final String X_TENANT_HEADER = "X-Tenant";

    private final DocspaceClient docspaceClient;

    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain chain) throws ServletException, IOException {
        Cookie[] cookies = request.getCookies();
        if (cookies == null || cookies.length < 1) {
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
            return;
        }

        var authCookie = Arrays.stream(cookies)
                .filter(c -> c.getName().equalsIgnoreCase(AUTH_COOKIE_NAME))
                .findFirst();

        if (authCookie.isEmpty()) {
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
            return;
        }

        var cookie = String.format("%s=%s", authCookie.get().getName(),
                authCookie.get().getValue());

        var me = docspaceClient.getMe(cookie);
        if (me.getStatusCode() == HttpStatus.OK.value() && !me.getResponse().getIsAdmin()) {
            response.setStatus(HttpStatus.FORBIDDEN.value());
            return;
        }

        var tenant = docspaceClient.getTenant(cookie);
        if (tenant.getStatusCode() != HttpStatus.OK.value()) {
            response.setStatus(HttpStatus.FORBIDDEN.value());
            return;
        }

        response.setHeader(X_TENANT_HEADER, String.valueOf(tenant.getResponse().getTenantId()));
        chain.doFilter(request, response);
    }
}