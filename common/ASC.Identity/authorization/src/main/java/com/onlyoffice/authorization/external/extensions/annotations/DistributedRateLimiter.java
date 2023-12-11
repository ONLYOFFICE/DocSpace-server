package com.onlyoffice.authorization.external.extensions.annotations;

import java.lang.annotation.*;

@Retention(RetentionPolicy.RUNTIME)
@Target({ElementType.METHOD, ElementType.TYPE})
@Documented
public @interface DistributedRateLimiter {
    String name();
}
