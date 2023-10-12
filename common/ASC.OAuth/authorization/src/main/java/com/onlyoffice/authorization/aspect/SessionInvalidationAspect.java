package com.onlyoffice.authorization.aspect;

import com.onlyoffice.authorization.configuration.DocspaceConfiguration;
import jakarta.servlet.http.Cookie;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import jakarta.servlet.http.HttpSession;
import lombok.RequiredArgsConstructor;
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

@Component
@RequiredArgsConstructor
@Aspect
public class SessionInvalidationAspect {
    private final DocspaceConfiguration docspaceConfiguration;

    @Value("${server.servlet.session.cookie.name:JSESSIONID}")
    private String JSESSIONID;
    @Around("@annotation(InvalidateSession)")
    public Object sessionInvalidationAdvice(
            ProceedingJoinPoint pjp
    ) throws Throwable {
        HttpServletRequest request = ((ServletRequestAttributes) RequestContextHolder
                .getRequestAttributes()).getRequest();
        HttpServletResponse response = ((ServletRequestAttributes) RequestContextHolder
                .getRequestAttributes()).getResponse();

        if (request != null && Arrays.stream(request.getCookies())
                .filter(c -> c.getName().equalsIgnoreCase("asc_auth_key"))
                .findFirst().isEmpty()) {
            HttpSession session = request.getSession(false);
            if (session != null)
                session.invalidate();
            boolean isSecure = request.isSecure();
            String contextPath = request.getContextPath();

            SecurityContext context = SecurityContextHolder.getContext();
            SecurityContextHolder.clearContext();
            context.setAuthentication(null);
            if (response != null) {
                Cookie cookie = new Cookie(JSESSIONID, null);
                String cookiePath = StringUtils.hasText(contextPath) ? contextPath : "/";
                cookie.setPath(cookiePath);
                cookie.setMaxAge(0);
                cookie.setSecure(isSecure);
                response.addCookie(cookie);
                response.sendRedirect(docspaceConfiguration.getUrl());
            }
        }

        return pjp.proceed();
    }
}
