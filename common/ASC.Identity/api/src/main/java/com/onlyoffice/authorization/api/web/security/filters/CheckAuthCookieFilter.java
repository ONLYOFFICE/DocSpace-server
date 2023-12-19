package com.onlyoffice.authorization.api.web.security.filters;

import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.HttpStatus;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.regex.Pattern;

@Slf4j
@Component
@RequiredArgsConstructor
public class CheckAuthCookieFilter extends OncePerRequestFilter {
    private final CheckAscCookieCommonProcessor ascCookieCommonProcessor;

    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain chain) throws ServletException, IOException {
        log.debug("Validating asc user");
        try {
            ascCookieCommonProcessor.processAscCookies(request);
            chain.doFilter(request, response);
        } catch (BadCredentialsException accessException) {
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
        }
    }

    @Override
    protected boolean shouldNotFilter(HttpServletRequest request)
            throws ServletException {
        var first = Pattern.compile("/api/2.0/oauth/info");
        var second = Pattern.compile("/health/*");
        var third = Pattern.compile("/api/2.0/clients/.*/info");
        var path = request.getRequestURI();
        return first.matcher(path).find() || second.matcher(path).find() ||
                third.matcher(path).find();
    }
}
