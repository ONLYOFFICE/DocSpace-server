package com.onlyoffice.authorization.api.extensions.annotations;

import java.lang.annotation.*;

/**
 *
 */
@Retention(RetentionPolicy.RUNTIME)
@Target({ElementType.METHOD, ElementType.TYPE})
@Documented
public @interface DistributedRateLimiter {
    /**
     *
     * @return
     */
    String name();
}
