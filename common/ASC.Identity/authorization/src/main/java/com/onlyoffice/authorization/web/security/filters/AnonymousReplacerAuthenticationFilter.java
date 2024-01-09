package com.onlyoffice.authorization.web.security.filters;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpMethod;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.Arrays;
import java.util.regex.Pattern;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class AnonymousReplacerAuthenticationFilter extends OncePerRequestFilter {
    private final String ASC_AUTH_COOKIE = "asc_auth_key";
    private final String CLIENT_ID_QUERY = "client_id";

    private final AuthenticationManager manager;

    /**
     *
     * @param request
     * @param response
     * @param filterChain
     * @throws ServletException
     * @throws IOException
     */
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain) throws ServletException, IOException {
        var clientId = request.getParameter(CLIENT_ID_QUERY);

        if (clientId == null || clientId.isEmpty()) {
            log.warn("Query string does not contain client_id");
            throw new BadCredentialsException("Query string does not contain client_id");
        }

        var authCookie = Arrays.stream(request.getCookies())
                .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_COOKIE))
                .findFirst();

        if (authCookie.isEmpty()) {
            log.warn("ASC authorization cookie is empty");
            throw new BadCredentialsException("ASC authorization cookie is empty");
        }

        var authenticationToken = new UsernamePasswordAuthenticationToken(clientId, authCookie.get());

        var authentication = manager.authenticate(authenticationToken);
        if (authentication.isAuthenticated())
            SecurityContextHolder.getContext().setAuthentication(authentication);

        filterChain.doFilter(request, response);
    }

    /**
     *
     * @param request current HTTP request
     * @return
     * @throws ServletException
     */
    protected boolean shouldNotFilter(HttpServletRequest request) throws ServletException {
        var first = Pattern.compile("/oauth2/authorize");
        var second = Pattern.compile("/oauth2/login");
        var path = request.getRequestURI();
        return !first.matcher(path).find() && !(second.matcher(path).find() && request
                .getMethod().equalsIgnoreCase(HttpMethod.POST.name()));
    }
}
