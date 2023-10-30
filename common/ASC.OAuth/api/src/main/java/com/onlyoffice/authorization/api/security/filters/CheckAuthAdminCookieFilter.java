/**
 *
 */
package com.onlyoffice.authorization.api.security.filters;

import com.onlyoffice.authorization.api.external.clients.DocspaceClient;
import com.onlyoffice.authorization.api.security.container.UserContextContainer;
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
import java.net.URI;
import java.util.Arrays;
import java.util.regex.Pattern;

/**
 *
 */
@Component
@RequiredArgsConstructor
public class CheckAuthAdminCookieFilter extends OncePerRequestFilter {
    private final String AUTH_COOKIE_NAME = "asc_auth_key";
    private final String X_DOCSPACE_ADDRESS = "x-docspace-address";
    private final String X_TENANT_HEADER = "X-Tenant";

    private final DocspaceClient docspaceClient;

    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain chain) throws ServletException, IOException {
        Cookie[] cookies = request.getCookies();
        if (cookies == null || cookies.length < 1) {
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
            return;
        }

        var addressCookie = Arrays.stream(cookies)
                .filter(c -> c.getName().equalsIgnoreCase(X_DOCSPACE_ADDRESS))
                .findFirst();

        if (addressCookie.isEmpty()) {
            response.setStatus(HttpStatus.BAD_REQUEST.value());
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

        var address = URI.create(addressCookie.get().getValue());
        var me = docspaceClient.getMe(address, cookie);
        if (me.getStatusCode() == HttpStatus.OK.value() && !me.getResponse().getIsAdmin()) {
            response.setStatus(HttpStatus.FORBIDDEN.value());
            return;
        }

        var tenant = docspaceClient.getTenant(address, cookie);
        if (tenant.getStatusCode() != HttpStatus.OK.value()) {
            response.setStatus(HttpStatus.FORBIDDEN.value());
            return;
        }

        UserContextContainer.context.set(me);
        response.setHeader(X_TENANT_HEADER, String.valueOf(tenant.getResponse().getTenantId()));
        chain.doFilter(request, response);
    }

    @Override
    protected boolean shouldNotFilter(HttpServletRequest request)
            throws ServletException {
        String path = request.getRequestURI();
        Pattern first = Pattern.compile("/api/2.0/clients/.*/info");
        Pattern second = Pattern.compile("/api/2.0/clients/consents");
        return first.matcher(path).find() || second.matcher(path).find();
    }
}