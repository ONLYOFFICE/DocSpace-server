package com.onlyoffice.authorization.api.extensions.aspects;

import com.onlyoffice.authorization.api.core.exceptions.DistributedRateLimiterException;
import com.onlyoffice.authorization.api.extensions.annotations.DistributedRateLimiter;
import lombok.RequiredArgsConstructor;
import org.aspectj.lang.ProceedingJoinPoint;
import org.aspectj.lang.annotation.Around;
import org.aspectj.lang.annotation.Aspect;
import org.aspectj.lang.reflect.MethodSignature;
import org.redisson.api.RedissonClient;
import org.springframework.stereotype.Component;
import org.springframework.web.context.request.RequestContextHolder;
import org.springframework.web.context.request.ServletRequestAttributes;

/**
 *
 */
@Aspect
@Component
@RequiredArgsConstructor
public class DistributedRateLimiterAspect {
    private final String X_RATE_LIMIT = "X-Ratelimit-Limit";
    private final String X_RATE_REMAINING = "X-Ratelimit-Remaining";
    private final String X_RATE_RESET = "X-Ratelimit-Reset";

    private final RedissonClient redissonClient;

    /**
     *
     * @param joinPoint
     * @return
     * @throws Throwable
     */
    @Around("@annotation(com.onlyoffice.authorization.api.extensions.annotations.DistributedRateLimiter)")
    public Object rateLimit(ProceedingJoinPoint joinPoint) throws Throwable {
        var signature = (MethodSignature) joinPoint.getSignature();
        var method = signature.getMethod();
        var annotation = method.getAnnotation(DistributedRateLimiter.class);

        var limiter = redissonClient.getRateLimiter(annotation.name());
        if (limiter == null)
            throw new DistributedRateLimiterException("Could not initialize a distributed rate-limiter instance");

        var response = ((ServletRequestAttributes) RequestContextHolder.currentRequestAttributes()).getResponse();
        response.setHeader(X_RATE_LIMIT, String.valueOf(limiter.getConfig().getRate()));
        response.setHeader(X_RATE_REMAINING, String.valueOf(limiter.availablePermits()));
        response.setHeader(X_RATE_RESET, String.valueOf(limiter.getConfig().getRateInterval()));
        if (!limiter.tryAcquire(1))
            throw new DistributedRateLimiterException("Could not acquire a rate-limiter permission");

        return joinPoint.proceed();
    }
}
