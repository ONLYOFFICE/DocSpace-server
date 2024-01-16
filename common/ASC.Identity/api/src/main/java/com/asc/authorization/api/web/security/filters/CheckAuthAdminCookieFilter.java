/**
 *
 */
package com.asc.authorization.api.web.security.filters;

import com.asc.authorization.api.web.security.context.PersonContextContainer;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.slf4j.MDC;
import org.springframework.http.HttpStatus;
import org.springframework.security.authentication.BadCredentialsException;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.regex.Pattern;

/**
 *
 */
@Slf4j
@Component
@RequiredArgsConstructor
public class CheckAuthAdminCookieFilter extends OncePerRequestFilter {
    private final CheckAscCookieCommonProcessor ascCookieCommonProcessor;

    /**
     *
     * @param request
     * @param response
     * @param chain
     * @throws ServletException
     * @throws IOException
     */
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain chain) throws ServletException, IOException {
        MDC.put("requestURI", request.getRequestURI());
        log.debug("Validating admin user", request);
        MDC.clear();
        try {
            ascCookieCommonProcessor.processAscCookies(request);
            if (!PersonContextContainer.context.get().getResponse().getIsAdmin()) {
                response.setStatus(HttpStatus.FORBIDDEN.value());
                return;
            }

            chain.doFilter(request, response);
        } catch (BadCredentialsException accessException) {
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
        }
    }

    /**
     *
     * @param request current HTTP request
     * @return
     * @throws ServletException
     */
    protected boolean shouldNotFilter(HttpServletRequest request)
            throws ServletException {
        var path = request.getRequestURI();
        var first = Pattern.compile("/api/2.0/clients/.*/info");
        var second = Pattern.compile("/api/2.0/clients/consents");
        var third = Pattern.compile("/api/2.0/clients/.*/revoke");
        var fourth = Pattern.compile("/api/2.0/oauth/info");
        var fifth = Pattern.compile("/health/*");
        return first.matcher(path).find() || second
                .matcher(path).find() || third.matcher(path).find() ||
                fourth.matcher(path).find() || fifth.matcher(path).find();
    }
}