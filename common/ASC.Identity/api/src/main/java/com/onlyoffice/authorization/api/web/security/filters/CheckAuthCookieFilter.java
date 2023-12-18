package com.onlyoffice.authorization.api.web.security.filters;

import com.onlyoffice.authorization.api.web.client.APIClient;
import com.onlyoffice.authorization.api.web.security.context.PersonContextContainer;
import com.onlyoffice.authorization.api.web.security.context.TenantContextContainer;
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
import java.net.URI;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.ExecutionException;
import java.util.regex.Pattern;

@Slf4j
@Component
@RequiredArgsConstructor
public class CheckAuthCookieFilter extends OncePerRequestFilter {
    private final CheckAscCookieCommonProcessor ascCookieCommonProcessor;
    private final APIClient apiClient;

    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response,
                                    FilterChain chain) throws ServletException, IOException {
        log.debug("Validating asc user");
        try {
            var processed = ascCookieCommonProcessor.processAscCookies(request);
            var address = processed.getFirst();
            var ascCookie = processed.getSecond();
            MDC.put("address", address);
            MDC.put("cookie", ascCookie);
            log.debug("ASC COOKIE");
            MDC.clear();

            log.debug("An attempt to get current user profile and tenant info");
            var firstFuture = CompletableFuture.supplyAsync(() -> apiClient
                    .getMe(URI.create(address), ascCookie));
            var secondFuture = CompletableFuture.supplyAsync(() -> apiClient
                    .getTenant(URI.create(address), ascCookie));

            CompletableFuture.allOf(firstFuture, secondFuture).join();
            PersonContextContainer.context.set(firstFuture.get());
            TenantContextContainer.context.set(secondFuture.get());

            chain.doFilter(request, response);
        } catch (BadCredentialsException accessException) {
            response.setStatus(HttpStatus.UNAUTHORIZED.value());
        } catch (InterruptedException | ExecutionException exception) {
            response.setStatus(HttpStatus.BAD_REQUEST.value());
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
