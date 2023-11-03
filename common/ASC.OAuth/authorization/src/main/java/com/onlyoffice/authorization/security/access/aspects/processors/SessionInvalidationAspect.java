/**
 *
 */
package com.onlyoffice.authorization.security.access.aspects.processors;

import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.aspectj.lang.ProceedingJoinPoint;
import org.aspectj.lang.annotation.Around;
import org.aspectj.lang.annotation.Aspect;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.security.core.context.SecurityContext;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.stereotype.Component;
import org.springframework.util.StringUtils;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

import java.util.Arrays;

/**
 *
 */
@Slf4j
@Aspect
@Component
@RequiredArgsConstructor
public class SessionInvalidationAspect {
    @Value("${server.servlet.session.cookie.name:JSESSIONID}")
    private String JSESSIONID;
    private final String X_DOCSPACE_ADDRESS = "x-docspace-address";
    private final String ASC_AUTH_KEY = "asc_auth_key";

    @Around("@annotation(com.onlyoffice.authorization.security.access.aspects.annotations.InvalidateSession)")
    public Object sessionInvalidationAdvice(ProceedingJoinPoint pjp)
            throws Throwable {
        HttpServletRequest request = ((ServletRequestAttributes) RequestContextHolder
                .getRequestAttributes()).getRequest();
        HttpServletResponse response = ((ServletRequestAttributes) RequestContextHolder
                .getRequestAttributes()).getResponse();

        var address = Arrays.stream(request.getCookies()).filter(c -> c.getName()
                .equalsIgnoreCase(X_DOCSPACE_ADDRESS)).findFirst();
        if (request != null && address.isPresent() && Arrays.stream(request.getCookies())
                .filter(c -> c.getName().equalsIgnoreCase(ASC_AUTH_KEY))
                .findFirst().isEmpty()) {
            log.info("Trying to remove JSESSIONID");
            HttpSession session = request.getSession(false);
            if (session != null)
                session.invalidate();
            boolean isSecure = request.isSecure();
            String contextPath = request.getContextPath();

            SecurityContext context = SecurityContextHolder.getContext();
            SecurityContextHolder.clearContext();
            context.setAuthentication(null);
            if (response != null) {
                log.info("Removing JSESSIONID and redirecting to Docspace");
                Cookie cookie = new Cookie(JSESSIONID, null);
                String cookiePath = StringUtils.hasText(contextPath) ? contextPath : "/";
                cookie.setPath(cookiePath);
                cookie.setMaxAge(0);
                cookie.setSecure(isSecure);
                response.addCookie(cookie);
                response.sendRedirect(address.get().getValue());
            }
        }

        return pjp.proceed();
    }
}
