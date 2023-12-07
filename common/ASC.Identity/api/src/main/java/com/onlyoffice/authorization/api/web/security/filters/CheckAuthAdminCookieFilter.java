/**
 *
 */
package com.onlyoffice.authorization.api.web.security.filters;

import com.onlyoffice.authorization.api.web.client.APIClient;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
import com.onlyoffice.authorization.api.web.security.context.UserContextContainer;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
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
@Slf4j
public class CheckAuthAdminCookieFilter extends OncePerRequestFilter {
    private final String AUTH_COOKIE_NAME = "asc_auth_key";
    private final String X_DOCSPACE_ADDRESS = "x-docspace-address";

    private final APIClient apiClient;

    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain chain) throws ServletException, IOException {
        log.debug("Validating admin user");
        Cookie[] cookies = request.getCookies();
        if (cookies == null || cookies.length < 1) {
            log.debug("No ASC and Docspace cookie provided");
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
            return;
        }

        var addressCookie = Arrays.stream(cookies)
                .filter(c -> c.getName().equalsIgnoreCase(X_DOCSPACE_ADDRESS))
                .findFirst();

        if (addressCookie.isEmpty()) {
            log.debug("Docspace cookie is empty");
            response.setStatus(HttpStatus.BAD_REQUEST.value());
            return;
        }

        var authCookie = Arrays.stream(cookies)
                .filter(c -> c.getName().equalsIgnoreCase(AUTH_COOKIE_NAME))
                .findFirst();

        if (authCookie.isEmpty()) {
            log.debug("ASC cookie is empty");
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
            return;
        }

        var cookie = String.format("%s=%s", authCookie.get().getName(),
                authCookie.get().getValue());

        MDC.put("cookie", cookie);
        log.debug("========ASC COOKIE========");
        MDC.clear();

        var address = URI.create(addressCookie.get().getValue());
        log.debug("An attempt to get current user profile");
        var me = apiClient.getMe(address, cookie);
        if (me.getStatusCode() == HttpStatus.OK.value() && !me.getResponse().getIsAdmin()) {
            MDC.put("address", address.toString());
            MDC.put("cookie", cookie);
            log.debug("Could not get current user profile or user is not an admin");
            response.setStatus(HttpStatus.FORBIDDEN.value());
            MDC.clear();
            return;
        }

        log.debug("An attempt to get tenant info");
        var tenant = apiClient.getTenant(address, cookie);
        if (tenant.getStatusCode() != HttpStatus.OK.value()) {
            MDC.put("address", address.toString());
            MDC.put("cookie", cookie);
            log.debug("Could not get tenant info");
            MDC.clear();
            response.setStatus(HttpStatus.FORBIDDEN.value());
            return;
        }

        UserContextContainer.context.set(me);
        TenantContextContainer.context.set(tenant);
        chain.doFilter(request, response);
    }

    @Override
    protected boolean shouldNotFilter(HttpServletRequest request)
            throws ServletException {
        String path = request.getRequestURI();
        Pattern first = Pattern.compile("/api/2.0/clients/.*/info");
        Pattern second = Pattern.compile("/api/2.0/clients/consents");
        Pattern third = Pattern.compile("/api/2.0/clients/.*/revoke");
        Pattern fourth = Pattern.compile("/api/2.0/oauth/info");
        Pattern fifth = Pattern.compile("/health/*");
        return first.matcher(path).find() || second
                .matcher(path).find() || third.matcher(path).find() ||
                fourth.matcher(path).find() || fifth.matcher(path).find();
    }
}